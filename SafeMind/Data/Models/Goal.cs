using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SafeMind.Data.Constants;

namespace SafeMind.Data.Models
{
    public class Goal
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(GoalConstants.DescriptionMaxLength)]
        [MinLength(GoalConstants.DescriptionMinLength)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset TargetDate { get; set; } = DateTime.UtcNow;
        [Required]
        public bool IsCompleted { get; set; } = false;
        public IdentityUser User { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
    }
}