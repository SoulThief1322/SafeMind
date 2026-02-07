using Data.Models;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;

namespace SafeMind.Services
{
    public class MySessionService
    {
        public Task<List<MySessionsViewModel>> GetSessions(SafeMindDbContext context, string userId)
        {
            var sessions = context.Sessions
            .AsNoTracking()
            .Include(s => s.Doctor)
            .Include(s => s.Contact)
            .Where(s => s.PatientId == userId)
            .OrderBy(s => s.StartTime)
            .Select(s => new MySessionsViewModel
            {
                DoctorName = s.Doctor != null ? s.Doctor.Name : string.Empty,
                SessionDate = DateOnly.FromDateTime(s.StartTime.DateTime),
                SessionTime = TimeOnly.FromDateTime(s.StartTime.DateTime),
                SessionPrice = s.Price,
                SessionDuration = s.Doctor != null ? s.Doctor.SessionDuration : 0,
                ContactFullName = s.Contact != null ? s.Contact.FullName : string.Empty,
                PaymentStatus = s.PaymentStatus,
                SessionStatus = s.SessionStatus
            })
            .ToListAsync();
            return sessions;
        }
    }
}