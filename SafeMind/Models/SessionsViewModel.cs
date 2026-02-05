using System;
using System.Collections.Generic;

namespace SafeMind.Models
{
    public class SessionsViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public IEnumerable<string> Specialties { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Languages { get; set; } = Array.Empty<string>();
        public decimal Price { get; set; }
        public int SessionDuration { get; set; }
        public decimal Rating { get; set; }
        public string AvailabilityRange { get; set; } = string.Empty;
        public DateOnly SelectedDate { get; set; }
        public IReadOnlyCollection<string> AvailableSlots { get; set; } = Array.Empty<string>();
    }
}