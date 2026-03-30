using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SafeMind.Data.Models
{
    public class MoodCheck
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;
        public IdentityUser User { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Mood { get; set; } = string.Empty;

        public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
