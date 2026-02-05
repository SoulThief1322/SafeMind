using System.Linq;
using Data.Models;
using SafeMind.Models;

namespace SafeMind.Services;

public static class SessionMapper
{
    public static SessionsViewModel ToViewModel(Doctor doctor, DateOnly selectedDate, IReadOnlyCollection<string> availableSlots)
    {
        return new SessionsViewModel
        {
            DoctorId = doctor.Id,
            DoctorName = doctor.Name,
            Biography = doctor.Biography,
            Specialties = doctor.DoctorSpecialties.Select(ds => ds.Specialty?.Name ?? string.Empty),
            Languages = doctor.DoctorLanguages.Select(dl => dl.Language?.Name ?? string.Empty),
            Price = doctor.Price,
            SessionDuration = doctor.SessionDuration,
            Rating = doctor.Rating,
            AvailabilityRange = $"{doctor.WorkStart:HH\\:mm} - {doctor.WorkEnd:HH\\:mm}",
            SelectedDate = selectedDate,
            AvailableSlots = availableSlots
        };
    }
}
