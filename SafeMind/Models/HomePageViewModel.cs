using SafeMind.Data.Models;

namespace SafeMind.Models
{
    public class HomePageViewModel
    {
        public List<Article> RecentArticles { get; set; } = new();
    }
}