using SafeMind.Data.Models;

namespace SafeMind.Models
{
    public class HomePageViewModel
    {
        public List<Article> RecentArticles { get; set; } = new();

        // Mood-based article recommendations
        public List<Article> RecommendedArticles { get; set; } = new();

        // History insight data (populated for authenticated users with recent diary activity)
        public string? InsightDominantMood { get; set; }
        public int InsightStreak { get; set; }
        public double InsightAvgScore { get; set; }
        public int InsightTotalEntries { get; set; }
    }
}