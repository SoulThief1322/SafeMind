using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IWebHostEnvironment _env;

    public ArticlesController(ILogger<ArticlesController> logger, SafeMindDbContext context, ArticleService articleService, IWebHostEnvironment env)
    {
        _logger = logger;
        _context = context;
        _articleService = articleService;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var articles = await _articleService.GetAllArticlesAsync();
        var categories = await _articleService.GetAllCategoriesAsync();
        var viewModel = new ArticlesAndCategoriesViewModel
        {
            Articles = articles,
            Categories = categories
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
        var articleForCount = _context.Articles.FirstOrDefault(a => a.Id == id);
        if (articleForCount != null)
        {
            articleForCount.ViewCount++;
            _context.SaveChanges();
        }
        return View(article);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> LikeArticle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (hasLiked, likes) = await _articleService.ToggleLikeAsync(id, userId);
        return Json(new { hasLiked, likes });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var vm = new CreateArticleViewModel
        {
            AvailableCategories = await _articleService.GetCategoryOptionsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateArticleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _articleService.GetCategoryOptionsAsync();
            return View(model);
        }

        string? imagePath = null;

        if (model.Image != null && model.Image.Length > 0)
        {
            try
            {
                if (string.IsNullOrEmpty(_env.WebRootPath))
                {
                    ModelState.AddModelError("Image", "Unable to upload image. Server configuration error.");
                    _logger.LogError("WebRootPath is null or empty during article image upload");
                    model.AvailableCategories = await _articleService.GetCategoryOptionsAsync();
                    return View(model);
                }

                const long maxFileSize = 5 * 1024 * 1024;
                if (model.Image.Length > maxFileSize)
                {
                    ModelState.AddModelError("Image", "Image file size cannot exceed 5MB.");
                    model.AvailableCategories = await _articleService.GetCategoryOptionsAsync();
                    return View(model);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(model.Image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Image", "Only image files (.jpg, .jpeg, .png, .gif, .webp) are allowed.");
                    model.AvailableCategories = await _articleService.GetCategoryOptionsAsync();
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "articles");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(uploadsFolder, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                imagePath = "/images/articles/" + uniqueName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the article image");
                ModelState.AddModelError("Image", "An error occurred while uploading the image. Please try again.");
                model.AvailableCategories = await _articleService.GetCategoryOptionsAsync();
                return View(model);
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _articleService.CreateArticleAsync(
            model.Headline,
            model.Content,
            userId,
            imagePath,
            model.SelectedCategoryIds);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _articleService.SoftDeleteArticleAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
