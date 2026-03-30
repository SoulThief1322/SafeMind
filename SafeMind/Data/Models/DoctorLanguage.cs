using System.ComponentModel.DataAnnotations.Schema;

namespace SafeMind.Data.Models
{
    public class DoctorLanguage
    {
        public Doctor Doctor { get; set; } = null!;
        [ForeignKey(nameof(Doctor))]
        public int DoctorId { get; set; }

        public Language Language { get; set; } = null!;
        [ForeignKey(nameof(Language))]
        public int LanguageId { get; set; }
    }
}