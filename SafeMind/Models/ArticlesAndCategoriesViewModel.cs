namespace SafeMind.Models
{
    public class ArticlesAndCategoriesViewModel
    {
        public List<ArticlesViewModel> Articles { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}