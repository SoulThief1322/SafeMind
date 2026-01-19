using System.Collections.Generic;

namespace SafeMind.Models
{
    public class DiaryPageViewModel
    {
        public IEnumerable<JournalViewModel> Journals { get; set; } = new List<JournalViewModel>();
        public IEnumerable<DailyCheckViewModel> CheckIns { get; set; } = new List<DailyCheckViewModel>();
        public InsightsViewModel Insights { get; set; } = new InsightsViewModel();
    }
}
