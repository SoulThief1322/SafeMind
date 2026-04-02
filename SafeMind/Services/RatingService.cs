using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Data.Models;

namespace SafeMind.Services
{
    public class RatingService
    {
        private readonly SafeMindDbContext _context;

        public RatingService(SafeMindDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Error)> SubmitRatingAsync(int sessionId, string patientId, int stars)
        {
            if (stars < 1 || stars > 5)
                return (false, "Rating must be between 1 and 5.");

            var session = await _context.Sessions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.PatientId == patientId);

            if (session == null)
                return (false, "Session not found.");

            if (session.EndTime > DateTimeOffset.UtcNow)
                return (false, "Session has not ended yet.");

            if (DateTimeOffset.UtcNow - session.EndTime > TimeSpan.FromDays(30))
                return (false, "The 30-day rating window for this session has passed.");

            var alreadyRated = await _context.SessionRatings
                .AnyAsync(r => r.SessionId == sessionId && r.PatientId == patientId);

            if (alreadyRated)
                return (false, "You have already rated this session.");

            var rating = new SessionRating
            {
                SessionId = sessionId,
                PatientId = patientId,
                Stars = stars,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.SessionRatings.Add(rating);
            await _context.SaveChangesAsync();

            await RecalculateDoctorRatingAsync(session.Doctor.Id);

            return (true, string.Empty);
        }

        private async Task RecalculateDoctorRatingAsync(int doctorId)
        {
            var average = await _context.SessionRatings
                .Where(r => r.Session.DoctorId == doctorId)
                .AverageAsync(r => (double?)r.Stars);

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return;

            doctor.Rating = average.HasValue ? (decimal)Math.Round(average.Value, 2) : doctor.Rating;
            await _context.SaveChangesAsync();
        }
    }
}
