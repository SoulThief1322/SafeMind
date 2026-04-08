using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTests
    {
        private SafeMindDbContext _context = null!;
        private HomeController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            var logger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(logger.Object, _context);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public async Task Index_ReturnsViewWithRecentArticles()
        {
            _context.Articles.AddRange(
                new Article { Headline = "A1", Content = "C1", AuthorId = "u1", IsDeleted = false, PublishedOn = System.DateTimeOffset.UtcNow.AddDays(-1) },
                new Article { Headline = "A2", Content = "C2", AuthorId = "u1", IsDeleted = false, PublishedOn = System.DateTimeOffset.UtcNow },
                new Article { Headline = "Deleted", Content = "C3", AuthorId = "u1", IsDeleted = true, PublishedOn = System.DateTimeOffset.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.Index();

            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            var model = viewResult!.Model as HomePageViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.RecentArticles.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Index_ExcludesDeletedArticles()
        {
            _context.Articles.Add(new Article { Headline = "Del", Content = "C", AuthorId = "u1", IsDeleted = true, PublishedOn = System.DateTimeOffset.UtcNow });
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;
            var model = result!.Model as HomePageViewModel;
            Assert.That(model!.RecentArticles.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Index_ReturnsMax6Articles()
        {
            for (int i = 0; i < 10; i++)
                _context.Articles.Add(new Article { Headline = $"A{i}", Content = "C", AuthorId = "u1", IsDeleted = false, PublishedOn = System.DateTimeOffset.UtcNow.AddDays(-i) });
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;
            var model = result!.Model as HomePageViewModel;
            Assert.That(model!.RecentArticles.Count, Is.EqualTo(6));
        }

        [Test]
        public void Privacy_ReturnsView()
        {
            var result = _controller.Privacy();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void TermsOfService_ReturnsView()
        {
            var result = _controller.TermsOfService();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void AboutUs_ReturnsView()
        {
            var result = _controller.AboutUs();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void HipaaCompliance_ReturnsView()
        {
            var result = _controller.HipaaCompliance();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void CrisisResources_ReturnsView()
        {
            var result = _controller.CrisisResources();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        // ── SaveMood / GetMood / ResetMood ──

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
        public async Task SaveMood_ValidMood_ReturnsOk()
        {
            SetUser("user1");
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "user1", UserName = "u1" });
            await _context.SaveChangesAsync();

            var result = await _controller.SaveMood(new SaveMoodRequest { Mood = "Great" });
            Assert.That(result, Is.InstanceOf<OkResult>());

            var saved = await _context.MoodChecks.FirstOrDefaultAsync(m => m.UserId == "user1");
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.Mood, Is.EqualTo("Great"));
        }

        [Test]
        public async Task SaveMood_InvalidMood_ReturnsBadRequest()
        {
            SetUser("user1");

            var result = await _controller.SaveMood(new SaveMoodRequest { Mood = "Terrible" });
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task SaveMood_EmptyMood_ReturnsBadRequest()
        {
            SetUser("user1");

            var result = await _controller.SaveMood(new SaveMoodRequest { Mood = "" });
            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task SaveMood_UpdatesExistingMoodWithin24Hours()
        {
            SetUser("user1");
            _context.MoodChecks.Add(new MoodCheck { UserId = "user1", Mood = "Okay", SavedAt = System.DateTimeOffset.UtcNow.AddHours(-1) });
            await _context.SaveChangesAsync();

            var result = await _controller.SaveMood(new SaveMoodRequest { Mood = "Great" });
            Assert.That(result, Is.InstanceOf<OkResult>());

            var checks = await _context.MoodChecks.Where(m => m.UserId == "user1").ToListAsync();
            Assert.That(checks.Count, Is.EqualTo(1));
            Assert.That(checks[0].Mood, Is.EqualTo("Great"));
        }

        [Test]
        public async Task GetMood_ReturnsMoodWithin24Hours()
        {
            SetUser("user1");
            _context.MoodChecks.Add(new MoodCheck { UserId = "user1", Mood = "Not great", SavedAt = System.DateTimeOffset.UtcNow.AddHours(-2) });
            await _context.SaveChangesAsync();

            var result = await _controller.GetMood() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task ResetMood_RemovesRecentMoods()
        {
            SetUser("user1");
            _context.MoodChecks.Add(new MoodCheck { UserId = "user1", Mood = "Great", SavedAt = System.DateTimeOffset.UtcNow.AddHours(-1) });
            await _context.SaveChangesAsync();

            var result = await _controller.ResetMood();
            Assert.That(result, Is.InstanceOf<OkResult>());

            var remaining = await _context.MoodChecks.Where(m => m.UserId == "user1").CountAsync();
            Assert.That(remaining, Is.EqualTo(0));
        }
    }
}
