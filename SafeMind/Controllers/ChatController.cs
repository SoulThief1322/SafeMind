using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly SafeMindDbContext _safeMindDbContext;

        public ChatController(SafeMindDbContext safeMindDbContext)
        {
            _safeMindDbContext = safeMindDbContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
                return Unauthorized();

            var conversations = await _safeMindDbContext.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToListAsync();

            return Json(conversations);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetMyDoctors()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null)
                return Unauthorized();

            var doctors = await _safeMindDbContext.Sessions
                .Where(s => s.PatientId == currentUserId)
                .Select(s => s.Doctor)
                .Distinct()
                .Select(d => new
                {
                    d.UserId,
                    d.Name
                })
                .ToListAsync();

            return Json(doctors);
        }
    }
}