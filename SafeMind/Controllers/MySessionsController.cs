using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Enums;
namespace SafeMind.Controllers;

public class MySessionsController : Controller
{
    private readonly ILogger<MySessionsController> _logger;
    private readonly SafeMindDbContext _context;

    public MySessionsController(ILogger<MySessionsController> logger, SafeMindDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.Doctor)
            .Include(s => s.Contact)
            .Where(s => s.PatientId == userId)
            .OrderBy(s => s.StartTime)
            .Select(s => new MySessionsViewModel
            {
                DoctorName = s.Doctor != null ? s.Doctor.Name : string.Empty,
                SessionDate = DateOnly.FromDateTime(s.StartTime.DateTime),
                SessionTime = TimeOnly.FromDateTime(s.StartTime.DateTime),
                SessionPrice = s.Price,
                SessionDuration = s.Doctor != null ? s.Doctor.SessionDuration : 0,
                ContactFullName = s.Contact != null ? s.Contact.FullName : string.Empty,
                PaymentStatus = s.PaymentStatus,
                SessionStatus = s.SessionStatus
            })
            .ToListAsync();

        return View(sessions);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
