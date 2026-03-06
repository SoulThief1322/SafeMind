using System.ComponentModel.DataAnnotations;

namespace Data.Models
{
    public class GoalTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
    }
}
