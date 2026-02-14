using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Data;
using SafeMind.Models;
using SafeMind.Services;

namespace SafeMind.Controllers;

public class ArticlesController : Controller
{
    private readonly ILogger<ArticlesController> _logger;
    private SafeMindDbContext _context;
    private ArticleService _articleService;

    public ArticlesController(ILogger<ArticlesController> logger, SafeMindDbContext context, ArticleService articleService)
    {
        _logger = logger;
        _context = context;
        _articleService = articleService;
    }

    public async Task<IActionResult> Index()
    {
        var articles = await _articleService.GetAllArticlesAsync();
        return View(articles);
    }
    public async Task<IActionResult> SelectedArticle(int id)
    {
        var article = await _articleService.GetSelectedArticleAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        _context.Articles.Where(a => a.Id == id).FirstOrDefault().ViewCount++;
        _context.SaveChanges();
        return View(article);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
