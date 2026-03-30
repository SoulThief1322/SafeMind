
using SafeMind.Data;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;

namespace SafeMind.Services
{
    public class DiaryService
    {
        public Task<int> CalculateStreak(IEnumerable<DailyCheck> checks)
        {
            var distinctDates = checks
                .Select(c => c.CreatedOn.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!distinctDates.Any()) return Task.FromResult(0);

            var current = distinctDates[^1];
            var streak = 1;

            while (distinctDates.Contains(current.AddDays(-streak)))
            {
                streak++;
            }

            return Task.FromResult(streak);
        }
        public double MapMoodScore(JournalMood mood) => mood switch
        {
            JournalMood.Happy => 5.0,
            JournalMood.Excited => 4.5,
            JournalMood.Calm => 4.0,
            JournalMood.Anxious => 2.0,
            JournalMood.Sad => 1.5,
            JournalMood.Angry => 1.0,
            _ => 3.0
        };
        public Task<IQueryable<Journal>> GetJournals(SafeMindDbContext context, string userId)
        {
            var journals = context.Journals.Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);
            return Task.FromResult<IQueryable<Journal>>(journals);
        }
        public Task<IQueryable<DailyCheck>> GetChecks(SafeMindDbContext context, string userId)
        {
            var checks = context.DailyChecks.Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedOn);
            return Task.FromResult<IQueryable<DailyCheck>>(checks);
        }
        public Task<Dictionary<string, int>> GetMoodDistribution(IQueryable<Journal> journals, IQueryable<DailyCheck> checks)
        {
            var journalMoods = journals.Select(j => j.Mood).ToList();
            var checkMoods = checks.Select(c => c.Mood).ToList();
            
            var moodDistribution = journalMoods
                .Concat(checkMoods)
                .GroupBy(m => m.ToString())
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(moodDistribution);
        }
        public Task<List<double>> GetMoodScores(IQueryable<Journal> journals, DiaryService diaryService, IQueryable<DailyCheck> checks)
        {
            var journalScores = journals.Select(j => j.Mood).ToList().Select(m => diaryService.MapMoodScore(m));
            var checkScores = checks.Select(c => c.Mood).ToList().Select(m => diaryService.MapMoodScore(m));
            
            var score = journalScores
                .Concat(checkScores)
                .ToList();
            return Task.FromResult(score);
        }
    }
}