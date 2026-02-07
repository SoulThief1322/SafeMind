using Data.Models;
using SafeMind.Models;

namespace SafeMind.Services
{
    public static class DiaryMapper
    {
        public static JournalViewModel ToViewModel(Journal journal)
        {
            return new JournalViewModel
            {
                CreatedOn = journal.CreatedAt,
                Mood = journal.Mood,
                Title = journal.Title,
                Category = journal.Category,
                Content = journal.Content
            };
        }

        public static DailyCheckViewModel ToViewModel(DailyCheck check)
        {
            return new DailyCheckViewModel
            {
                CreatedOn = check.CreatedOn,
                Mood = check.Mood,
                Energy = check.Energy,
                Stress = check.Stress,
                Sleep = check.Sleep,
                Notes = check.Notes
            };
        }
        public static InsightsViewModel ToViewModel(
            int totalJournals,
            int totalCheckIns,
            int totalGoals,
            double? averageMoodScore,
            Dictionary<string, int> moodDistribution,
            int dayStreak)
        {
            return new InsightsViewModel
            {
                TotalJournals = totalJournals,
                TotalCheckIns = totalCheckIns,
                TotalGoals = totalGoals,
                AverageMoodScore = averageMoodScore,
                MoodDistribution = moodDistribution,
                DayStreak = dayStreak
            };
        }

        public static IEnumerable<JournalViewModel> ToViewModels(IEnumerable<Journal> journals)
        {
            return journals.Select(ToViewModel);
        }

        public static IEnumerable<DailyCheckViewModel> ToViewModels(IEnumerable<DailyCheck> checks)
        {
            return checks.Select(ToViewModel);
        }

        public static Journal ToEntity(NewJournalEntryRequest request, string userId)
        {
            return new Journal
            {
                UserId = userId,
                Title = request.Title?.Trim() ?? string.Empty,
                Category = request.Category,
                Mood = request.Mood,
                Content = request.Content?.Trim() ?? string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public static DailyCheck ToEntity(SaveDailyCheckRequest request, string userId)
        {
            return new DailyCheck
            {
                UserId = userId,
                Mood = request.Mood,
                Energy = request.Energy,
                Stress = request.Stress,
                Sleep = request.Sleep,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedOn = DateTimeOffset.UtcNow
            };
        }
    }
}
