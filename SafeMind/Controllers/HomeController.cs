using System.Diagnostics;
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

    public async Task<IActionResult> Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
