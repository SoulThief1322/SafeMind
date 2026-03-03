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
            Doctor = DoctorMapper.ToViewModel(doctor),
            SelectedDate = selectedDate,
            AvailableSlots = availableSlots
        };
    }
}