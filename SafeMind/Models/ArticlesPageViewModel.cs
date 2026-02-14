namespace SafeMind.Models
{
    public class ArticlesPageViewModel
    {
        public List<ArticlesViewModel> Articles { get; set; } = new();
        public List<FeaturedArticleViewModel> FeaturedArticles { get; set; } = new();
    }
}
