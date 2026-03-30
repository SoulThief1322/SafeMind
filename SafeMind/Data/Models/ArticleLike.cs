using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SafeMind.Data.Models
{
    public class ArticleLike
    {
        public Article Article { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Article))]
        public int ArticleId { get; set; }

        public IdentityUser User { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;
    }
}
