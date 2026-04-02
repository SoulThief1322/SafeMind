using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SafeMind.Data.Constants;

namespace SafeMind.Data.Models
{
    public class SessionRating
    {
        [Required]
        public int SessionId { get; set; }
        [ForeignKey(nameof(SessionId))]
        public Session Session { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = null!;
        [ForeignKey(nameof(PatientId))]
        public IdentityUser Patient { get; set; } = null!;

        [Required]
        [Range(GeneralConstants.RatingMinNumber, GeneralConstants.RatingMaxNumber)]
        public int Stars { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
