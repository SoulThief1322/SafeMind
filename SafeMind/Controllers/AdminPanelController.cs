using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Services;

namespace SafeMind.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPanelController : Controller
    {
        private readonly AdminService _adminService;

        public AdminPanelController(AdminService adminService)
        {
            _adminService = adminService;
        }

        // ──────── Dashboard ────────

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _adminService.GetTotalUsersAsync();
            ViewBag.TotalDoctors = await _adminService.GetTotalDoctorsAsync();
            ViewBag.TotalSessions = await _adminService.GetTotalSessionsAsync();
            ViewBag.TotalArticles = await _adminService.GetTotalArticlesAsync();
            ViewBag.TotalRevenue = await _adminService.GetTotalRevenueAsync();
            ViewBag.UnreadContacts = await _adminService.GetUnreadContactCountAsync();
            ViewBag.CompletedSessions = await _adminService.GetCompletedSessionsCountAsync();
            ViewBag.RecentSessions = await _adminService.GetRecentSessionsAsync();
            ViewBag.ActiveTab = "dashboard";
            return View();
        }

        // ──────── Contacts ────────

        public async Task<IActionResult> Contacts(bool showArchived = false)
        {
            var messages = await _adminService.GetContactMessagesAsync(showArchived);
            ViewBag.ShowArchived = showArchived;
            ViewBag.ActiveTab = "contacts";
            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkContactRead(int id)
        {
            await _adminService.MarkContactAsReadAsync(id);
            return RedirectToAction(nameof(Contacts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveContact(int id)
        {
            await _adminService.ArchiveContactAsync(id);
            return RedirectToAction(nameof(Contacts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(int id)
        {
            await _adminService.DeleteContactAsync(id);
            return RedirectToAction(nameof(Contacts));
        }

        // ──────── Users ────────

        public async Task<IActionResult> Users()
        {
            var usersWithRoles = await _adminService.GetAllUsersWithRolesAsync();
            ViewBag.ActiveTab = "users";
            return View(usersWithRoles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string userId)
        {
            await _adminService.ToggleUserLockoutAsync(userId);
            return RedirectToAction(nameof(Users));
        }

        // ──────── Goals ────────

        public async Task<IActionResult> Goals()
        {
            var templates = await _adminService.GetGoalTemplatesAsync();
            ViewBag.ActiveTab = "goals";
            return View(templates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGoal(string description)
        {
            if (!string.IsNullOrWhiteSpace(description) && description.Length <= 200)
            {
                await _adminService.AddGoalTemplateAsync(description);
            }
            return RedirectToAction(nameof(Goals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGoal(int id, string description)
        {
            if (!string.IsNullOrWhiteSpace(description) && description.Length <= 200)
            {
                await _adminService.UpdateGoalTemplateAsync(id, description);
            }
            return RedirectToAction(nameof(Goals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            await _adminService.DeleteGoalTemplateAsync(id);
            return RedirectToAction(nameof(Goals));
        }

        // ──────── Reports ────────

        public async Task<IActionResult> Reports()
        {
            ViewBag.SessionsByStatus = await _adminService.GetSessionsByStatusAsync();
            ViewBag.SessionsPerMonth = await _adminService.GetSessionsPerMonthAsync();
            ViewBag.RevenuePerMonth = await _adminService.GetRevenuePerMonthAsync();
            ViewBag.NewUsersThisMonth = await _adminService.GetNewUsersThisMonthAsync();
            ViewBag.TotalRevenue = await _adminService.GetTotalRevenueAsync();
            ViewBag.TotalSessions = await _adminService.GetTotalSessionsAsync();
            ViewBag.ActiveTab = "reports";
            return View();
        }
    }
}
