using System.ComponentModel.DataAnnotations;
using SafeMind.Data.Constants;
namespace SafeMind.Data.Models
{
    public class LicenceSpecialty
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(GeneralConstants.NameMaxLength)]
        [MinLength(GeneralConstants.NameMinLength)]
        public string Name { get; set; } = string.Empty;
        public ICollection<LicenceDoctorSpecialty> DoctorLicenceSpecialties { get; set; }
            = new List<LicenceDoctorSpecialty>();
    }
}