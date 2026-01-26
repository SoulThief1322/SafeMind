using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeMind.Models
{
    public class DoctorViewModel
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<string> Specialties { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Languages { get; set; } = Array.Empty<string>();
        public int SessionDuration { get; set; }
        public decimal Price { get; set; }
        public TimeOnly WorkStart { get; set; }
        public TimeOnly WorkEnd { get; set; }
        public decimal Rating { get; set; }
        public string Biography { get; set; } = string.Empty;

        public string Initials => string.Join(string.Empty,
            Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0])));

        public string AvailabilityRange => $"{WorkStart:HH\\:mm} - {WorkEnd:HH\\:mm}";

    }
}