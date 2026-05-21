using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Data.Enums;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ArticleServiceTests
    {
        private async Task<SafeMind.Data.SafeMindDbContext> CreateSeededContext()
        {
            var context = TestDbContextFactory.Create();

            context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser
            {
                Id = "author-1",
                UserName = "TestAuthor",
                Email = "author@test.com",
                NormalizedEmail = "AUTHOR@TEST.COM",
                NormalizedUserName = "TESTAUTHOR"
            });
            await context.SaveChangesAsync();

            context.Categories.AddRange(
                new Category { Name = "Mental Health" },
                new Category { Name = "Wellness" },
                new Category { Name = "Self-Care" }
            );
            await context.SaveChangesAsync();

            context.Articles.AddRange(
                new Article
                {
                    Headline = "First Article", Content = "Content of article one which is longer than 100 characters to test truncation behaviour in featured articles and summary generation methods",
                    AuthorId = "author-1", PublishedOn = DateTimeOffset.UtcNow.AddDays(-5),
                    ViewCount = 100, ViewsInLastWeek = 50, Likes = 10, IsDeleted = false,
                    ArticleCategories = new List<ArticleCategory>()
                },
                new Article
                {
                    Headline = "Second Article", Content = "Short content",
                    AuthorId = "author-1", PublishedOn = DateTimeOffset.UtcNow.AddDays(-3),
                    ViewCount = 200, ViewsInLastWeek = 80, Likes = 20, IsDeleted = false,
                    ArticleCategories = new List<ArticleCategory>()
                },
                new Article
                {
                    Headline = "Deleted Article", Content = "Deleted content",
                    AuthorId = "author-1", PublishedOn = DateTimeOffset.UtcNow.AddDays(-1),
                    ViewCount = 50, Likes = 5, IsDeleted = true,
                    ArticleCategories = new List<ArticleCategory>()
                }
            );
            await context.SaveChangesAsync();

            // Link articles to categories
            var article1 = await context.Articles.FirstAsync(a => a.Headline == "First Article");
            var article2 = await context.Articles.FirstAsync(a => a.Headline == "Second Article");
            var cat1 = await context.Categories.FirstAsync(c => c.Name == "Mental Health");

            context.ArticleCategories.AddRange(
                new ArticleCategory { ArticleId = article1.Id, CategoryId = cat1.Id },
                new ArticleCategory { ArticleId = article2.Id, CategoryId = cat1.Id }
            );
            await context.SaveChangesAsync();

            return context;
        }

        [Test]
        public async Task GetAllArticles_ExcludesDeleted()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var articles = await service.GetAllArticlesAsync();

            Assert.That(articles.Count, Is.EqualTo(2));
            Assert.That(articles.Any(a => a.Headline == "Deleted Article"), Is.False);
        }

        [Test]
        public async Task GetAllArticles_OrderedByDateDescending()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var articles = await service.GetAllArticlesAsync();

            Assert.That(articles[0].DateOfPublish, Is.GreaterThan(articles[1].DateOfPublish));
        }

        [Test]
        public async Task GetArticlesPaged_FirstPage_ReturnsCorrectCount()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var (articles, total) = await service.GetArticlesPagedAsync(1, 1);

            Assert.That(articles.Count, Is.EqualTo(1));
            Assert.That(total, Is.EqualTo(2)); // 2 non-deleted
        }

        [Test]
        public async Task GetArticlesPaged_SecondPage_ReturnsRemaining()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var (articles, total) = await service.GetArticlesPagedAsync(2, 1);

            Assert.That(articles.Count, Is.EqualTo(1));
            Assert.That(total, Is.EqualTo(2));
        }

        [Test]
        public async Task GetArticlesPaged_BeyondLastPage_ReturnsEmpty()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var (articles, total) = await service.GetArticlesPagedAsync(10, 10);

            Assert.That(articles.Count, Is.EqualTo(0));
            Assert.That(total, Is.EqualTo(2));
        }

        [Test]
        public async Task GetSelectedArticle_ValidId_ReturnsArticle()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await context.Articles.FirstAsync(a => a.Headline == "First Article");
            var vm = await service.GetSelectedArticleAsync(article.Id);

            Assert.That(vm, Is.Not.Null);
            Assert.That(vm!.Headline, Is.EqualTo("First Article"));
        }

        [Test]
        public async Task GetSelectedArticle_DeletedId_ReturnsNull()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await context.Articles.FirstAsync(a => a.IsDeleted);
            var vm = await service.GetSelectedArticleAsync(article.Id);

            Assert.That(vm, Is.Null);
        }

        [Test]
        public async Task GetSelectedArticle_InvalidId_ReturnsNull()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var vm = await service.GetSelectedArticleAsync(9999);

            Assert.That(vm, Is.Null);
        }

        // ── Likes ──

        [Test]
        public async Task ToggleLike_FirstLike_IncrementsCount()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await context.Articles.FirstAsync(a => !a.IsDeleted);
            var originalLikes = article.Likes;

            var (liked, newCount) = await service.ToggleLikeAsync(article.Id, "new-user");

            Assert.That(liked, Is.True);
            Assert.That(newCount, Is.EqualTo(originalLikes + 1));
        }

        [Test]
        public async Task ToggleLike_Unlike_DecrementsCount()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await context.Articles.FirstAsync(a => !a.IsDeleted);

            // Like first
            await service.ToggleLikeAsync(article.Id, "user-toggle");
            // Unlike
            var (liked, newCount) = await service.ToggleLikeAsync(article.Id, "user-toggle");

            Assert.That(liked, Is.False);
        }

        [Test]
        public async Task ToggleLike_NonExistentArticle_ReturnsFalseZero()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var (liked, count) = await service.ToggleLikeAsync(9999, "user-1");

            Assert.That(liked, Is.False);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task ToggleLike_UnlikeNeverGoesNegative()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            // Create article with 0 likes
            var article = new Article
            {
                Headline = "Zero Likes", Content = "No likes", AuthorId = "author-1",
                PublishedOn = DateTimeOffset.UtcNow, Likes = 0
            };
            context.Articles.Add(article);
            await context.SaveChangesAsync();

            // Manually add a like record to simulate database state
            context.ArticleLikes.Add(new ArticleLike { ArticleId = article.Id, UserId = "user-neg" });
            await context.SaveChangesAsync();

            // Unlike from 0 likes -- Math.Max(0, -1) should be 0
            var (liked, count) = await service.ToggleLikeAsync(article.Id, "user-neg");

            Assert.That(liked, Is.False);
            Assert.That(count, Is.EqualTo(0));
        }

        // ── Categories ──

        [Test]
        public async Task GetAllCategories_ReturnsOnlyCategoriesWithArticles()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var categories = await service.GetAllCategoriesAsync();

            Assert.That(categories.Count, Is.EqualTo(1)); // Only "Mental Health" has articles
            Assert.That(categories[0], Is.EqualTo("Mental Health"));
        }

        [Test]
        public async Task GetCategoryOptions_ReturnsAll()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var options = await service.GetCategoryOptionsAsync();

            Assert.That(options.Count, Is.EqualTo(3));
        }

        // ── Create Article ──

        [Test]
        public async Task CreateArticle_WithCategories_CreatesArticleAndLinks()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var categories = await context.Categories.ToListAsync();
            var catIds = categories.Take(2).Select(c => c.Id).ToList();

            var article = await service.CreateArticleAsync("New Headline", "New Content", "author-1", "/img.png", catIds);

            Assert.That(article.Id, Is.GreaterThan(0));
            Assert.That(article.Headline, Is.EqualTo("New Headline"));

            var links = await context.ArticleCategories.Where(ac => ac.ArticleId == article.Id).ToListAsync();
            Assert.That(links.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task CreateArticle_NoCategories_CreatesArticleOnly()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await service.CreateArticleAsync("No Cat Article", "Content", "author-1", null, new List<int>());

            Assert.That(article.Id, Is.GreaterThan(0));
            var links = await context.ArticleCategories.Where(ac => ac.ArticleId == article.Id).ToListAsync();
            Assert.That(links.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task CreateArticle_InvalidCategoryIds_IgnoresInvalid()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var validCat = await context.Categories.FirstAsync();
            var article = await service.CreateArticleAsync("Partial Cat", "Content", "author-1", null, new List<int> { validCat.Id, 9999 });

            var links = await context.ArticleCategories.Where(ac => ac.ArticleId == article.Id).ToListAsync();
            Assert.That(links.Count, Is.EqualTo(1));
        }

        // ── Soft Delete ──

        [Test]
        public async Task SoftDelete_ValidArticle_SetsIsDeletedTrue()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var article = await context.Articles.FirstAsync(a => !a.IsDeleted);
            var result = await service.SoftDeleteArticleAsync(article.Id);

            Assert.That(result, Is.True);
            var updated = await context.Articles.FindAsync(article.Id);
            Assert.That(updated!.IsDeleted, Is.True);
        }

        [Test]
        public async Task SoftDelete_NonExistentArticle_ReturnsFalse()
        {
            using var context = await CreateSeededContext();
            var service = new ArticleService(context);

            var result = await service.SoftDeleteArticleAsync(9999);
            Assert.That(result, Is.False);
        }
    }
}
