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

        public async Task SendMessage(string receiverUserId, string message)
        {
            var senderUserId = Context.UserIdentifier;
            if (senderUserId is null || string.IsNullOrWhiteSpace(message))
                return;

            bool hasSharedSession = await _db.Sessions.AnyAsync(s =>
                (s.PatientId == senderUserId && s.Doctor.UserId == receiverUserId) ||
                (s.Doctor.UserId == senderUserId && s.PatientId == receiverUserId)
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
                chatMessage.SenderId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });

            await Clients.Caller.SendAsync("MessageSent", new
            {
                chatMessage.Id,
                chatMessage.ReceiverId,
                chatMessage.Message,
                Timestamp = chatMessage.Timestamp.ToString("o")
            });
        }
        public async Task MarkAsRead(string senderUserId)
        {
            var currentUserId = Context.UserIdentifier;
            if (currentUserId is null) return;

            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
                msg.IsRead = true;

            await _db.SaveChangesAsync();
        }
    }
}