namespace SafeMind.Models
{
    public class ArticlesAndCategoriesViewModel
    {
        public List<ArticlesViewModel> Articles { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }
}