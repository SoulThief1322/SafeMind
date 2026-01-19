namespace SafeMind.Models
{
    public class InsightsViewModel
    {
        
        public int TotalJournals { get; set; }
        public int TotalCheckIns { get; set; }
        public int TotalGoals { get; set; }
        public double? AverageMoodScore { get; set; }
        public Dictionary<string, int> MoodDistribution { get; set; } = new Dictionary<string, int>();
        public int DayStreak { get; set; }

    }
}