using System.Diagnostics;
using System.Security.Claims;
using SafeMind.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;

namespace SafeMind.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SafeMindDbContext _context;

    public HomeController(ILogger<HomeController> logger, SafeMindDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var recentArticles = await _context.Articles
            .Where(a => !a.IsDeleted)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .OrderByDescending(a => a.PublishedOn)
            .Take(6)
            .ToListAsync();
        var viewModel = new HomePageViewModel
        {
            RecentArticles = recentArticles
        };
        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TermsOfService()
    {
        return View();
    }

    public IActionResult AboutUs()
    {
        return View();
    }

    public IActionResult HipaaCompliance()
    {
        return View();
    }

    public IActionResult CrisisResources()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("/Error/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodeError(int statusCode)
    {
        var vm = new ErrorViewModel
        {
            StatusCode = statusCode,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        return statusCode switch
        {
            403 => View("Error403", vm),
            404 => View("Error404", vm),
            _ => View("Error500", vm)
        };
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMood()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var mood = await _context.MoodChecks
            .Where(m => m.UserId == userId && m.SavedAt >= cutoff)
            .OrderByDescending(m => m.SavedAt)
            .Select(m => m.Mood)
            .FirstOrDefaultAsync();

        return Json(new { mood });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SaveMood([FromBody] SaveMoodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Mood)) return BadRequest();

        var allowed = new HashSet<string> { "Great", "Okay", "Not great" };
        if (!allowed.Contains(request.Mood)) return BadRequest();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var existing = await _context.MoodChecks
            .Where(m => m.UserId == userId && m.SavedAt >= cutoff)
            .OrderByDescending(m => m.SavedAt)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Mood = request.Mood;
            existing.SavedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _context.MoodChecks.Add(new MoodCheck
            {
                UserId = userId,
                Mood = request.Mood,
                SavedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ResetMood()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var recent = await _context.MoodChecks
            .Where(m => m.UserId == userId && m.SavedAt >= cutoff)
            .ToListAsync();

        _context.MoodChecks.RemoveRange(recent);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
