using Data.Enums;

namespace SafeMind.Models
{
    public class DailyCheckViewModel
    {
        public DateTimeOffset CreatedOn { get; set; }
        public JournalMood Mood { get; set; }
        public EnergyLevel Energy { get; set; }
        public StressLevel Stress { get; set; }
        public SleepQuality Sleep { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}