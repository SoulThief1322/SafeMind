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


        var sessions = await _mySessionService.GetSessions(_context, userId);

        return View(sessions);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
