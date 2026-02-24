namespace SafeMind.Models
{
    public class FeaturedArticleViewModel
    {
        public string Topic { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Eyebrow { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Cta { get; set; } = "Read article";
        public int ArticleId { get; set; }
    }
}
