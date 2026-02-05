using Data.Enums;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Services
{

    public class BookSessionService(SafeMindDbContext context, BookService bookService)
    {
        public async Task<Doctor> GetSelectedDoctor(int id)
        {
            var doctor = await (await bookService.GetDoctors()).Where(d => d.Id == id).FirstOrDefaultAsync();
            return doctor;
        }
        public async Task<List<TimeOnly>> GetTakenSessions(DateTimeOffset dayStart, DateTimeOffset dayEnd, int doctorId)
        {
            var sessions = await context.Sessions
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == doctorId &&
                s.StartTime >= dayStart &&
                s.StartTime < dayEnd &&
                s.SessionStatus != SessionStatus.Cancelled)
            .Select(s => TimeOnly.FromDateTime(s.StartTime.DateTime))
            .ToListAsync();
            return sessions;
        }
    }
}