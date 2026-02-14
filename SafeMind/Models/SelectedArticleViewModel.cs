using Data.Models;

namespace SafeMind.Models
{
    public class SelectedArticleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTimeOffset DateOfPublish { get; set; }
        public int ViewCount { get; set; }
        public int ViewsInLastWeek { get; set; }
        public int Likes { get; set; }
        public string imagePath { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}