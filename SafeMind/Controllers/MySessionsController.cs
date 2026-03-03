using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Enums;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using SafeMind.Services;
using static SafeMind.Services.SlotsService;
namespace SafeMind.Controllers;

public class MySessionsController : Controller
{
    private readonly ILogger<MySessionsController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly MySessionService _mySessionService;
    private readonly BookSessionService _bookSessionService;
    private readonly SlotsService _slotsService;

    public MySessionsController(ILogger<MySessionsController> logger, SafeMindDbContext context, MySessionService mySessionService, BookSessionService bookSessionService, SlotsService slotsService)
    {
        _logger = logger;
        _context = context;
        _mySessionService = mySessionService;
        _bookSessionService = bookSessionService;
        _slotsService = slotsService;
    }
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isDoctor = User.IsInRole("Doctor");

        var sessions = await _mySessionService.GetSessions(_context, userId, isDoctor);

        var now = DateTime.UtcNow;

        var upcoming = sessions
            .Where(m => m.SessionDate.ToDateTime(m.SessionTime) >= now)
            .OrderBy(m => m.SessionDate.ToDateTime(m.SessionTime))
            .ToList();

        var unpaid = isDoctor ? new List<MySessionsViewModel>() : upcoming
            .Where(m => m.PaymentStatus != PaymentStatus.Paid)
            .OrderBy(m => m.SessionDate.ToDateTime(m.SessionTime))
            .ToList();

        var past = sessions
            .Where(m => m.SessionDate.ToDateTime(m.SessionTime) < now)
            .OrderByDescending(m => m.SessionDate.ToDateTime(m.SessionTime))
            .ToList();

        var paidCount = upcoming.Count(m => m.PaymentStatus == PaymentStatus.Paid);
        var progressPercent = upcoming.Count == 0 ? 0 : (int)Math.Round((double)paidCount / upcoming.Count * 100);

        var viewModel = new MySessionsPageViewModel
        {
            Upcoming = upcoming,
            Unpaid = unpaid,
            Past = past,
            PaidCount = paidCount,
            ProgressPercent = progressPercent
        };

        return View(viewModel);
    }
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Payment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var session = await _context.Sessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == id && s.PatientId == userId);

        if (session == null)
            return NotFound();

        if (session.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["Error"] = "This session has already been paid.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new PaymentViewModel
        {
            SessionId = session.Id,
            TotalAmount = session.Price
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Payment", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == model.SessionId && s.PatientId == userId);

        if (session == null)
            return NotFound();

        if (session.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["Error"] = "This session has already been paid.";
            return RedirectToAction(nameof(Index));
        }
        session.PaymentStatus = PaymentStatus.Paid;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Payment successful! Your session is now confirmed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var session = await _context.Sessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == id && s.Doctor != null && s.Doctor.UserId == userId);

        if (session == null)
        {
            TempData["Error"] = "Session not found or you don't have permission to confirm it.";
            return RedirectToAction(nameof(Index));
        }

        if (session.SessionStatus == SessionStatus.Confirmed)
        {
            TempData["Error"] = "This session has already been confirmed.";
            return RedirectToAction(nameof(Index));
        }

        session.SessionStatus = SessionStatus.Confirmed;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Session confirmed successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var session = await _context.Sessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == id && s.Doctor != null && s.Doctor.UserId == userId);

        if (session == null)
        {
            TempData["Error"] = "Session not found or you don't have permission to update it.";
            return RedirectToAction(nameof(Index));
        }

        if (session.StartTime >= DateTime.UtcNow)
        {
            TempData["Error"] = "Only past sessions can be marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        if (session.SessionStatus == SessionStatus.Completed)
        {
            TempData["Error"] = "This session is already completed.";
            return RedirectToAction(nameof(Index));
        }

        if (session.SessionStatus == SessionStatus.Cancelled)
        {
            TempData["Error"] = "Cancelled sessions cannot be marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        session.SessionStatus = SessionStatus.Completed;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Session marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isDoctor = User.IsInRole("Doctor");

        var session = isDoctor
            ? await _context.Sessions.Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id && s.Doctor != null && s.Doctor.UserId == userId)
            : await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && s.PatientId == userId);

        if (session == null)
        {
            TempData["Error"] = "Session not found.";
            return RedirectToAction(nameof(Index));
        }

        if (session.SessionStatus == SessionStatus.Cancelled)
        {
            TempData["Error"] = "This session is already cancelled.";
            return RedirectToAction(nameof(Index));
        }

        if (session.StartTime <= DateTimeOffset.UtcNow.AddHours(24))
        {
            TempData["Error"] = "Sessions can only be cancelled more than 24 hours in advance.";
            return RedirectToAction(nameof(Index));
        }

        session.SessionStatus = SessionStatus.Cancelled;
        if (session.PaymentStatus == PaymentStatus.Paid)
        {
            session.PaymentStatus = PaymentStatus.Refunded;
        }
        await _context.SaveChangesAsync();

        TempData["Success"] = "Session cancelled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Postpone(int id, DateOnly? date)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isDoctor = User.IsInRole("Doctor");

        var session = isDoctor
            ? await _context.Sessions.Include(s => s.Doctor)
                .ThenInclude(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                .Include(s => s.Doctor).ThenInclude(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
                .FirstOrDefaultAsync(s => s.Id == id && s.Doctor != null && s.Doctor.UserId == userId)
            : await _context.Sessions.Include(s => s.Doctor)
                .ThenInclude(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
                .Include(s => s.Doctor).ThenInclude(d => d.DoctorLanguages).ThenInclude(dl => dl.Language)
                .FirstOrDefaultAsync(s => s.Id == id && s.PatientId == userId);

        if (session == null)
        {
            TempData["Error"] = "Session not found.";
            return RedirectToAction(nameof(Index));
        }

        if (session.SessionStatus == SessionStatus.Cancelled || session.SessionStatus == SessionStatus.Completed)
        {
            TempData["Error"] = "This session cannot be postponed.";
            return RedirectToAction(nameof(Index));
        }

        if (session.StartTime <= DateTimeOffset.UtcNow.AddHours(24))
        {
            TempData["Error"] = "Sessions can only be postponed more than 24 hours in advance.";
            return RedirectToAction(nameof(Index));
        }

        var doctor = session.Doctor;
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dayStart = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var bookedTimes = await _bookSessionService.GetTakenSessions(dayStart, dayEnd, doctor.Id);
        var availableSlots = _slotsService.BuildSlots(doctor, selectedDate, bookedTimes);

        var vm = SessionMapper.ToViewModel(doctor, selectedDate, availableSlots);

        ViewBag.PostponeSessionId = id;
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPostpone(int oldSessionId, int doctorId, string? selectedSlotsJson)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isDoctor = User.IsInRole("Doctor");

        // Validate old session
        var oldSession = isDoctor
            ? await _context.Sessions.Include(s => s.Doctor).Include(s => s.Contact)
                .FirstOrDefaultAsync(s => s.Id == oldSessionId && s.Doctor != null && s.Doctor.UserId == userId)
            : await _context.Sessions.Include(s => s.Doctor).Include(s => s.Contact)
                .FirstOrDefaultAsync(s => s.Id == oldSessionId && s.PatientId == userId);

        if (oldSession == null)
        {
            TempData["Error"] = "Original session not found.";
            return RedirectToAction(nameof(Index));
        }

        if (oldSession.StartTime <= DateTimeOffset.UtcNow.AddHours(24))
        {
            TempData["Error"] = "Sessions can only be postponed more than 24 hours in advance.";
            return RedirectToAction(nameof(Index));
        }

        // Parse the selected slot
        if (!_slotsService.TryParseSlots(selectedSlotsJson, out _, out var slots, out var error) || slots == null || slots.Count == 0)
        {
            TempData["Error"] = error ?? "Please select a new time slot.";
            return RedirectToAction(nameof(Postpone), new { id = oldSessionId });
        }

        var doctor = await _bookSessionService.GetSelectedDoctor(doctorId);
        if (doctor == null)
        {
            TempData["Error"] = "Doctor not found.";
            return RedirectToAction(nameof(Index));
        }

        // Only allow picking one slot for postpone
        var newSlotVm = slots.First();
        var normalizedSlots = _slotsService.NormalizeSlots(new[] { newSlotVm }, doctor.SessionDuration);
        var newSlot = normalizedSlots.First();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Cancel the old session
            oldSession.SessionStatus = SessionStatus.Cancelled;

            // Create new session preserving payment status and contact
            var newSession = new Session
            {
                DoctorId = doctor.Id,
                PatientId = oldSession.PatientId,
                StartTime = newSlot.StartTime,
                EndTime = newSlot.EndTime,
                Price = doctor.Price,
                SessionStatus = SessionStatus.Scheduled,
                PaymentStatus = oldSession.PaymentStatus,
                ContactId = oldSession.ContactId
            };
            _context.Sessions.Add(newSession);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        TempData["Success"] = $"Session rescheduled to {newSlot.StartTime:ddd, MMM d} at {newSlot.StartTime:HH:mm}.";
        return RedirectToAction(nameof(Index));
    }
}
