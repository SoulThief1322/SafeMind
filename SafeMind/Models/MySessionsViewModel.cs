using Data.Enums;

namespace SafeMind.Models
{
    public class MySessionsViewModel
    {
        public int SessionId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateOnly SessionDate { get; set; }
        public TimeOnly SessionTime { get; set; }
        public decimal SessionPrice { get; set; }
        public int SessionDuration { get; set; }
        public string ContactFullName { get; set; } = string.Empty;
        public PaymentStatus PaymentStatus { get; set; }
        public SessionStatus SessionStatus { get; set; }
        


    }
}