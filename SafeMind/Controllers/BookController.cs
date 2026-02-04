using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;
using Data.Enums;

namespace SafeMind.Controllers;

[Authorize]
public class BookController : Controller
{
    private readonly ILogger<BookController> _logger;
    private readonly SafeMindDbContext _context;

    public BookController(ILogger<BookController> logger, SafeMindDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? specialty, string? name, int page = 1)
    {
        const int pageSize = 5;

        var doctorQuery = _context.Doctors
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            doctorQuery = doctorQuery.Where(d =>
                d.DoctorSpecialties.Any(ds => ds.Specialty != null && ds.Specialty.Name == specialty));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            doctorQuery = doctorQuery.Where(d => d.Name.Contains(name));
        }

        var totalDoctors = await doctorQuery.CountAsync();
        page = Math.Max(1, page);

        var totalPages = totalDoctors == 0
            ? 0
            : (int)Math.Ceiling(totalDoctors / (double)pageSize);

        page = totalPages == 0 ? 1 : Math.Min(page, totalPages);

        var doctors = await doctorQuery
            .OrderBy(d => d.Name)
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
            Doctors = doctors.Select(d => new DoctorViewModel
            {
                Id = d.Id,
                Name = d.Name,
                Specialties = d.DoctorSpecialties
                    .Where(ds => ds.Specialty != null)
                    .Select(ds => ds.Specialty!.Name),
                Languages = d.DoctorLanguages
                    .Where(dl => dl.Language != null)
                    .Select(dl => dl.Language!.Name),
                SessionDuration = d.SessionDuration,
                Price = d.Price,
                WorkStart = d.WorkStart,
                WorkEnd = d.WorkEnd,
                Rating = d.Rating,
                Biography = d.Biography
            }).ToList(),
            Specialties = specialties,
            SelectedSpecialty = specialty ?? string.Empty,
            SearchName = name ?? string.Empty,
            PageNumber = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            HasSearched = true
        };

        return View(vm);
    }

    [HttpGet("/Book/BookAppointment/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> BookAppointment(int id, DateOnly? date)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Challenge();

        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound();

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dayStart = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var bookedTimes = await _context.Sessions
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == id &&
                s.StartTime >= dayStart &&
                s.StartTime < dayEnd &&
                s.SessionStatus != SessionStatus.Cancelled)
            .Select(s => TimeOnly.FromDateTime(s.StartTime.DateTime))
            .ToListAsync();

        var availableSlots = BuildSlots(doctor, selectedDate, bookedTimes);

        var vm = new AppointmentViewModel
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
            return Challenge();

        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound();

        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var bookedTimes = await _context.Sessions
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == id &&
                s.StartTime >= dayStart &&
                s.StartTime < dayEnd &&
                s.SessionStatus != SessionStatus.Cancelled)
            .Select(s => TimeOnly.FromDateTime(s.StartTime.DateTime))
            .ToListAsync();

        var slots = BuildSlots(doctor, date, bookedTimes);

        return Json(new
        {
            date = date.ToString("yyyy-MM-dd"),
            label = date.ToString("dddd, MMM d"),
            slots
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int doctorId, string? selectedSlotsJson)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Challenge();

        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        if (doctor == null)
            return NotFound();

        if (!TryParseSlots(selectedSlotsJson, out var slots, out var error))
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(BookAppointment), new { id = doctorId });
        }

        return View(new CheckoutViewModel
        {
            DoctorId = doctorId,
            DoctorName = doctor.Name,
            SessionPrice = doctor.Price,
            SessionDuration = doctor.SessionDuration,
            Slots = slots!
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel model)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Challenge();

        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == model.DoctorId);

        if (doctor == null)
            return NotFound();

        if (!ModelState.IsValid || model.Slots == null || model.Slots.Count == 0)
        {
            model.FullName = doctor.Name;
            model.SessionPrice = doctor.Price;
            model.SessionDuration = doctor.SessionDuration;
            return View("Checkout", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var normalizedSlots = NormalizeSlots(model.Slots, doctor.SessionDuration);
        var requestedStarts = normalizedSlots.Select(s => s.StartTime).ToList();

        var conflicts = await _context.Sessions
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == doctor.Id &&
                requestedStarts.Contains(s.StartTime) &&
                s.SessionStatus != SessionStatus.Cancelled)
            .AnyAsync();

        if (conflicts)
        {
            ModelState.AddModelError(string.Empty, "One or more selected slots were just booked.");
            model.FullName = doctor.Name;
            model.SessionPrice = doctor.Price;
            model.SessionDuration = doctor.SessionDuration;
            return View("Checkout", model);
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var contact = new SessionContact
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
            };

            _context.SessionContacts.Add(contact);

            foreach (var slot in normalizedSlots)
            {
                _context.Sessions.Add(new Session
                {
                    DoctorId = doctor.Id,
                    PatientId = userId,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Price = doctor.Price,
                    SessionStatus = SessionStatus.Scheduled,
                    PaymentStatus = model.PaymentStatus,
                    Contact = contact
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var redirectUrl = Url.Action(nameof(Confirmation),
            new { doctorId = doctor.Id, count = normalizedSlots.Count })
            ?? Url.Action(nameof(Index))
            ?? "/";

        return View("Processing", new PaymentProcessingViewModel
        {
            RedirectUrl = redirectUrl,
            DelayMs = 4000
        });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int doctorId, int count)
    {
        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        var vm = new ConfirmationViewModel
        {
            DoctorName = doctor?.Name ?? "Doctor",
            SessionCount = count
        };

        return View(vm);
    }

    // ===================== Helpers =====================

    private static bool TryParseSlots(string? rawJson, out List<SlotVM>? slots, out string error)
    {
        slots = null;
        error = "Please select at least one session.";

        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<List<SlotInput>>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed == null || parsed.Count == 0)
                return false;

            slots = parsed
                .Where(p => DateTime.TryParse(p.Date, out _) && TimeSpan.TryParse(p.Time, out _))
                .Select(p => new SlotVM
                {
                    Date = DateTime.Parse(p.Date!).Date,
                    Time = TimeSpan.Parse(p.Time!)
                })
                .DistinctBy(s => new { s.Date, s.Time })
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Time)
                .ToList();

            return slots.Count > 0;
        }
        catch
        {
            error = "Invalid slot selection.";
            return false;
        }
    }

    private static List<NormalizedSlot> NormalizeSlots(IEnumerable<SlotVM> slots, int durationMinutes)
    {
        return slots.Select(s =>
        {
            var startUtc = DateTime.SpecifyKind(s.Date + s.Time, DateTimeKind.Utc);
            var start = new DateTimeOffset(startUtc);

            return new NormalizedSlot
            {
                StartTime = start,
                EndTime = start.AddMinutes(durationMinutes)
            };
        }).ToList();
    }

    private static IReadOnlyCollection<string> BuildSlots(
        Doctor doctor,
        DateOnly date,
        IEnumerable<TimeOnly> bookedTimes)
    {
        var booked = new HashSet<TimeOnly>(bookedTimes);
        var slots = new List<string>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentTimeUtc = TimeOnly.FromDateTime(DateTime.UtcNow);

        var start = date.ToDateTime(doctor.WorkStart);
        var end = date.ToDateTime(doctor.WorkEnd);
        var duration = TimeSpan.FromMinutes(doctor.SessionDuration);

        for (var current = start; current.Add(duration) <= end; current = current.Add(duration))
        {
            var time = TimeOnly.FromDateTime(current);
            if (date == today && time <= currentTimeUtc)
                continue;
            if (!booked.Contains(time))
                slots.Add(current.ToString("HH:mm"));
        }

        return slots;
    }

    private static string GetInitials(string name) =>
        string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => char.ToUpperInvariant(p[0])));

    private sealed class SlotInput
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
    }

    private sealed class NormalizedSlot
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}