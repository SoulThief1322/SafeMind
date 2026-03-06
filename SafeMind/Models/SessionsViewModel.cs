using System;
using System.Collections.Generic;

namespace SafeMind.Models
{
    public class SessionsViewModel
    {
        public DoctorViewModel Doctor { get; set; } = new();
        public DateOnly SelectedDate { get; set; }
        public IReadOnlyCollection<string> AvailableSlots { get; set; } = Array.Empty<string>();
    }
}