using System.ComponentModel.DataAnnotations;
using Data.Constants;
using Data.Enums;

namespace Data.Models
{
    public class SessionContact
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(ContactsConstants.FullNameMaxLength)]
        [MinLength(ContactsConstants.FullNameMinLength)]
        public string FullName { get; set; } = null!;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}