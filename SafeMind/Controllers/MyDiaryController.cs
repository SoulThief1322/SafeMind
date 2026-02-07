using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Data.Enums;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using SafeMind.Services;

namespace SafeMind.Controllers;

[Authorize]

public class MyDiaryController : Controller
{
    private readonly ILogger<MyDiaryController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DiaryService _diaryService;


    public MyDiaryController(ILogger<MyDiaryController> logger, SafeMindDbContext context, UserManager<IdentityUser> userManager, DiaryService diaryService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _diaryService = diaryService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        var today = DateTimeOffset.UtcNow.Date;

        var journals = await _diaryService.GetJournals(_context, userId);
            

        var checks = await _diaryService.GetChecks(_context, userId);

        var hasTodayCheck = checks.Any(c => c.CreatedOn.Date == today);

        var moodDistribution = await _diaryService.GetMoodDistribution(journals, checks);

        var moodScoresList = await _diaryService.GetMoodScores(journals, _diaryService, checks);
        double? avgMood = moodScoresList.Any() ? moodScoresList.Average() : null;
        var streak = await _diaryService.CalculateStreak(checks);
        
        var vm = new DiaryPageViewModel
        {
            Journals = DiaryMapper.ToViewModels(journals),
            CheckIns = DiaryMapper.ToViewModels(checks),
            Insights = DiaryMapper.ToViewModel(
                totalJournals: journals.Count(),
                totalCheckIns: checks.Count(),
                totalGoals: _context.Goals.Count(g => g.UserId == userId),
                averageMoodScore: avgMood,
                moodDistribution: moodDistribution,
                dayStreak: streak
            ),
            HasTodayCheck = hasTodayCheck
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCheck([FromForm] SaveDailyCheckRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var today = DateTimeOffset.UtcNow.Date;
        var alreadyToday = await _context.DailyChecks.AnyAsync(c => c.UserId == userId && c.CreatedOn.Date == today);
        if (alreadyToday)
        {
            return Conflict(new { success = false, error = "You already checked in today." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid data" });
        }

        var check = DiaryMapper.ToEntity(request, userId);

        _context.DailyChecks.Add(check);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            id = check.Id,
            createdOn = check.CreatedOn,
            mood = check.Mood.ToString(),
            energy = check.Energy.ToString(),
            stress = check.Stress.ToString(),
            sleep = check.Sleep.ToString(),
            notes = check.Notes
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> NewEntry()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewEntry([FromForm] NewJournalEntryRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid data" });
        }

        var journal = DiaryMapper.ToEntity(request, userId);

        _context.Journals.Add(journal);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    

    


}
