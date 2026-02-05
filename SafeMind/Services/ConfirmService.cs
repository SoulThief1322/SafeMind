using Data.Enums;
using Data.Models;
using Microsoft.AspNetCore.Mvc;
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
        
    }
}