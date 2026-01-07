using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Constants;
using Data.Enums;
using Microsoft.AspNetCore.Identity;

namespace Data.Models
{
    public class Session
    {
        public int Id { get; set; }
        [Required]
        public DateTimeOffset StartTime { get; set; }
        [Required]
        public DateTimeOffset EndTime { get; set; }
        public Doctor Doctor { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Doctor))]
        public int DoctorId { get; set; }

        public IdentityUser Patient { get; set; } = null!;
        [Required]
        [ForeignKey(nameof(Patient))]
        public string PatientId { get; set; } = null!;

        [MaxLength(SessionConstants.NotesAndPrescriptionMaxLength)]
        [MinLength(SessionConstants.NotesAndPrescriptionMinLength)]
        public string? Notes { get; set; }
        [MaxLength(SessionConstants.NotesAndPrescriptionMaxLength)]
        [MinLength(SessionConstants.NotesAndPrescriptionMinLength)]
        public string? Prescription { get; set; }
        [Required]
        public DateTimeOffset TimeOfBooking { get; set; } = DateTime.UtcNow;
        [Required]
        [Range(SessionConstants.PriceMinValue, SessionConstants.PriceMaxValue)]
        public decimal Price { get; set; }
        public SessionStatus SessionStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        [Range(GeneralConstants.RatingMinNumber, GeneralConstants.RatingMaxNumber)]
        public decimal? Rating { get; set; }
        
    }
}