using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Services
{
    public class ChatService
    {
        private readonly SafeMindDbContext _safeMindDbContext;

        public ChatService(SafeMindDbContext safeMindDbContext)
        {
            _safeMindDbContext = safeMindDbContext;
        }

        public async Task<object> GetConversationsAsync(string currentUserId)
        {
            var conversations = await _safeMindDbContext.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToListAsync();

            var otherUserIds = conversations.Select(c => c.OtherUserId).ToList();
            var userIdToDoctorMap = await _safeMindDbContext.Doctors
                .Where(d => otherUserIds.Contains(d.UserId))
                .Select(d => new { d.UserId, DoctorId = d.Id, d.Name })
                .ToDictionaryAsync(d => d.UserId);

            var result = conversations.Select(c => new
            {
                DoctorId = userIdToDoctorMap.ContainsKey(c.OtherUserId) ? userIdToDoctorMap[c.OtherUserId].DoctorId : (int?)null,
                DoctorName = userIdToDoctorMap.ContainsKey(c.OtherUserId) ? userIdToDoctorMap[c.OtherUserId].Name : null,
                c.LastMessage
            });

            return result;
        }

        public async Task<object> GetMyDoctorsAsync(string currentUserId)
        {
            // All doctors this patient has had sessions with
            var doctors = await _safeMindDbContext.Sessions
                .Where(s => s.PatientId == currentUserId)
                .Select(s => s.Doctor)
                .Distinct()
                .Select(d => new
                {
                    DoctorId = d.Id,
                    d.Name,
                    d.UserId
                })
                .ToListAsync();

            // Which of those doctors already have chat messages with this user
            var doctorUserIds = doctors.Select(d => d.UserId).ToList();
            var doctorsWithMessages = await _safeMindDbContext.ChatMessages
                .Where(m =>
                    (m.SenderId == currentUserId && doctorUserIds.Contains(m.ReceiverId)) ||
                    (m.ReceiverId == currentUserId && doctorUserIds.Contains(m.SenderId)))
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            return doctors.Select(d => new
            {
                d.DoctorId,
                d.Name,
                HasConversation = doctorsWithMessages.Contains(d.UserId)
            });
        }

        public async Task<object> GetMessagesAsync(string currentUserId, int doctorId)
        {
            // Resolve the doctor's IdentityUser ID
            var doctorUserId = await _safeMindDbContext.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync();

            if (doctorUserId == null) return new List<object>();

            var messages = await _safeMindDbContext.ChatMessages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == doctorUserId) ||
                    (m.SenderId == doctorUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    IsMine = m.SenderId == currentUserId,
                    m.Message,
                    Timestamp = m.Timestamp.ToString("o")
                })
                .ToListAsync();

            return messages;
        }
    }
}
