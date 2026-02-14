namespace SafeMind.Models
{
    public class ArticlesAndCategoriesViewModel
    {
        public List<ArticlesViewModel> Articles { get; set; } = new();
        public CategoriesViewModel Categories { get; set; } = new CategoriesViewModel();
    }
}