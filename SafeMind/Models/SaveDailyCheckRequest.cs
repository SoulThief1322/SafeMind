using System.ComponentModel.DataAnnotations;
using Data.Enums;

namespace SafeMind.Models
{
    public class SaveDailyCheckRequest
    {
        [Required]
        public JournalMood Mood { get; set; }

        [Required]
        public EnergyLevel Energy { get; set; }

        [Required]
        public StressLevel Stress { get; set; }

        [Required]
        public SleepQuality Sleep { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
