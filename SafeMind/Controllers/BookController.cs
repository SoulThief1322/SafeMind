using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;
using System.Globalization;

namespace SafeMind.Controllers;

[Authorize]
public class BookController : Controller
{
    private readonly ILogger<BookController> _logger;
    private SafeMindDbContext _context;

    public BookController(ILogger<BookController> logger, SafeMindDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? specialty, string? name, int page = 1)
    {
        var hasSearched = true;
        const int pageSize = 5;

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

        var totalDoctors = await doctorQuery.CountAsync();
        page = Math.Max(1, page);

        var totalPages = totalDoctors == 0
            ? 0
            : (int)Math.Ceiling(totalDoctors / (double)pageSize);

        if (totalPages == 0)
        {
            page = 1;
        }
        else if (page > totalPages)
        {
            page = totalPages;
        }

        var doctors = await doctorQuery
            .OrderBy(doctor => doctor.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var specialties = await _context.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .ToListAsync();

        var vm = new BookPageViewModel
        {
            Doctors = doctors.Select(doctor => new DoctorViewModel
            {
                Id = doctor.Id,
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
            HasSearched = hasSearched,
            PageNumber = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };
        return View(vm);
    }
    [HttpGet("/Book/BookAppointment/{id:int?}")]
    [AllowAnonymous]
    public async Task<IActionResult> BookAppointment(int? id, DateOnly? date)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Challenge();
        }

        if (!id.HasValue || id.Value <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id.Value);

        if (doctor == null)
        {
            return NotFound();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dayStart = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var sessionsForDay = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.DoctorId == id.Value)
            .Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd)
            .Select(s => TimeOnly.FromDateTime(s.StartTime.DateTime))
            .ToListAsync();

        var availableSlots = BuildSlots(doctor, selectedDate, sessionsForDay);

        var vm = new AppointmentViewModel
        {
            DoctorId = doctor.Id,
            DoctorName = doctor.Name,
            Biography = doctor.Biography,
            Specialties = doctor.DoctorSpecialties.Select(ds => ds.Specialty?.Name ?? string.Empty).Where(n => !string.IsNullOrWhiteSpace(n)),
            Languages = doctor.DoctorLanguages.Select(dl => dl.Language?.Name ?? string.Empty).Where(n => !string.IsNullOrWhiteSpace(n)),
            Price = doctor.Price,
            SessionDuration = doctor.SessionDuration,
            Rating = doctor.Rating,
            AvailabilityRange = $"{doctor.WorkStart:HH\\:mm} - {doctor.WorkEnd:HH\\:mm}",
            Initials = GetInitials(doctor.Name),
            SelectedDate = selectedDate,
            AvailableSlots = availableSlots
        };

        return View(vm);
    }

    [HttpGet("/Book/AvailableSessions")]
    [AllowAnonymous]
    public async Task<IActionResult> AvailableSessions(int id, DateOnly date)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            return Challenge();
        }

        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
        {
            return NotFound();
        }

        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var sessionsForDay = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.DoctorId == id)
            .Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd)
            .Select(s => TimeOnly.FromDateTime(s.StartTime.DateTime))
            .ToListAsync();

        var slots = BuildSlots(doctor, date, sessionsForDay);

        return Json(new
        {
            date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            label = date.ToString("dddd, MMM d", CultureInfo.InvariantCulture),
            slots
        });
    }

    private static IReadOnlyCollection<string> BuildSlots(Doctor doctor, DateOnly date, IEnumerable<TimeOnly> bookedTimes)
    {
        var booked = new HashSet<TimeOnly>(bookedTimes);
        var slots = new List<string>();

        var start = date.ToDateTime(doctor.WorkStart);
        var end = date.ToDateTime(doctor.WorkEnd);
        var duration = TimeSpan.FromMinutes(doctor.SessionDuration);

        for (var current = start; current.Add(duration) <= end; current = current.Add(duration))
        {
            var currentTime = TimeOnly.FromDateTime(current);
            if (!booked.Contains(currentTime))
            {
                slots.Add(current.ToString("HH:mm"));
            }
        }

        return slots;
    }

    private static string GetInitials(string name)
    {
        return string.Join(string.Empty,
            (name ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0])));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
