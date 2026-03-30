using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using SafeMind.Data.Models;
using Microsoft.AspNetCore.Identity;
using SafeMind.Data.Enums;
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
    private readonly GoalService _goalService;


    public MyDiaryController(ILogger<MyDiaryController> logger, SafeMindDbContext context, UserManager<IdentityUser> userManager, DiaryService diaryService, GoalService goalService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _diaryService = diaryService;
        _goalService = goalService;
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

        var weeklyGoals = await _goalService.GetOrCreateWeeklyGoalsAsync(userId);
        var totalGoalsCompleted = await _goalService.GetTotalCompletedAsync(userId);
        
        var vm = new DiaryPageViewModel
        {
            Journals = DiaryMapper.ToViewModels(journals),
            CheckIns = DiaryMapper.ToViewModels(checks),
            Insights = DiaryMapper.ToViewModel(
                totalJournals: journals.Count(),
                totalCheckIns: checks.Count(),
                totalGoals: totalGoalsCompleted,
                averageMoodScore: avgMood,
                moodDistribution: moodDistribution,
                dayStreak: streak
            ),
            HasTodayCheck = hasTodayCheck,
            WeeklyGoals = weeklyGoals.Select(w => new WeeklyGoalItem
            {
                Id = w.Id,
                Description = w.GoalTemplate.Description,
                IsCompleted = w.IsCompleted
            }).ToList(),
            TotalGoalsCompleted = totalGoalsCompleted
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

    [HttpGet]
    public async Task<IActionResult> GetEntryDates(int year, int month)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var journalDates = await _context.Journals
            .Where(j => j.UserId == userId && j.CreatedAt >= startDate && j.CreatedAt < endDate)
            .Select(j => j.CreatedAt.Date)
            .Distinct()
            .ToListAsync();

        var checkDates = await _context.DailyChecks
            .Where(c => c.UserId == userId && c.CreatedOn >= startDate && c.CreatedOn < endDate)
            .Select(c => c.CreatedOn.Date)
            .Distinct()
            .ToListAsync();

        return Json(new
        {
            journalDates = journalDates.Select(d => d.ToString("yyyy-MM-dd")),
            checkDates = checkDates.Select(d => d.ToString("yyyy-MM-dd"))
        });
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

        [HttpGet]
        public async Task<IActionResult> AllEntries()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var journals = await _diaryService.GetJournals(_context, userId);
            var checks = await _diaryService.GetChecks(_context, userId);
            var insights = DiaryMapper.ToViewModel(
                totalJournals: journals.Count(),
                totalCheckIns: checks.Count(),
                totalGoals: 0,
                averageMoodScore: 0,
                moodDistribution: new Dictionary<string, int>(),
                dayStreak: 0
            );
            var vm = new DiaryPageViewModel
            {
                Journals = DiaryMapper.ToViewModels(journals),
                CheckIns = DiaryMapper.ToViewModels(checks),
                Insights = insights
            };
            return View(vm);
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteGoal([FromBody] CompleteGoalRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var success = await _goalService.CompleteGoalAsync(userId, request.WeeklyGoalId);
        if (!success)
            return Conflict(new { success = false, error = "Already completed." });

        var total = await _goalService.GetTotalCompletedAsync(userId);
        return Json(new { success = true, totalCompleted = total });
    }
}

public class CompleteGoalRequest
{
    public int WeeklyGoalId { get; set; }
}
