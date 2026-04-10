using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests.Controllers
{
    [TestFixture]
    public class ArticlesControllerTests
    {
        private SafeMindDbContext _context = null!;
        private ArticleService _articleService = null!;
        private ArticlesController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            _articleService = new ArticleService(_context);
            var logger = new Mock<ILogger<ArticlesController>>();
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            _controller = new ArticlesController(logger.Object, _context, _articleService, env.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        private void SetUser(string userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Test]
        public async Task Index_ReturnsViewWithArticlesAndCategories()
        {
            var cat = new Category { Name = "Health" };
            _context.Categories.Add(cat);
            _context.Articles.Add(new Article
            {
                Headline = "A1",
                Content = "C1",
                AuthorId = "u1",
                IsDeleted = false,
                PublishedOn = DateTimeOffset.UtcNow,
                ArticleCategories = new List<ArticleCategory> { new() { Category = cat } }
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as ArticlesAndCategoriesViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Articles.Count, Is.EqualTo(1));
            Assert.That(model.Categories.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task SelectedArticle_ValidId_ReturnsView()
        {
            SetUser("u1");
            _context.Articles.Add(new Article
            {
                Headline = "Test",
                Content = "Content",
                AuthorId = "u1",
                IsDeleted = false,
                PublishedOn = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
            var article = await _context.Articles.FirstAsync();

            var result = await _controller.SelectedArticle(article.Id) as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SelectedArticle_InvalidId_ReturnsNotFound()
        {
            SetUser("u1");

            var result = await _controller.SelectedArticle(999);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task SelectedArticle_DeletedArticle_ReturnsNotFound()
        {
            SetUser("u1");
            _context.Articles.Add(new Article
            {
                Headline = "Del",
                Content = "C",
                AuthorId = "u1",
                IsDeleted = true,
                PublishedOn = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
            var article = await _context.Articles.FirstAsync();

            var result = await _controller.SelectedArticle(article.Id);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task SelectedArticle_IncrementsViewCount()
        {
            SetUser("u1");
            _context.Articles.Add(new Article
            {
                Headline = "T",
                Content = "C",
                AuthorId = "u1",
                IsDeleted = false,
                PublishedOn = DateTimeOffset.UtcNow,
                ViewCount = 5
            });
            await _context.SaveChangesAsync();
            var article = await _context.Articles.FirstAsync();

            await _controller.SelectedArticle(article.Id);

            var updated = await _context.Articles.FirstAsync(a => a.Id == article.Id);
            Assert.That(updated.ViewCount, Is.EqualTo(6));
        }

        [Test]
        public async Task LikeArticle_TogglesLike()
        {
            SetUser("u1");
            _context.Articles.Add(new Article
            {
                Headline = "T",
                Content = "C",
                AuthorId = "author",
                IsDeleted = false,
                PublishedOn = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
            var article = await _context.Articles.FirstAsync();

            var result = await _controller.LikeArticle(article.Id) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task Delete_SoftDeletesArticle()
        {
            SetUser("u1");
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), new MockTempDataProvider());
            _context.Articles.Add(new Article
            {
                Headline = "Delete Me",
                Content = "C",
                AuthorId = "u1",
                IsDeleted = false,
                PublishedOn = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
            var article = await _context.Articles.FirstAsync();

            var result = await _controller.Delete(article.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var deleted = await _context.Articles.FirstAsync(a => a.Id == article.Id);
            Assert.That(deleted.IsDeleted, Is.True);
        }

        [Test]
        public async Task Create_Get_ReturnsViewWithCategories()
        {
            SetUser("u1");
            _context.Categories.Add(new Category { Name = "Cat1" });
            await _context.SaveChangesAsync();

            var result = await _controller.Create() as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as CreateArticleViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.AvailableCategories.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            SetUser("u1");
            _controller.ModelState.AddModelError("Headline", "Required");
            var model = new CreateArticleViewModel();

            var result = await _controller.Create(model) as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        private class MockTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
        {
            private readonly Dictionary<string, object?> _data = new();
            public System.Collections.Generic.IDictionary<string, object?> LoadTempData(HttpContext context) => _data;
            public void SaveTempData(HttpContext context, System.Collections.Generic.IDictionary<string, object?> values)
            {
                foreach (var kv in values) _data[kv.Key] = kv.Value;
            }
        }
    }
}
