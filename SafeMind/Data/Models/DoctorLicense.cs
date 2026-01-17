using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Data.Models
{
    public class DoctorLicense
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string NationalId { get; set; } = string.Empty;

        [MaxLength(120)]
        public string IssuingAuthority { get; set; } = "Medical Board";

        public DateTime IssuedOn { get; set; }
        public DateTime ExpiresOn { get; set; }

        [MaxLength(120)]
        public string Specialty { get; set; } = string.Empty;

        [MaxLength(40)]
        public string Status { get; set; } = "Active";
    }
}
