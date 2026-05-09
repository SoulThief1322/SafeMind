using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SafeMind.Data;
using SafeMind.Data.Models;
namespace SafeMind.Services
{
    public class BookService(SafeMindDbContext context)
    {
        public Task<IQueryable<Doctor>> GetDoctors()
        {
            var doctors = context.Doctors.Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
            .AsSplitQuery()
            .AsNoTracking()
            .AsQueryable();
            return Task.FromResult(doctors);
        }
        public Task<IQueryable<Doctor>> DoctorsWithSpecialty(string specialty)
        {
            var doctors = context.Doctors.Where(d =>
                d.DoctorSpecialties.Any(ds => ds.Specialty != null && ds.Specialty.Name == specialty));
                return Task.FromResult(doctors);
        }
        public Task<IQueryable<Doctor>> DoctorsWithName(string name)
        {
            var doctors = context.Doctors.Where(d => d.Name.Contains(name));
            return Task.FromResult(doctors);
        }
        public IQueryable<Doctor> FilterBySpecialty(IQueryable<Doctor> query, string specialty)
        {
            return query.Where(d =>
                d.DoctorSpecialties.Any(ds => ds.Specialty != null && ds.Specialty.Name == specialty));
        }
        public IQueryable<Doctor> FilterByName(IQueryable<Doctor> query, string name)
        {
            return query
                .Where(d => d.Name.Contains(name))
                .OrderBy(d => d.Name.StartsWith(name) ? 0 : 1)
                .ThenBy(d => d.Name);
        }
        public Task<IQueryable<Doctor>> GetPageDoctors(IQueryable<Doctor> doctorsQuery, int page, int pageSize)
        {
            // Preserve existing ordering (e.g. starts-with priority from FilterByName) if already ordered,
            // otherwise fall back to alphabetical.
            var doctors = doctorsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
            return Task.FromResult(doctors);
        }
        public Task<IQueryable<string?>> GetSpecialties()
        {
            var specialties = context.Doctors
            .AsNoTracking()
            .SelectMany(d => d.DoctorSpecialties)
            .Where(ds => ds.Specialty != null)
            .Select(ds => ds.Specialty!.Name)
            .OrderBy(name => name)
            .AsQueryable();
            return Task.FromResult<IQueryable<string?>>(specialties);
        }
    }
}