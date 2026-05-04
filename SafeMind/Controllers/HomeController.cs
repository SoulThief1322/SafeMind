using System.Diagnostics;
using System.Security.Claims;
using SafeMind.Data.Models;
using SafeMind.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using SafeMind.Services;

namespace SafeMind.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly DiaryService _diaryService;

    public HomeController(ILogger<HomeController> logger, SafeMindDbContext context, DiaryService diaryService)
    {
        _logger = logger;
        _context = context;
        _diaryService = diaryService;
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

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

            var recentJournals = await _context.Journals
                .Where(j => j.UserId == userId && j.CreatedAt >= cutoff)
                .ToListAsync();

            var allChecks = await _context.DailyChecks
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedOn)
                .ToListAsync();

            var recentChecks = allChecks.Where(c => c.CreatedOn >= cutoff).ToList();
            var totalEntries = recentJournals.Count + recentChecks.Count;

            if (totalEntries > 0)
            {
                var allMoods = recentJournals.Select(j => j.Mood)
                    .Concat(recentChecks.Select(c => c.Mood))
                    .ToList();

                var dominantMood = allMoods
                    .GroupBy(m => m)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                var avgScore = allMoods.Average(m => _diaryService.MapMoodScore(m));
                var streak = await _diaryService.CalculateStreak(allChecks);

                viewModel.InsightDominantMood = dominantMood.ToString();
                viewModel.InsightStreak = streak;
                viewModel.InsightAvgScore = Math.Round(avgScore, 1);
                viewModel.InsightTotalEntries = totalEntries;
                viewModel.RecommendedArticles = await GetRecommendedArticlesAsync(MoodToCategories(dominantMood), 3);
            }
            else
            {
                viewModel.RecommendedArticles = await GetRecommendedArticlesAsync(
                    new[] { "Wellness", "Mind", "Insights" }, 3);
            }
        }

        return View(viewModel);
    }

    // Called via AJAX for unauthenticated users after they pick a mood from the cookie widget
    [HttpGet]
    public async Task<IActionResult> GetArticleRecommendations(string mood)
    {
        var categories = GuestMoodToCategories(mood);
        var articles = await GetRecommendedArticlesAsync(categories, 3);
        return Json(articles.Select(a => new
        {
            a.Id,
            a.Headline,
            a.ImagePath,
            Content = a.Content.Length > 120 ? a.Content[..120] + "..." : a.Content,
            Category = a.ArticleCategories.FirstOrDefault()?.Category?.Name
        }));
    }

    private static string[] MoodToCategories(JournalMood mood) => mood switch
    {
        JournalMood.Happy or JournalMood.Excited => new[] { "Wellness", "Mind", "Insights" },
        JournalMood.Calm                          => new[] { "Mind", "Sleep", "Wellness" },
        JournalMood.Anxious                       => new[] { "Anxiety", "Stress", "Mind" },
        JournalMood.Sad                           => new[] { "Therapy", "Wellness", "Mind" },
        JournalMood.Angry                         => new[] { "Stress", "Mind", "Therapy" },
        _                                         => new[] { "Wellness", "Mind", "Insights" }
    };

    private static string[] GuestMoodToCategories(string mood) => mood switch
    {
        "Great"     => new[] { "Wellness", "Mind", "Insights" },
        "Okay"      => new[] { "Mind", "Sleep", "Wellness" },
        "Not great" => new[] { "Anxiety", "Stress", "Therapy" },
        _           => new[] { "Wellness", "Mind", "Insights" }
    };

    private async Task<List<Article>> GetRecommendedArticlesAsync(string[] preferredCategories, int count)
    {
        var articles = await _context.Articles
            .Where(a => !a.IsDeleted &&
                        a.ArticleCategories.Any(ac => preferredCategories.Contains(ac.Category.Name)))
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .OrderByDescending(a => a.ViewCount)
            .Take(count)
            .ToListAsync();

        if (articles.Count < count)
        {
            var existingIds = articles.Select(a => a.Id).ToList();
            var fallback = await _context.Articles
                .Where(a => !a.IsDeleted && !existingIds.Contains(a.Id))
                .Include(a => a.ArticleCategories)
                    .ThenInclude(ac => ac.Category)
                .OrderByDescending(a => a.ViewCount)
                .Take(count - articles.Count)
                .ToListAsync();
            articles.AddRange(fallback);
        }

        return articles;
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
