using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Enums;
using Microsoft.AspNetCore.Authorization;
using SafeMind.Services;
namespace SafeMind.Controllers;

public class MySessionsController : Controller
{
    private readonly ILogger<MySessionsController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly MySessionService _mySessionService;

    public MySessionsController(ILogger<MySessionsController> logger, SafeMindDbContext context, MySessionService mySessionService)
    {
        _logger = logger;
        _context = context;
        _mySessionService = mySessionService;
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

        // Update session payment status
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
