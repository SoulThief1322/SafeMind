using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;

namespace SafeMind.Controllers;

public class BookController : Controller
{
    private readonly ILogger<BookController> _logger;
    private SafeMindDbContext _context;

    public BookController(ILogger<BookController> logger, SafeMindDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index(string? specialty, string? name)
    {
        // Always show results; filters narrow the list when provided.
        var hasSearched = true;

        var doctorQuery = _context.Doctors
            .Include(doctor => doctor.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .Include(doctor => doctor.DoctorLanguages)
                .ThenInclude(dl => dl.Language)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            doctorQuery = doctorQuery.Where(doctor => doctor.DoctorSpecialties
                .Any(ds => ds.Specialty != null && ds.Specialty.Name == specialty));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            doctorQuery = doctorQuery.Where(doctor => doctor.Name.Contains(name));
        }

        var doctors = await doctorQuery.ToListAsync();

        var specialties = await _context.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .ToListAsync();

        var vm = new BookPageViewModel
        {
            Doctors = doctors.Select(doctor => new DoctorViewModel
            {
                Name = doctor.Name,
                Specialties = doctor.DoctorSpecialties
                    .Where(ds => ds.Specialty != null)
                    .Select(ds => ds.Specialty!.Name),
                Languages = doctor.DoctorLanguages
                    .Where(dl => dl.Language != null)
                    .Select(dl => dl.Language!.Name),
                SessionDuration = doctor.SessionDuration,
                Price = doctor.Price,
                WorkStart = doctor.WorkStart,
                WorkEnd = doctor.WorkEnd,
                Rating = doctor.Rating,
                Biography = doctor.Biography
            }).ToList(),
            Specialties = specialties,
            SelectedSpecialty = specialty ?? string.Empty,
            SearchName = name ?? string.Empty,
            HasSearched = hasSearched
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
