using SafeMind.Data.Models;
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
        /// Patient sends a message to a doctor using the doctor's public integer ID.
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

            var receiverUserId = doctor.UserId;

            // Verify the patient has at least one session with this doctor
            bool hasSharedSession = await _db.Sessions.AnyAsync(s =>
                s.DoctorId == doctorId && s.PatientId == senderUserId
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

            // Notify the doctor
            await Clients.User(receiverUserId).SendAsync("ReceiveMessage", new
            {
                chatMessage.Id,
                DoctorId = doctorId,
                PatientId = senderUserId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });

            // Echo back to patient
            await Clients.Caller.SendAsync("MessageSent", new
            {
                chatMessage.Id,
                DoctorId = doctorId,
                PatientId = senderUserId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });
        }

        /// <summary>
        /// Doctor sends a message to a patient using the patient's UserId.
        /// </summary>
        public async Task SendMessageToPatient(string patientId, string message)
        {
            var senderUserId = Context.UserIdentifier;
            if (senderUserId is null || string.IsNullOrWhiteSpace(message))
                return;

            // Resolve the doctor record for the sender
            var doctor = await _db.Doctors
                .Where(d => d.UserId == senderUserId)
                .Select(d => new { d.Id, d.UserId })
                .FirstOrDefaultAsync();

            if (doctor is null)
                return;

            // Verify this doctor has at least one session with the patient
            bool hasSharedSession = await _db.Sessions.AnyAsync(s =>
                s.DoctorId == doctor.Id && s.PatientId == patientId
            );

            if (!hasSharedSession)
                return;

            var chatMessage = new ChatMessage
            {
                SenderId = senderUserId,
                ReceiverId = patientId,
                Message = message.Trim(),
                Timestamp = DateTimeOffset.UtcNow,
                IsRead = false
            };
            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            // Notify the patient
            await Clients.User(patientId).SendAsync("ReceiveMessage", new
            {
                chatMessage.Id,
                DoctorId = doctor.Id,
                PatientId = patientId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });

            // Echo back to doctor
            await Clients.Caller.SendAsync("MessageSent", new
            {
                chatMessage.Id,
                DoctorId = doctor.Id,
                PatientId = patientId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });
        }

        /// <summary>
        /// Patient marks messages from a doctor as read.
        /// </summary>
        public async Task MarkAsRead(int doctorId)
        {
            var currentUserId = Context.UserIdentifier;
            if (currentUserId is null) return;

            var doctorUserId = await _db.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync();

            if (doctorUserId is null) return;

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == doctorUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
                msg.IsRead = true;

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Doctor marks messages from a patient as read.
        /// </summary>
        public async Task MarkPatientAsRead(string patientId)
        {
            var currentUserId = Context.UserIdentifier;
            if (currentUserId is null) return;

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == patientId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
                msg.IsRead = true;

            await _db.SaveChangesAsync();
        }
    }
}