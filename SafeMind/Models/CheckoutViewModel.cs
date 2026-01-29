using System.ComponentModel.DataAnnotations;

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
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public decimal TotalPrice => Slots.Count * SessionPrice;
    }

    public class SlotVM
    {
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
    }
}