using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly SafeMindDbContext _db;

        public ChatHub(SafeMindDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Client sends a message using the doctor's public integer ID.
        /// The hub resolves it to the actual UserId server-side.
        /// </summary>
        public async Task SendMessage(int doctorId, string message)
        {
            var senderUserId = Context.UserIdentifier;
            if (senderUserId is null || string.IsNullOrWhiteSpace(message))
                return;

            // Resolve doctorId to the doctor's IdentityUser ID
            var doctor = await _db.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => new { d.UserId })
                .FirstOrDefaultAsync();

            if (doctor is null)
                return;

            // Determine who is the receiver: if sender is the doctor, receiver is the patient; otherwise receiver is the doctor
            var receiverUserId = doctor.UserId == senderUserId
                ? await _db.Sessions
                    .Where(s => s.DoctorId == doctorId && s.Doctor.UserId == senderUserId)
                    .Select(s => s.PatientId)
                    .FirstOrDefaultAsync()
                : doctor.UserId;

            if (receiverUserId is null)
                return;

            // Verify they share at least one session
            bool hasSharedSession = await _db.Sessions.AnyAsync(s =>
                s.DoctorId == doctorId &&
                (s.PatientId == senderUserId || s.Doctor.UserId == senderUserId)
            );

            if (!hasSharedSession)
                return; 

            var chatMessage = new ChatMessage
            {
                SenderId = senderUserId,
                ReceiverId = receiverUserId,
                Message = message.Trim(),
                Timestamp = DateTimeOffset.UtcNow,
                IsRead = false
            };
            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            await Clients.User(receiverUserId).SendAsync("ReceiveMessage", new
            {
                chatMessage.Id,
                DoctorId = doctorId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });

            await Clients.Caller.SendAsync("MessageSent", new
            {
                chatMessage.Id,
                DoctorId = doctorId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });
        }
        public async Task MarkAsRead(int doctorId)
        {
            var currentUserId = Context.UserIdentifier;
            if (currentUserId is null) return;

            // Resolve the doctor's UserId from the public doctorId
            var doctorUserId = await _db.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync();

            if (doctorUserId is null) return;

            // The "sender" is whichever side is the doctor in this conversation
            var senderUserId = doctorUserId == currentUserId
                ? await _db.Sessions.Where(s => s.DoctorId == doctorId).Select(s => s.PatientId).FirstOrDefaultAsync()
                : doctorUserId;

            if (senderUserId is null) return;

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
                msg.IsRead = true;

            await _db.SaveChangesAsync();
        }
    }
}