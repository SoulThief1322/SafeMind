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

        public async Task<SelectedArticleViewModel> GetSelectedArticleAsync(int id)
        {
            var article = await _context.Articles
                .Where(a => a.Id == id)
                .Select(a => new SelectedArticleViewModel
                {
                    Title = a.Headline,
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
                .FirstOrDefaultAsync();

            return article;
        } 
    }
}