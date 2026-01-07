using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Data.Constants;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace Data.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public IdentityUser User { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;
        [Required]
        [MaxLength(GeneralConstants.NameMaxLength)]
        [MinLength(GeneralConstants.NameMinLength)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public TimeOnly WorkStart { get; set; }
        [Required]
        public TimeOnly WorkEnd { get; set; }
        [Required]
        [Comment("Session duration in minutes")]
        public int SessionDuration { get; set; }
        [Required]
        [Range(DoctorConstants.RatingMinNumber, DoctorConstants.RatingMaxNumber)]
        public decimal Rating { get; set; }
        
        
    }
}