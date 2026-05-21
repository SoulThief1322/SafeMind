using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Services;

namespace SafeMind.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        // ── Patient endpoints ──

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Route to doctor-specific method if in Doctor role
            if (User.IsInRole("Doctor"))
            {
                var doctorResult = await _chatService.GetDoctorConversationsAsync(currentUserId);
                return Json(doctorResult);
            }

            var result = await _chatService.GetConversationsAsync(currentUserId);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDoctors()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var doctors = await _chatService.GetMyDoctorsAsync(currentUserId);
            return Json(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int doctorId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var messages = await _chatService.GetMessagesAsync(currentUserId, doctorId);
            return Json(messages);
        }

        // ── Doctor endpoints ──

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMyPatients()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var patients = await _chatService.GetMyPatientsAsync(currentUserId);
            return Json(patients);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var count = await _chatService.GetUnreadCountAsync(currentUserId);
            return Json(new { count });
        }

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetPatientMessages(string patientId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var messages = await _chatService.GetDoctorMessagesAsync(currentUserId, patientId);
            return Json(messages);
        }
    }
}