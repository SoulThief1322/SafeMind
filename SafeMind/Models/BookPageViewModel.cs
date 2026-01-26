using Data.Models;

namespace SafeMind.Models
{
    public class BookPageViewModel
    {
        public IEnumerable<DoctorViewModel> Doctors { get; set; }
            = new List<DoctorViewModel>();

        public IEnumerable<string> Specialties { get; set; }
            = new List<string>();

        public string SelectedSpecialty { get; set; } = string.Empty;

        public string SearchName { get; set; } = string.Empty;

        public bool HasSearched { get; set; }
            = false;
    }
}