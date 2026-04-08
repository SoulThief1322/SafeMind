using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SafeMind.Tests.Controllers
{
    [TestFixture]
    public class AdminPanelControllerTests
    {
        private SafeMind.Data.SafeMindDbContext _context = null!;
        private AdminService _adminService = null!;
        private AdminPanelController _controller = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            _adminService = new AdminService(_context, null!);
            _controller = new AdminPanelController(_adminService);
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), new MockTempDataProvider());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        // ── Contacts ──

        [Test]
        public async Task Contacts_ReturnsViewWithMessages()
        {
            _context.ContactMessages.Add(new SafeMind.Data.Models.ContactMessage
            {
                FullName = "U1", Email = "u@e.com", Subject = "S", Message = "M"
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Contacts() as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task MarkContactRead_Redirects()
        {
            _context.ContactMessages.Add(new SafeMind.Data.Models.ContactMessage
            {
                FullName = "U", Email = "u@e.com", Subject = "S", Message = "M", IsRead = false
            });
            await _context.SaveChangesAsync();
            var msg = await _context.ContactMessages.FirstAsync();

            var result = await _controller.MarkContactRead(msg.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var updated = await _context.ContactMessages.FindAsync(msg.Id);
            Assert.That(updated!.IsRead, Is.True);
        }

        [Test]
        public async Task ArchiveContact_SetsArchived()
        {
            _context.ContactMessages.Add(new SafeMind.Data.Models.ContactMessage
            {
                FullName = "U", Email = "u@e.com", Subject = "S", Message = "M"
            });
            await _context.SaveChangesAsync();
            var msg = await _context.ContactMessages.FirstAsync();

            var result = await _controller.ArchiveContact(msg.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var updated = await _context.ContactMessages.FindAsync(msg.Id);
            Assert.That(updated!.IsArchived, Is.True);
        }

        [Test]
        public async Task DeleteContact_RemovesFromDb()
        {
            _context.ContactMessages.Add(new SafeMind.Data.Models.ContactMessage
            {
                FullName = "U", Email = "u@e.com", Subject = "S", Message = "M"
            });
            await _context.SaveChangesAsync();
            var msg = await _context.ContactMessages.FirstAsync();

            var result = await _controller.DeleteContact(msg.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var count = await _context.ContactMessages.CountAsync();
            Assert.That(count, Is.EqualTo(0));
        }

        // ── Goals ──

        [Test]
        public async Task Goals_ReturnsView()
        {
            var result = await _controller.Goals() as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task AddGoal_ValidDescription_AddsAndRedirects()
        {
            var result = await _controller.AddGoal("Read a book");
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var count = await _context.GoalTemplates.CountAsync();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task AddGoal_EmptyDescription_DoesNotAdd()
        {
            await _controller.AddGoal("   ");

            var count = await _context.GoalTemplates.CountAsync();
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddGoal_TooLong_DoesNotAdd()
        {
            var longDesc = new string('a', 201);
            await _controller.AddGoal(longDesc);

            var count = await _context.GoalTemplates.CountAsync();
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateGoal_ValidInput_UpdatesAndRedirects()
        {
            _context.GoalTemplates.Add(new SafeMind.Data.Models.GoalTemplate { Description = "Old" });
            await _context.SaveChangesAsync();
            var template = await _context.GoalTemplates.FirstAsync();

            var result = await _controller.UpdateGoal(template.Id, "New");
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var updated = await _context.GoalTemplates.FindAsync(template.Id);
            Assert.That(updated!.Description, Is.EqualTo("New"));
        }

        [Test]
        public async Task DeleteGoal_RemovesFromDb()
        {
            _context.GoalTemplates.Add(new SafeMind.Data.Models.GoalTemplate { Description = "G" });
            await _context.SaveChangesAsync();
            var template = await _context.GoalTemplates.FirstAsync();

            var result = await _controller.DeleteGoal(template.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var count = await _context.GoalTemplates.CountAsync();
            Assert.That(count, Is.EqualTo(0));
        }

        // ── Reports ──

        [Test]
        public async Task Reports_ReturnsView()
        {
            var result = await _controller.Reports() as ViewResult;
            Assert.That(result, Is.Not.Null);
        }

        private class MockTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, object?> _data = new();
            public System.Collections.Generic.IDictionary<string, object?> LoadTempData(HttpContext context) => _data;
            public void SaveTempData(HttpContext context, System.Collections.Generic.IDictionary<string, object?> values)
            {
                foreach (var kv in values) _data[kv.Key] = kv.Value;
            }
        }
    }
}
