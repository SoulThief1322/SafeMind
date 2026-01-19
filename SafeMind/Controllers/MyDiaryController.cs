using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Data.Enums;
using System.Linq;

namespace SafeMind.Controllers;

public class MyDiaryController : Controller
{
    private readonly ILogger<MyDiaryController> _logger;
    private readonly SafeMindDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public MyDiaryController(ILogger<MyDiaryController> logger, SafeMindDbContext context, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var journals = await _context.Journals
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var checks = await _context.DailyChecks
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();

        var moodDistribution = journals.Select(j => j.Mood)
            .Concat(checks.Select(c => c.Mood))
            .GroupBy(m => m.ToString())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var moodScores = journals.Select(j => MapMoodScore(j.Mood))
            .Concat(checks.Select(c => MapMoodScore(c.Mood)))
            .ToList();
        double? avgMood = moodScores.Any() ? moodScores.Average() : null;
        var streak = CalculateStreak(checks);
        var vm = new DiaryPageViewModel
        {
            Journals = journals.Select(journal => new JournalViewModel
            {
                CreatedOn = journal.CreatedAt,
                Mood = journal.Mood,
                Title = journal.Title,
                Category = journal.Category,
                Content = journal.Content
            }),
            CheckIns = checks.Select(check => new DailyCheckViewModel
            {
                CreatedOn = check.CreatedOn,
                Mood = check.Mood,
                Energy = check.Energy,
                Stress = check.Stress,
                Sleep = check.Sleep,
                Notes = check.Notes
            }),
            Insights = new InsightsViewModel
            {
                TotalJournals = journals.Count,
                TotalCheckIns = checks.Count,
                MoodDistribution = moodDistribution,
                TotalGoals = _context.Goals.Count(g => g.UserId == userId),
                AverageMoodScore = avgMood,
                DayStreak = streak
            }
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static double MapMoodScore(JournalMood mood) => mood switch
    {
        JournalMood.Happy => 5.0,
        JournalMood.Excited => 4.5,
        JournalMood.Calm => 4.0,
        JournalMood.Anxious => 2.0,
        JournalMood.Sad => 1.5,
        JournalMood.Angry => 1.0,
        _ => 3.0
    };

    private static int CalculateStreak(IEnumerable<DailyCheck> checks)
    {
        var distinctDates = checks
            .Select(c => c.CreatedOn.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!distinctDates.Any()) return 0;

        var current = distinctDates[^1];
        var streak = 1;

        while (distinctDates.Contains(current.AddDays(-streak)))
        {
            streak++;
        }

        return streak;
    }


}
