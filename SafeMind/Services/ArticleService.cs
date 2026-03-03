using Data.Models;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Models;

namespace SafeMind.Services
{
    public class ArticleService
    {
        private SafeMindDbContext _context;

        public ArticleService(SafeMindDbContext context)
        {
            _context = context;
        }

        public async Task<List<ArticlesViewModel>> GetAllArticlesAsync()
        {
            var articles = await _context.Articles
                .Where(a => !a.IsDeleted)
                .Select(a => new ArticlesViewModel
                {
                    Id = a.Id,
                    Headline = a.Headline,
                    Content = a.Content,
                    AuthorName = _context.Users.Where(u => u.Id == a.AuthorId).Select(u => u.UserName).FirstOrDefault() ?? "Unknown",
                    DateOfPublish = a.PublishedOn,
                    ViewCount = a.ViewCount,
                    ViewsInLastWeek = a.ViewsInLastWeek,
                    Likes = a.Likes,
                    imagePath = a.ImagePath,
                    Categories = a.ArticleCategories.Select(ac => ac.Category).ToList()
                })
                .OrderByDescending(a => a.DateOfPublish)
                .ToListAsync();

            return articles;
        }

        public async Task<ArticlesViewModel> GetSelectedArticleAsync(int id, string? userId = null)
        {
            var article = await _context.Articles
                .Where(a => a.Id == id && !a.IsDeleted)
                .Select(a => new ArticlesViewModel
                {
                    Id = a.Id,
                    Headline = a.Headline,
                    Content = a.Content,
                    AuthorName = _context.Users.Where(u => u.Id == a.AuthorId).Select(u => u.UserName).FirstOrDefault() ?? "Unknown",
                    DateOfPublish = a.PublishedOn,
                    ViewCount = a.ViewCount,
                    ViewsInLastWeek = a.ViewsInLastWeek,
                    Likes = a.Likes,
                    imagePath = a.ImagePath,
                    HasLiked = userId != null && _context.ArticleLikes.Any(al => al.ArticleId == a.Id && al.UserId == userId),
                    Categories = a.ArticleCategories.Select(ac => ac.Category).ToList()
                })
                .FirstOrDefaultAsync();

            return article;
        }
        public async Task<(bool HasLiked, int Likes)> ToggleLikeAsync(int articleId, string userId)
        {
            var existingLike = await _context.ArticleLikes
                .FirstOrDefaultAsync(al => al.ArticleId == articleId && al.UserId == userId);

            var article = await _context.Articles.FindAsync(articleId);
            if (article == null) return (false, 0);

            bool liked;
            if (existingLike != null)
            {
                _context.ArticleLikes.Remove(existingLike);
                article.Likes = Math.Max(0, article.Likes - 1);
                liked = false;
            }
            else
            {
                _context.ArticleLikes.Add(new ArticleLike
                {
                    ArticleId = articleId,
                    UserId = userId
                });
                article.Likes++;
                liked = true;
            }

            await _context.SaveChangesAsync();
            return (liked, article.Likes);
        }

        public async Task<List<FeaturedArticleViewModel>> GetFeaturedPerCategoryAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.ArticleCategories)
                    .ThenInclude(ac => ac.Article)
                .ToListAsync();

            var featured = new List<FeaturedArticleViewModel>();

            foreach (var cat in categories)
            {
                var topArticle = cat.ArticleCategories
                    .Select(ac => ac.Article)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.ViewsInLastWeek)
                    .FirstOrDefault();

                if (topArticle != null)
                {
                    featured.Add(new FeaturedArticleViewModel
                    {
                        Topic = cat.Name,
                        Category = cat.Name,
                        Eyebrow = $"Top in {cat.Name}",
                        Title = topArticle.Headline,
                        Summary = topArticle.Content.Length > 100
                            ? topArticle.Content.Substring(0, 100) + "..."
                            : topArticle.Content,
                        Cta = "Read article",
                        ArticleId = topArticle.Id
                    });
                }
            }

            return featured;
        }
        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _context.Categories.Select(c => c.Name).ToListAsync();
        }

        public async Task<List<CategoryOptionViewModel>> GetCategoryOptionsAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryOptionViewModel { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        public async Task<Article> CreateArticleAsync(string headline, string content, string authorId, string? imagePath, List<int> categoryIds)
        {
            var article = new Article
            {
                Headline = headline,
                Content = content,
                AuthorId = authorId,
                PublishedOn = DateTimeOffset.UtcNow,
                ImagePath = imagePath
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            if (categoryIds.Any())
            {
                var validIds = await _context.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                foreach (var catId in validIds)
                {
                    _context.ArticleCategories.Add(new ArticleCategory
                    {
                        ArticleId = article.Id,
                        CategoryId = catId
                    });
                }

                await _context.SaveChangesAsync();
            }

            return article;
        }

        public async Task<bool> SoftDeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}