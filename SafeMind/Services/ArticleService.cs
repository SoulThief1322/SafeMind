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
                .ToListAsync();

            return articles;
        }

        public async Task<ArticlesViewModel> GetSelectedArticleAsync(int id, string? userId = null)
        {
            var article = await _context.Articles
                .Where(a => a.Id == id)
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
    }
}