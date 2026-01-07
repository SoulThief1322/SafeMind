using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Models;

namespace SafeMind.Controllers;

public class MySessionsController : Controller
{
    private readonly ILogger<MySessionsController> _logger;

    public MySessionsController(ILogger<MySessionsController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
