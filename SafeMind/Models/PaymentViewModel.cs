using System.ComponentModel.DataAnnotations;
using SafeMind.Attributes;

namespace SafeMind.Models
{
    public class PaymentViewModel
    {
        public int DoctorId { get; set; }
        public int SessionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ContactId { get; set; } = string.Empty;
        public int? SessionId { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^([0-9]{4} ?){4}$", ErrorMessage = "Card number is incorrect")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cardholder name is required")]
        [RegularExpression(@"^[A-Z ]{2,26}$", ErrorMessage = "Cardholder name must be 2-26 characters, uppercase letters and spaces only")]
        [Display(Name = "Cardholder Name")]
        public string CardholderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry date must be in MM/YY format")]
        [FutureDate(ErrorMessage = "Card has expired")]
        [Display(Name = "Expiry Date (MM/YY)")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits")]
        [Display(Name = "CVV")]
        public string CVV { get; set; } = string.Empty;
    }
}
