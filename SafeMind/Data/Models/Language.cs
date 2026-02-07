using System.ComponentModel.DataAnnotations;
using Data.Constants;
namespace Data.Models
{
    public class Language
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(GeneralConstants.NameMaxLength)]
        [MinLength(GeneralConstants.NameMinLength)]
        public string Name { get; set; } = string.Empty;
        public ICollection<DoctorLanguage> DoctorLanguages { get; set; }
            = new List<DoctorLanguage>();
    }
}