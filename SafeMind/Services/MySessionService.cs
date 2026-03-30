
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Models;
using static SafeMind.Services.SlotsService;

namespace SafeMind.Services
{
    public class MySessionService
    {
        private readonly SafeMindDbContext _context;
        private readonly BookSessionService _bookSessionService;
        private readonly SlotsService _slotsService;

        public MySessionService(SafeMindDbContext context, BookSessionService bookSessionService, SlotsService slotsService)
        {
            _context = context;
            _bookSessionService = bookSessionService;
            _slotsService = slotsService;
        }

        public Task<List<MySessionsViewModel>> GetSessions(string userId, bool isDoctor)
        {
            IQueryable<Session> query = _context.Sessions
                .AsNoTracking()
                .Include(s => s.Doctor)
                .Include(s => s.Contact)
                .Where(s => s.SessionStatus != SessionStatus.Cancelled);

            query = isDoctor
                ? query.Where(s => s.Doctor != null && s.Doctor.UserId == userId)
                : query.Where(s => s.PatientId == userId);

            var sessions = query
                .OrderBy(s => s.StartTime)
                .Select(s => new MySessionsViewModel
                {
                    SessionId = s.Id,
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

        // ── Find session for a given user ──

        public async Task<Session?> GetSessionForUser(int sessionId, string userId, bool isDoctor)
        {
            return isDoctor
                ? await _context.Sessions.Include(s => s.Doctor)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.Doctor != null && s.Doctor.UserId == userId)
                : await _context.Sessions.Include(s => s.Doctor)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.PatientId == userId);
        }

        public async Task<Session?> GetSessionWithDoctorDetails(int sessionId, string userId, bool isDoctor)
        {
            return isDoctor
                ? await _context.Sessions.Include(s => s.Doctor)
                    .ThenInclude(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                    .Include(s => s.Doctor).ThenInclude(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.Doctor != null && s.Doctor.UserId == userId)
                : await _context.Sessions.Include(s => s.Doctor)
                    .ThenInclude(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                    .Include(s => s.Doctor).ThenInclude(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.PatientId == userId);
        }

        // ── Cancel ──

        public async Task<(bool Success, string Message)> CancelSessionAsync(int sessionId, string userId, bool isDoctor)
        {
            var session = await GetSessionForUser(sessionId, userId, isDoctor);

            if (session == null)
                return (false, "Session not found.");

            if (session.SessionStatus == SessionStatus.Cancelled)
                return (false, "This session is already cancelled.");

            if (session.StartTime <= DateTimeOffset.UtcNow.AddHours(24))
                return (false, "Sessions can only be cancelled more than 24 hours in advance.");

            session.SessionStatus = SessionStatus.Cancelled;
            if (session.PaymentStatus == PaymentStatus.Paid)
                session.PaymentStatus = PaymentStatus.Refunded;

            await _context.SaveChangesAsync();
            return (true, "Session cancelled successfully.");
        }

        // ── Confirm (doctor only) ──

        public async Task<(bool Success, string Message)> ConfirmSessionAsync(int sessionId, string doctorUserId)
        {
            var session = await _context.Sessions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Doctor != null && s.Doctor.UserId == doctorUserId);

            if (session == null)
                return (false, "Session not found or you don't have permission to confirm it.");

            if (session.SessionStatus == SessionStatus.Confirmed)
                return (false, "This session has already been confirmed.");

            session.SessionStatus = SessionStatus.Confirmed;
            await _context.SaveChangesAsync();
            return (true, "Session confirmed successfully.");
        }

        // ── Complete (doctor only) ──

        public async Task<(bool Success, string Message)> CompleteSessionAsync(int sessionId, string doctorUserId)
        {
            var session = await _context.Sessions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Doctor != null && s.Doctor.UserId == doctorUserId);

            if (session == null)
                return (false, "Session not found or you don't have permission to update it.");

            if (session.StartTime >= DateTime.UtcNow)
                return (false, "Only past sessions can be marked as completed.");

            if (session.SessionStatus == SessionStatus.Completed)
                return (false, "This session is already completed.");

            if (session.SessionStatus == SessionStatus.Cancelled)
                return (false, "Cancelled sessions cannot be marked as completed.");

            session.SessionStatus = SessionStatus.Completed;
            await _context.SaveChangesAsync();
            return (true, "Session marked as completed.");
        }

        // ── Payment ──

        public async Task<Session?> GetPayableSession(int sessionId, string patientUserId)
        {
            return await _context.Sessions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.PatientId == patientUserId);
        }

        public async Task<(bool Success, string Message)> ProcessPaymentAsync(int sessionId, string patientUserId)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.PatientId == patientUserId);

            if (session == null)
                return (false, "Session not found.");

            if (session.PaymentStatus == PaymentStatus.Paid)
                return (false, "This session has already been paid.");

            session.PaymentStatus = PaymentStatus.Paid;
            await _context.SaveChangesAsync();
            return (true, "Payment successful! Your session is now confirmed.");
        }

        // ── Postpone ──

        public async Task<(bool Success, string Message, NormalizedSlot? NewSlot)> ConfirmPostponeAsync(
            int oldSessionId, int doctorId, string? selectedSlotsJson, string userId, bool isDoctor)
        {
            var oldSession = isDoctor
                ? await _context.Sessions.Include(s => s.Doctor).Include(s => s.Contact)
                    .FirstOrDefaultAsync(s => s.Id == oldSessionId && s.Doctor != null && s.Doctor.UserId == userId)
                : await _context.Sessions.Include(s => s.Doctor).Include(s => s.Contact)
                    .FirstOrDefaultAsync(s => s.Id == oldSessionId && s.PatientId == userId);

            if (oldSession == null)
                return (false, "Original session not found.", null);

            if (oldSession.StartTime <= DateTimeOffset.UtcNow.AddHours(24))
                return (false, "Sessions can only be postponed more than 24 hours in advance.", null);

            if (!_slotsService.TryParseSlots(selectedSlotsJson, out _, out var slots, out var error) || slots == null || slots.Count == 0)
                return (false, error ?? "Please select a new time slot.", null);

            var doctor = await _bookSessionService.GetSelectedDoctor(doctorId);
            if (doctor == null)
                return (false, "Doctor not found.", null);

            var newSlotVm = slots.First();
            var normalizedSlots = _slotsService.NormalizeSlots(new[] { newSlotVm }, doctor.SessionDuration);
            var newSlot = normalizedSlots.First();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                oldSession.SessionStatus = SessionStatus.Cancelled;

                _context.Sessions.Add(new Session
                {
                    DoctorId = doctor.Id,
                    PatientId = oldSession.PatientId,
                    StartTime = newSlot.StartTime,
                    EndTime = newSlot.EndTime,
                    Price = doctor.Price,
                    SessionStatus = SessionStatus.Scheduled,
                    PaymentStatus = oldSession.PaymentStatus,
                    ContactId = oldSession.ContactId
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return (true, $"Session rescheduled to {newSlot.StartTime:ddd, MMM d} at {newSlot.StartTime:HH:mm}.", newSlot);
        }
    }
}