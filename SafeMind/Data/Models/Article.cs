using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class Article
    {
        public int Id { get; set; }
        [Required]
        public string Headline { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset PublishedOn { get; set; } = DateTime.UtcNow;
        public IdentityUser Author { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Author))]
        public string AuthorId { get; set; } = null!;
        public int ViewCount { get; set; } = 0;
        public int ViewsInLastWeek { get; set; } = 0;
        public int Likes { get; set; } = 0;
        public string? ImagePath { get; set; }
        public ICollection<ArticleCategory> ArticleCategories { get; set; }
            = new List<ArticleCategory>();
    }
}