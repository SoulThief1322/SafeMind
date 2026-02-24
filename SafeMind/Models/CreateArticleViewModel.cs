using System.ComponentModel.DataAnnotations;

namespace SafeMind.Models
{
    public class CreateArticleViewModel
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Headline { get; set; } = string.Empty;

        [Required]
        [StringLength(10000, MinimumLength = 10)]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Article Image")]
        public IFormFile? Image { get; set; }

        [Display(Name = "Categories")]
        public List<int> SelectedCategoryIds { get; set; } = new();

        public List<CategoryOptionViewModel> AvailableCategories { get; set; } = new();
    }

    public class CategoryOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
