using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Data.Models
{
    public class DoctorLicense
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression("^[0-9]{10}$", ErrorMessage = "License number must be exactly 10 digits.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^[0-9]{10}$", ErrorMessage = "National ID must be exactly 10 digits.")]
        public string NationalId { get; set; } = string.Empty;

        [MaxLength(120)]
        public string IssuingAuthority { get; set; } = "Medical Board";

        public DateTime IssuedOn { get; set; }
        public DateTime ExpiresOn { get; set; }

        public ICollection<LicenceDoctorSpecialty> DoctorLicenseSpecialties { get; set; }
            = new List<LicenceDoctorSpecialty>();

        [MaxLength(40)]
        public string Status { get; set; } = "Active";
    }
}
