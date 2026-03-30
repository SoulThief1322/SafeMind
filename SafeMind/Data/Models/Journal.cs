using Microsoft.AspNetCore.Identity;
using SafeMind.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeMind.Data.Models
{
    public class Journal
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public JournalMood Mood { get; set; }
        [Required]
        public JournalCategories Category { get; set; }

        public IdentityUser User { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;

    }
}