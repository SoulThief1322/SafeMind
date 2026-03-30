using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SafeMind.Data.Models
{
    public class DoctorSpecialty
    {
        public Doctor Doctor { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Doctor))]
        public int DoctorId { get; set; }
        public Specialty Specialty { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Specialty))]
        public int SpecialtyId { get; set; }
    }
}