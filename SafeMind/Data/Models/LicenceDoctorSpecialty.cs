using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeMind.Data.Models
{
    public class LicenceDoctorSpecialty
    {

        public DoctorLicense DoctorLicense { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(DoctorLicense))]
        public int DoctorLicenseId { get; set; }

        public LicenceSpecialty Specialty { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Specialty))]
        public int SpecialtyId { get; set; }

    }
}