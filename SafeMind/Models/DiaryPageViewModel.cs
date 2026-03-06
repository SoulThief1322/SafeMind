using System.Collections.Generic;

namespace SafeMind.Models
{
    public class DiaryPageViewModel
    {
        public IEnumerable<JournalViewModel> Journals { get; set; } = new List<JournalViewModel>();
        public IEnumerable<DailyCheckViewModel> CheckIns { get; set; } = new List<DailyCheckViewModel>();
        public InsightsViewModel Insights { get; set; } = new InsightsViewModel();
        public bool HasTodayCheck { get; set; }
        public List<WeeklyGoalItem> WeeklyGoals { get; set; } = new();
        public int TotalGoalsCompleted { get; set; }
    }

    public class WeeklyGoalItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}
