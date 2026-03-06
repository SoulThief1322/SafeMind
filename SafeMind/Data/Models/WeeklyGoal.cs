using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class WeeklyGoal
    {
        public int Id { get; set; }

        [Required]
        public int GoalTemplateId { get; set; }

        [ForeignKey(nameof(GoalTemplateId))]
        public GoalTemplate GoalTemplate { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = null!;

        [Required]
        public DateTime WeekStart { get; set; }

        public bool IsCompleted { get; set; }
    }
}
