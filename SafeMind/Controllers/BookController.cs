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
using SafeMind.Services;
namespace SafeMind.Controllers;

[Authorize]
public class BookController : Controller
{
    private readonly ILogger<BookController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly BookService _bookService;
    private readonly BookSessionService _bookSessionService;
    private readonly SlotsService _slotsService;
    private readonly ConfirmService _confirmService;

    public BookController(ILogger<BookController> logger, SafeMindDbContext context, BookService bookService, BookSessionService bookSessionService, SlotsService slotsService, ConfirmService confirmService)
    {
        _logger = logger;
        _context = context;
        _bookService = bookService;
        _bookSessionService = bookSessionService;
        _slotsService = slotsService;
        _confirmService = confirmService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? specialty, string? name, int page = 1)
    {
        const int pageSize = 5;

        var doctorQuery = await _bookService.GetDoctors();

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            doctorQuery = await _bookService.DoctorsWithSpecialty(specialty);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            doctorQuery = await _bookService.DoctorsWithName(name);
        }

        var totalDoctors = await doctorQuery.CountAsync();
        page = Math.Max(1, page);

        var totalPages = totalDoctors == 0
            ? 0
            : (int)Math.Ceiling(totalDoctors / (double)pageSize);

        page = totalPages == 0 ? 1 : Math.Min(page, totalPages);

        var doctors = await _bookService.GetPageDoctors(doctorQuery, page, pageSize);

        var specialties = await _bookService.GetSpecialties();

        var vm = new BookPageViewModel
        {
            Doctors = doctors
        .Select(DoctorMapper.ToViewModel)
        .ToList(),

            Specialties = specialties.Where(s => s != null).Select(s => s!).ToList(),
            SelectedSpecialty = specialty ?? string.Empty,
            SearchName = name ?? string.Empty,
            PageNumber = page,
            TotalPages = totalPages,
            PageSize = pageSize,
        };


        return View(vm);
    }

    [HttpGet("/Book/BookSession/{id:int}")]
    [Authorize]
    public async Task<IActionResult> BookSession(int id, DateOnly? date)
    {
        var doctor = await _bookSessionService.GetSelectedDoctor(id);

        if (doctor == null)
            return NotFound();

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dayStart = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var bookedTimes = await _bookSessionService.GetTakenSessions(dayStart, dayEnd, doctor.Id);

        var availableSlots = _slotsService.BuildSlots(doctor, selectedDate, bookedTimes);

        var vm = SessionMapper.ToViewModel(doctor, selectedDate, availableSlots);

        return View(vm);
    }

    [HttpGet("/Book/AvailableSessions")]
    [Authorize]
    public async Task<IActionResult> AvailableSessions(int id, DateOnly date)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Challenge();

        var doctor = await _bookSessionService.GetSelectedDoctor(id);

        if (doctor == null)
            return NotFound();

        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var bookedTimes = await _bookSessionService.GetTakenSessions(dayStart, dayEnd, doctor.Id);
            

        var slots = _slotsService.BuildSlots(doctor, date, bookedTimes);

        return Json(new
        {
            date = date.ToString("yyyy-MM-dd"),
            label = date.ToString("dddd, MMM d"),
            slots
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Checkout(int doctorId, string? selectedSlotsJson)
    {

        if (!_slotsService.TryParseSlots(selectedSlotsJson, out var payloadDoctorId, out var slots, out var error))
        {
            var fallbackDoctorId = doctorId != 0 ? doctorId : payloadDoctorId;
            TempData["Error"] = error;
            return RedirectToAction(nameof(BookSession), new { id = fallbackDoctorId });
        }

        var effectiveDoctorId = doctorId != 0 ? doctorId : payloadDoctorId;

        if (doctorId != 0 && payloadDoctorId != 0 && doctorId != payloadDoctorId)
            return BadRequest("Doctor mismatch.");

        if (effectiveDoctorId == 0)
            return BadRequest("Missing doctor.");

        var doctor = await _bookSessionService.GetSelectedDoctor(effectiveDoctorId);

        if (doctor == null)
            return NotFound();

        return View(new CheckoutViewModel
        {
            DoctorId = doctor.Id,
            DoctorName = doctor.Name,
            SessionPrice = doctor.Price,
            SessionDuration = doctor.SessionDuration,
            Slots = slots!
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel model)
    {

        if (!Enum.IsDefined(typeof(PaymentStatus), model.PaymentStatus))
            model.PaymentStatus = PaymentStatus.Pending;

        var doctor = await _bookSessionService.GetSelectedDoctor(model.DoctorId);

        if (doctor == null)
            return NotFound();

        if (!ModelState.IsValid || model.Slots == null || model.Slots.Count == 0)
        {
            model.DoctorId = doctor.Id;
            model.DoctorName = doctor.Name;
            model.SessionPrice = doctor.Price;
            model.SessionDuration = doctor.SessionDuration;
            if (model.PaymentStatus == default)
                model.PaymentStatus = PaymentStatus.Pending;
            model.Slots ??= new List<SlotVM>();
            return View("Checkout", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var normalizedSlots = _slotsService.NormalizeSlots(model.Slots, doctor.SessionDuration);
        var requestedStarts = normalizedSlots.Select(s => s.StartTime).ToList();

        var conflicts = await _confirmService.GetConflicts(doctor, requestedStarts);

        if (conflicts)
        {
            ModelState.AddModelError(string.Empty, "One or more selected slots were just booked.");
            model.DoctorId = doctor.Id;
            model.DoctorName = doctor.Name;
            model.SessionPrice = doctor.Price;
            model.SessionDuration = doctor.SessionDuration;
            if (model.PaymentStatus == default)
                model.PaymentStatus = PaymentStatus.Pending;
            return View("Checkout", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

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
                await _confirmService.AddSessionToDb(doctor, slot, userId, model.PaymentStatus, contact);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
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
    [Authorize]
    public async Task<IActionResult> Confirmation(int doctorId, int count)
    {
        var doctor = await _bookSessionService.GetSelectedDoctor(doctorId);

        var vm = new ConfirmationViewModel
        {
            DoctorName = doctor?.Name ?? "Doctor",
            SessionCount = count
        };

        return View(vm);
    }

}