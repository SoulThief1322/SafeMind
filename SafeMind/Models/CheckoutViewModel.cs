using System.ComponentModel.DataAnnotations;
    using SafeMind.Data.Enums;

namespace SafeMind.Models
{
    using System.ComponentModel.DataAnnotations;

    public class CheckoutViewModel
    {
        [Required]
        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public decimal SessionPrice { get; set; }

        public int SessionDuration { get; set; }

        [Required(ErrorMessage = "Please select at least one session.")]
        public List<SlotVM> Slots { get; set; } = new();
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Use 100 characters or fewer for your name.")]
        [RegularExpression("^^[A-Z][a-z]+ [A-Z][a-z]+$", ErrorMessage = "Please enter a valid full name (first and last name).")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public decimal TotalPrice => Slots.Count * SessionPrice;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    }

    public class SlotVM
    {
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
    }
}