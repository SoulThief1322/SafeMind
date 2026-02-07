using System.Linq;
using Data.Enums;
using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using static SafeMind.Services.SlotsService;

namespace SafeMind.Services
{
    public class ConfirmService(SafeMindDbContext context)
    {
        public Task<IActionResult> AddSessionToDb(Doctor doctor, NormalizedSlot slot, string userId, PaymentStatus paymentStatus, SessionContact contact)
        {
            context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id,
                PatientId = userId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Price = doctor.Price,
                SessionStatus = SessionStatus.Scheduled,
                PaymentStatus = paymentStatus,
                Contact = contact
            });
            context.SaveChanges();
            return Task.FromResult<IActionResult>(new OkResult());
        }
        public async Task<bool> GetConflicts(Doctor doctor, List<DateTimeOffset> requestedStarts)
        {
            var sessions = await
            context.Sessions
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == doctor.Id &&
                requestedStarts.Any(start => start == s.StartTime) &&
                s.SessionStatus != SessionStatus.Cancelled)
            .AnyAsync();
            return sessions;
        }

    }
}