using System.Diagnostics;
using System.Security.Claims;
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
        var featured = await _articleService.GetFeaturedPerCategoryAsync();
        var viewModel = new ArticlesPageViewModel
        {
            Articles = articles,
            FeaturedArticles = featured
        };
        return View(viewModel);
    }
    public async Task<IActionResult> SelectedArticle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var article = await _articleService.GetSelectedArticleAsync(id, userId);
        if (article == null)
        {
            return NotFound();
        }
        _context.Articles.Where(a => a.Id == id).FirstOrDefault().ViewCount++;
        _context.SaveChanges();
        return View(article);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LikeArticle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }
        var (hasLiked, likes) = await _articleService.ToggleLikeAsync(id, userId);
        return Json(new { hasLiked, likes });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
