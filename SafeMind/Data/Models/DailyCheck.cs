using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Enums;
using Microsoft.AspNetCore.Identity;
namespace Data.Models
{
    public class DailyCheck
    {
        public int Id { get; set; }
        [Required]
        public DateTimeOffset CreatedOn { get; set; } = DateTime.UtcNow;
        [Required]
        public JournalMood Mood { get; set; }
        [Required]
        public EnergyLevel Energy { get; set; }
        [Required]
        public StressLevel Stress { get; set; }
        [Required]
        public SleepQuality Sleep { get; set; }
        [Required]
        public string Notes { get; set; } = string.Empty;
        public IdentityUser User { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;

    }
}