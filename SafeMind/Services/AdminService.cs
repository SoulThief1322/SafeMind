using Data.Models;
using Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Services
{
    public class AdminService
    {
        private readonly SafeMindDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminService(SafeMindDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ──────── Dashboard ────────

        public async Task<int> GetTotalUsersAsync()
            => await _userManager.Users.CountAsync();

        public async Task<int> GetTotalDoctorsAsync()
            => await _context.Doctors.CountAsync();

        public async Task<int> GetTotalSessionsAsync()
            => await _context.Sessions.CountAsync();

        public async Task<int> GetTotalArticlesAsync()
            => await _context.Articles.CountAsync(a => !a.IsDeleted);

        public async Task<decimal> GetTotalRevenueAsync()
            => await _context.Sessions
                .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(s => s.Price);

        public async Task<int> GetUnreadContactCountAsync()
            => await _context.ContactMessages.CountAsync(c => !c.IsRead && !c.IsArchived);

        public async Task<int> GetCompletedSessionsCountAsync()
            => await _context.Sessions.CountAsync(s => s.SessionStatus == SessionStatus.Completed);

        public async Task<List<Session>> GetRecentSessionsAsync(int count = 5)
            => await _context.Sessions
                .Include(s => s.Doctor)
                .Include(s => s.Patient)
                .OrderByDescending(s => s.TimeOfBooking)
                .Take(count)
                .ToListAsync();

        // ──────── Contacts ────────

        public async Task<List<ContactMessage>> GetContactMessagesAsync(bool includeArchived = false)
        {
            var query = _context.ContactMessages.AsQueryable();
            if (!includeArchived)
                query = query.Where(c => !c.IsArchived);
            return await query.OrderByDescending(c => c.SubmittedOn).ToListAsync();
        }

        public async Task<ContactMessage?> GetContactMessageAsync(int id)
            => await _context.ContactMessages.FindAsync(id);

        public async Task MarkContactAsReadAsync(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null && !msg.IsRead)
            {
                msg.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ArchiveContactAsync(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                msg.IsArchived = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteContactAsync(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _context.ContactMessages.Remove(msg);
                await _context.SaveChangesAsync();
            }
        }

        // ──────── Users ────────

        public async Task<List<(IdentityUser User, IList<string> Roles)>> GetAllUsersWithRolesAsync()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var result = new List<(IdentityUser, IList<string>)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add((user, roles));
            }
            return result;
        }

        public async Task<bool> ToggleUserLockoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            return true;
        }

        // ──────── Goals ────────

        public async Task<List<GoalTemplate>> GetGoalTemplatesAsync()
            => await _context.GoalTemplates.OrderBy(g => g.Id).ToListAsync();

        public async Task AddGoalTemplateAsync(string description)
        {
            _context.GoalTemplates.Add(new GoalTemplate { Description = description });
            await _context.SaveChangesAsync();
        }

        public async Task UpdateGoalTemplateAsync(int id, string description)
        {
            var template = await _context.GoalTemplates.FindAsync(id);
            if (template != null)
            {
                template.Description = description;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteGoalTemplateAsync(int id)
        {
            var template = await _context.GoalTemplates.FindAsync(id);
            if (template != null)
            {
                _context.GoalTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }
        }

        // ──────── Reports ────────

        public async Task<Dictionary<string, int>> GetSessionsByStatusAsync()
        {
            var sessions = await _context.Sessions.ToListAsync();
            return sessions
                .GroupBy(s => s.SessionStatus)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
        }

        public async Task<Dictionary<string, int>> GetSessionsPerMonthAsync(int months = 6)
        {
            var since = DateTimeOffset.UtcNow.AddMonths(-months);
            var sessions = await _context.Sessions
                .Where(s => s.StartTime >= since)
                .ToListAsync();

            return sessions
                .GroupBy(s => s.StartTime.ToString("MMM yyyy"))
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<Dictionary<string, decimal>> GetRevenuePerMonthAsync(int months = 6)
        {
            var since = DateTimeOffset.UtcNow.AddMonths(-months);
            var sessions = await _context.Sessions
                .Where(s => s.PaymentStatus == PaymentStatus.Paid && s.StartTime >= since)
                .ToListAsync();

            return sessions
                .GroupBy(s => s.StartTime.ToString("MMM yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Price));
        }

        public async Task<int> GetNewUsersThisMonthAsync()
        {
            // Approximate via recent sessions with unique patients
            var since = DateTimeOffset.UtcNow.AddDays(-30);
            return await _context.Sessions
                .Where(s => s.TimeOfBooking >= since)
                .Select(s => s.PatientId)
                .Distinct()
                .CountAsync();
        }
    }
}
