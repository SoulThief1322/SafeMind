using System.ComponentModel.DataAnnotations;

namespace Data.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTimeOffset SubmittedOn { get; set; } = DateTimeOffset.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsArchived { get; set; } = false;
    }
}
