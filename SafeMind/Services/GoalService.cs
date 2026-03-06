using Data.Models;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Services
{
    public class GoalService
    {
        private readonly SafeMindDbContext _context;

        public GoalService(SafeMindDbContext context)
        {
            _context = context;
        }

        private static DateTime GetWeekStart()
        {
            var today = DateTime.UtcNow.Date;
            int diff = ((int)today.DayOfWeek + 6) % 7;
            return today.AddDays(-diff);
        }

        public async Task<List<WeeklyGoal>> GetOrCreateWeeklyGoalsAsync(string userId)
        {
            var weekStart = GetWeekStart();

            var goals = await _context.WeeklyGoals
                .Include(w => w.GoalTemplate)
                .Where(w => w.UserId == userId && w.WeekStart == weekStart)
                .ToListAsync();

            if (goals.Count > 0)
                return goals;

            var templates = await _context.GoalTemplates
                .OrderBy(g => EF.Functions.Random())
                .Take(4)
                .ToListAsync();

            goals = templates.Select(t => new WeeklyGoal
            {
                UserId = userId,
                GoalTemplateId = t.Id,
                WeekStart = weekStart,
                IsCompleted = false
            }).ToList();

            _context.WeeklyGoals.AddRange(goals);
            await _context.SaveChangesAsync();

            return await _context.WeeklyGoals
                .Include(w => w.GoalTemplate)
                .Where(w => w.UserId == userId && w.WeekStart == weekStart)
                .ToListAsync();
        }

        public async Task<bool> CompleteGoalAsync(string userId, int weeklyGoalId)
        {
            var goal = await _context.WeeklyGoals
                .FirstOrDefaultAsync(w => w.Id == weeklyGoalId && w.UserId == userId);

            if (goal == null || goal.IsCompleted) return false;

            goal.IsCompleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalCompletedAsync(string userId)
        {
            return await _context.WeeklyGoals.CountAsync(w => w.UserId == userId && w.IsCompleted);
        }
    }
}
