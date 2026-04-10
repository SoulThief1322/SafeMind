using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public class MyDiaryControllerTests
    {
        private SafeMindDbContext _context = null!;
        private DiaryService _diaryService = null!;
        private GoalService _goalService = null!;
        private Mock<UserManager<IdentityUser>> _userManagerMock = null!;
        private MyDiaryController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            _diaryService = new DiaryService();
            _goalService = new GoalService(_context);

            var store = new Mock<IUserStore<IdentityUser>>();
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                store.Object,
                Options.Create(new IdentityOptions()),
                new Mock<IPasswordHasher<IdentityUser>>().Object,
                new IUserValidator<IdentityUser>[0],
                new IPasswordValidator<IdentityUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<IdentityUser>>>().Object
            );

            var logger = new Mock<ILogger<MyDiaryController>>();
            _controller = new MyDiaryController(logger.Object, _context, _userManagerMock.Object, _diaryService, _goalService);
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
            _userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        }

        // ── Index ──

        [Test]
        public async Task Index_ReturnsViewWithDiaryData()
        {
            SetUser("user1");

            // Seed some data
            _context.GoalTemplates.AddRange(
                new GoalTemplate { Description = "G1" },
                new GoalTemplate { Description = "G2" },
                new GoalTemplate { Description = "G3" }
            );
            _context.Journals.Add(new Journal
            {
                UserId = "user1", Title = "Entry 1", Content = "Content",
                Mood = JournalMood.Happy, CreatedAt = DateTime.UtcNow
            });
            _context.DailyChecks.Add(new DailyCheck
            {
                UserId = "user1", Mood = JournalMood.Calm, Energy = EnergyLevel.High,
                Stress = StressLevel.Low, Sleep = SleepQuality.Good,
                CreatedOn = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as DiaryPageViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.HasTodayCheck, Is.True);
        }

        [Test]
        public async Task Index_NullUser_ReturnsUnauthorized()
        {
            _userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var result = await _controller.Index();
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        // ── SaveCheck ──

        [Test]
        public async Task SaveCheck_Valid_ReturnsJson()
        {
            SetUser("user1");
            var request = new SaveDailyCheckRequest
            {
                Mood = JournalMood.Happy,
                Energy = EnergyLevel.High,
                Stress = StressLevel.Low,
                Sleep = SleepQuality.Good,
                Notes = "Feeling great"
            };

            var result = await _controller.SaveCheck(request) as JsonResult;
            Assert.That(result, Is.Not.Null);

            var check = await _context.DailyChecks.FirstOrDefaultAsync(c => c.UserId == "user1");
            Assert.That(check, Is.Not.Null);
        }

        [Test]
        public async Task SaveCheck_AlreadyCheckedToday_ReturnsConflict()
        {
            SetUser("user1");
            _context.DailyChecks.Add(new DailyCheck
            {
                UserId = "user1", Mood = JournalMood.Happy, Energy = EnergyLevel.High,
                Stress = StressLevel.Low, Sleep = SleepQuality.Good,
                CreatedOn = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var request = new SaveDailyCheckRequest { Mood = JournalMood.Sad };

            var result = await _controller.SaveCheck(request);
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public async Task SaveCheck_InvalidModel_ReturnsBadRequest()
        {
            SetUser("user1");
            _controller.ModelState.AddModelError("Mood", "Required");
            var request = new SaveDailyCheckRequest();

            var result = await _controller.SaveCheck(request);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SaveCheck_NullUser_ReturnsUnauthorized()
        {
            _userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var result = await _controller.SaveCheck(new SaveDailyCheckRequest());
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        // ── NewEntry ──

        [Test]
        public async Task NewEntry_Get_ReturnsView()
        {
            var result = await _controller.NewEntry() as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task NewEntry_Post_Valid_Redirects()
        {
            SetUser("user1");
            var request = new NewJournalEntryRequest
            {
                Title = "My Entry",
                Content = "Content here",
                Mood = JournalMood.Happy
            };

            var result = await _controller.NewEntry(request);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var journal = await _context.Journals.FirstAsync();
            Assert.That(journal.Title, Is.EqualTo("My Entry"));
        }

        [Test]
        public async Task NewEntry_Post_InvalidModel_ReturnsBadRequest()
        {
            SetUser("user1");
            _controller.ModelState.AddModelError("Title", "Required");
            var request = new NewJournalEntryRequest();

            var result = await _controller.NewEntry(request);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        // ── AllEntries ──

        [Test]
        public async Task AllEntries_ReturnsViewWithEntries()
        {
            SetUser("user1");
            _context.Journals.Add(new Journal
            {
                UserId = "user1", Title = "J", Content = "C",
                Mood = JournalMood.Calm, CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _controller.AllEntries() as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as DiaryPageViewModel;
            Assert.That(model, Is.Not.Null);
        }

        // ── CompleteGoal ──

        [Test]
        public async Task CompleteGoal_Valid_ReturnsSuccessJson()
        {
            SetUser("user1");
            _context.GoalTemplates.AddRange(
                new GoalTemplate { Description = "G1" },
                new GoalTemplate { Description = "G2" },
                new GoalTemplate { Description = "G3" }
            );
            await _context.SaveChangesAsync();

            // Create weekly goals first
            var goals = await _goalService.GetOrCreateWeeklyGoalsAsync("user1");
            var goalId = goals[0].Id;

            var result = await _controller.CompleteGoal(new CompleteGoalRequest { WeeklyGoalId = goalId }) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task CompleteGoal_AlreadyCompleted_ReturnsConflict()
        {
            SetUser("user1");
            _context.GoalTemplates.AddRange(
                new GoalTemplate { Description = "G1" },
                new GoalTemplate { Description = "G2" },
                new GoalTemplate { Description = "G3" }
            );
            await _context.SaveChangesAsync();

            var goals = await _goalService.GetOrCreateWeeklyGoalsAsync("user1");
            await _goalService.CompleteGoalAsync("user1", goals[0].Id);

            var result = await _controller.CompleteGoal(new CompleteGoalRequest { WeeklyGoalId = goals[0].Id });
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        // ── GetEntryDates ──

        [Test]
        public async Task GetEntryDates_ReturnsJson()
        {
            SetUser("user1");
            var now = DateTime.UtcNow;
            _context.Journals.Add(new Journal
            {
                UserId = "user1", Title = "J", Content = "C",
                Mood = JournalMood.Happy, CreatedAt = now
            });
            await _context.SaveChangesAsync();

            var result = await _controller.GetEntryDates(now.Year, now.Month) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }
    }
}
