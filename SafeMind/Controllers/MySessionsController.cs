using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Models;
using SafeMind.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using SafeMind.Services;
namespace SafeMind.Controllers;

[Authorize]
public class MySessionsController : Controller
{
    private readonly ILogger<MySessionsController> _logger;
    private readonly MySessionService _mySessionService;
    private readonly BookSessionService _bookSessionService;
    private readonly SlotsService _slotsService;
    private readonly RatingService _ratingService;

    public MySessionsController(ILogger<MySessionsController> logger, MySessionService mySessionService, BookSessionService bookSessionService, SlotsService slotsService, RatingService ratingService)
    {
        _logger = logger;
        _mySessionService = mySessionService;
        _bookSessionService = bookSessionService;
        _slotsService = slotsService;
        _ratingService = ratingService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isDoctor = User.IsInRole("Doctor");

        var sessions = await _mySessionService.GetSessions(userId, isDoctor);

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
    public async Task<IActionResult> Payment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _mySessionService.GetPayableSession(id, userId);

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Payment", model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (model.SessionId == null)
        {
            TempData["Error"] = "Session not found.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message) = await _mySessionService.ProcessPaymentAsync(model.SessionId.Value, userId);

        if (!success)
        {
            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, message) = await _mySessionService.ConfirmSessionAsync(id, userId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, message) = await _mySessionService.CompleteSessionAsync(id, userId);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isDoctor = User.IsInRole("Doctor");
        var (success, message) = await _mySessionService.CancelSessionAsync(id, userId, isDoctor);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Postpone(int id, DateOnly? date)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isDoctor = User.IsInRole("Doctor");

        var session = await _mySessionService.GetSessionWithDoctorDetails(id, userId, isDoctor);

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateSession([FromForm] int sessionId, [FromForm] int stars)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _ratingService.SubmitRatingAsync(sessionId, userId, stars);
        if (!success)
            return Json(new { success = false, error });
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPostpone(int oldSessionId, int doctorId, string? selectedSlotsJson)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isDoctor = User.IsInRole("Doctor");

        var (success, message, _) = await _mySessionService.ConfirmPostponeAsync(oldSessionId, doctorId, selectedSlotsJson, userId, isDoctor);

        if (!success)
        {
            TempData["Error"] = message;
            if (message.Contains("select"))
                return RedirectToAction(nameof(Postpone), new { id = oldSessionId });
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }
}
