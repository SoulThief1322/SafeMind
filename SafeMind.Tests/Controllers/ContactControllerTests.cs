using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests.Controllers
{
    [TestFixture]
    public class ContactControllerTests
    {
        private SafeMindDbContext _context = null!;
        private ContactController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            _controller = new ContactController(_context);
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), new Mock_TempDataProvider());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public void Index_Get_ReturnsViewWithEmptyModel()
        {
            var result = _controller.Index() as ViewResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Model, Is.InstanceOf<ContactViewModel>());
        }

        [Test]
        public async Task Index_Post_ValidModel_SavesAndRedirects()
        {
            var model = new ContactViewModel
            {
                FullName = "Test User",
                Email = "test@example.com",
                Subject = "Help",
                Message = "I need help with the app."
            };

            var result = await _controller.Index(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = result as RedirectToActionResult;
            Assert.That(redirect!.ActionName, Is.EqualTo("Index"));

            var saved = await _context.ContactMessages.FirstOrDefaultAsync();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.FullName, Is.EqualTo("Test User"));
            Assert.That(saved.Email, Is.EqualTo("test@example.com"));
        }

        [Test]
        public async Task Index_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("FullName", "Required");
            var model = new ContactViewModel();

            var result = await _controller.Index(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult!.Model, Is.EqualTo(model));
        }

        [Test]
        public async Task Index_Post_SetsTempData()
        {
            var model = new ContactViewModel
            {
                FullName = "Test",
                Email = "t@e.com",
                Subject = "S",
                Message = "M"
            };

            await _controller.Index(model);

            Assert.That(_controller.TempData["ContactSuccess"], Is.Not.Null);
        }

        // Minimal ITempDataProvider for testing
        private class Mock_TempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
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
