using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    public class ArticleCategory
    {

        public Article Article { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Article))]
        public int ArticleId { get; set; }

        public Category Category { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

    }
}