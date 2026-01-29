namespace SafeMind.Models
{
    public class PaymentProcessingViewModel
    {
        public string RedirectUrl { get; set; } = "/";
        public int DelayMs { get; set; } = 2000;
    }
}
