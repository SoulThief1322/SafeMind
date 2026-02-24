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

        // ── Doctor-side methods ──

        /// <summary>
        /// Returns conversations for a doctor, keyed by patient.
        /// </summary>
        public async Task<object> GetDoctorConversationsAsync(string doctorUserId)
        {
            // Get all patient IDs this doctor has sessions with
            var doctor = await _safeMindDbContext.Doctors
                .Where(d => d.UserId == doctorUserId)
                .Select(d => d.Id)
                .FirstOrDefaultAsync();

            if (doctor == 0) return new List<object>();

            var patientIds = await _safeMindDbContext.Sessions
                .Where(s => s.DoctorId == doctor)
                .Select(s => s.PatientId)
                .Distinct()
                .ToListAsync();

            var conversations = await _safeMindDbContext.ChatMessages
                .Where(m =>
                    (m.SenderId == doctorUserId && patientIds.Contains(m.ReceiverId)) ||
                    (m.ReceiverId == doctorUserId && patientIds.Contains(m.SenderId)))
                .GroupBy(m => m.SenderId == doctorUserId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    PatientId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToListAsync();

            // Resolve patient names
            var patientUserIds = conversations.Select(c => c.PatientId).ToList();
            var patientNames = await _safeMindDbContext.Users
                .Where(u => patientUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "Patient");

            // Also get the session contact full names for better display
            var contactNames = await _safeMindDbContext.Sessions
                .Where(s => s.DoctorId == doctor && patientUserIds.Contains(s.PatientId))
                .GroupBy(s => s.PatientId)
                .Select(g => new { PatientId = g.Key, Name = g.OrderByDescending(s => s.TimeOfBooking).First().Contact.FullName })
                .ToDictionaryAsync(x => x.PatientId, x => x.Name);

            return conversations.Select(c => new
            {
                c.PatientId,
                PatientName = contactNames.ContainsKey(c.PatientId) ? contactNames[c.PatientId]
                             : patientNames.ContainsKey(c.PatientId) ? patientNames[c.PatientId]
                             : "Patient",
                c.LastMessage
            });
        }

        /// <summary>
        /// Returns all patients this doctor has had sessions with.
        /// </summary>
        public async Task<object> GetMyPatientsAsync(string doctorUserId)
        {
            var doctor = await _safeMindDbContext.Doctors
                .Where(d => d.UserId == doctorUserId)
                .Select(d => d.Id)
                .FirstOrDefaultAsync();

            if (doctor == 0) return new List<object>();

            // All patients this doctor has had sessions with, with their contact names
            var patients = await _safeMindDbContext.Sessions
                .Where(s => s.DoctorId == doctor)
                .GroupBy(s => s.PatientId)
                .Select(g => new
                {
                    PatientId = g.Key,
                    Name = g.OrderByDescending(s => s.TimeOfBooking).First().Contact.FullName
                })
                .ToListAsync();

            // Which of those patients already have chat messages with this doctor
            var patientIdsAll = patients.Select(p => p.PatientId).ToList();
            var patientsWithMessages = await _safeMindDbContext.ChatMessages
                .Where(m =>
                    (m.SenderId == doctorUserId && patientIdsAll.Contains(m.ReceiverId)) ||
                    (m.ReceiverId == doctorUserId && patientIdsAll.Contains(m.SenderId)))
                .Select(m => m.SenderId == doctorUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            return patients.Select(p => new
            {
                p.PatientId,
                p.Name,
                HasConversation = patientsWithMessages.Contains(p.PatientId)
            });
        }

        /// <summary>
        /// Returns messages between the doctor and a specific patient.
        /// </summary>
        public async Task<object> GetDoctorMessagesAsync(string doctorUserId, string patientId)
        {
            var messages = await _safeMindDbContext.ChatMessages
                .Where(m =>
                    (m.SenderId == doctorUserId && m.ReceiverId == patientId) ||
                    (m.SenderId == patientId && m.ReceiverId == doctorUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    IsMine = m.SenderId == doctorUserId,
                    m.Message,
                    Timestamp = m.Timestamp.ToString("o")
                })
                .ToListAsync();

            return messages;
        }
    }
}
