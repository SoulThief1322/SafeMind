using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Constants;

namespace Data.Models
{
    public class Category
    {

        public int Id { get; set; }
        [Required]
        [MaxLength(GeneralConstants.NameMaxLength)]
        [MinLength(GeneralConstants.NameMinLength)]
        public string Name { get; set; } = string.Empty;
        public ICollection<ArticleCategories> ArticleCategories { get; set; }
            = new List<ArticleCategories>();

    }
}