
using SafeMind.Data.Models;
using SafeMind.Models;

namespace SafeMind.Services
{
    public static class DoctorMapper
{
    public static DoctorViewModel ToViewModel(Doctor doctor)
    {
        return new DoctorViewModel
        {
            Id = doctor.Id,
            Name = doctor.Name,
            Specialties = doctor.DoctorSpecialties
                .Where(ds => ds.Specialty != null)
                .Select(ds => ds.Specialty!.Name)
                .ToList(),

            Languages = doctor.DoctorLanguages
                .Where(dl => dl.Language != null)
                .Select(dl => dl.Language!.Name)
                .ToList(),

            SessionDuration = doctor.SessionDuration,
            Price = doctor.Price,
            WorkStart = doctor.WorkStart,
            WorkEnd = doctor.WorkEnd,
            Rating = doctor.Rating,
            Biography = doctor.Biography
        };
    }
}
}