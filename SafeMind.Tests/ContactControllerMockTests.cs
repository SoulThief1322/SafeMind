using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Models;
using System;
using System.Threading.Tasks;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ContactControllerMockTests
    {
        private SafeMindDbContext _context = null!;
        private ContactController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SafeMindDbContext(options);

            _controller = new ContactController(_context);

            // Setup TempData so redirects work
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public void Index_Get_ReturnsViewWithEmptyModel()
        {
            var result = _controller.Index() as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Model, Is.InstanceOf<ContactViewModel>());
        }

        [Test]
        public async Task Index_Post_ValidModel_SavesMessageAndRedirects()
        {
            var model = new ContactViewModel
            {
                FullName = "Test User",
                Email = "test@example.com",
                Subject = "Test Subject",
                Message = "Hello, this is a test message."
            };

            var result = await _controller.Index(model) as RedirectToActionResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ActionName, Is.EqualTo("Index"));

            var saved = await _context.ContactMessages.FirstOrDefaultAsync();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.FullName, Is.EqualTo("Test User"));
            Assert.That(saved.Email, Is.EqualTo("test@example.com"));
            Assert.That(saved.Subject, Is.EqualTo("Test Subject"));
            Assert.That(saved.Message, Is.EqualTo("Hello, this is a test message."));
        }

        [Test]
        public async Task Index_Post_InvalidModel_ReturnsViewWithModel()
        {
            var model = new ContactViewModel();
            _controller.ModelState.AddModelError("FullName", "Required");

            var result = await _controller.Index(model) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Model, Is.SameAs(model));
            Assert.That(await _context.ContactMessages.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Index_Post_SavesTimestamp()
        {
            var before = DateTimeOffset.UtcNow;

            var model = new ContactViewModel
            {
                FullName = "Jane",
                Email = "jane@example.com",
                Subject = "Time check",
                Message = "Testing timestamp"
            };

            await _controller.Index(model);
            var after = DateTimeOffset.UtcNow;

            var saved = await _context.ContactMessages.FirstAsync();
            Assert.That(saved.SubmittedOn, Is.GreaterThanOrEqualTo(before));
            Assert.That(saved.SubmittedOn, Is.LessThanOrEqualTo(after));
        }
    }
}
