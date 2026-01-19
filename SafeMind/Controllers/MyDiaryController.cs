using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;
using Data.Models;
using Microsoft.AspNetCore.Identity;

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
            })
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
