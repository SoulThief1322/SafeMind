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
    public class BookControllerTests
    {
        private SafeMindDbContext _context = null!;
        private BookService _bookService = null!;
        private BookSessionService _bookSessionService = null!;
        private SlotsService _slotsService = null!;
        private ConfirmService _confirmService = null!;
        private BookController _controller = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            _bookService = new BookService(_context);
            _bookSessionService = new BookSessionService(_context, _bookService);
            _slotsService = new SlotsService();
            _confirmService = new ConfirmService(_context);

            var logger = new Mock<ILogger<BookController>>();
            _controller = new BookController(logger.Object, _context, _bookService, _bookSessionService, _slotsService, _confirmService);
            SetUser("patient1");

            // Seed doctor data
            _context.Users.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient1", UserName = "p1" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "docuser1", UserName = "d1" }
            );
            _context.Doctors.Add(new Doctor
            {
                Name = "Dr. Test", UserId = "docuser1", Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m,
                DoctorSpecialties = new List<DoctorSpecialty>
                {
                    new() { Specialty = new Specialty { Name = "Anxiety" } }
                },
                DoctorLanguages = new List<DoctorLanguage>
                {
                    new() { Language = new Language { Name = "English" } }
                }
            });
            await _context.SaveChangesAsync();
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
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                _controller.ControllerContext.HttpContext, new MockTempDataProvider());
        }

        // ── Index ──

        [Test]
        public async Task Index_ReturnsViewWithDoctors()
        {
            var result = await _controller.Index(null, null) as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as BookPageViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Doctors, Is.Not.Empty);
        }

        [Test]
        public async Task Index_FilterBySpecialty_FiltersDoctors()
        {
            var result = await _controller.Index("Anxiety", null) as ViewResult;
            var model = result!.Model as BookPageViewModel;
            Assert.That(model!.Doctors, Is.Not.Empty);
        }

        [Test]
        public async Task Index_FilterByName_FiltersDoctors()
        {
            var result = await _controller.Index(null, "Dr. Test") as ViewResult;
            var model = result!.Model as BookPageViewModel;
            Assert.That(model!.Doctors, Is.Not.Empty);
        }

        [Test]
        public async Task Index_NoResults_ReturnsEmptyDoctors()
        {
            var result = await _controller.Index(null, "Nonexistent") as ViewResult;
            var model = result!.Model as BookPageViewModel;
            Assert.That(model!.Doctors, Is.Empty);
        }

        [Test]
        public async Task Index_PaginationDefaults()
        {
            var result = await _controller.Index(null, null) as ViewResult;
            var model = result!.Model as BookPageViewModel;
            Assert.That(model!.PageNumber, Is.EqualTo(1));
            Assert.That(model.PageSize, Is.EqualTo(5));
        }

        // ── BookSession ──

        [Test]
        public async Task BookSession_ValidDoctor_ReturnsView()
        {
            var doctor = await _context.Doctors.FirstAsync();

            var result = await _controller.BookSession(doctor.Id, null) as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as SessionsViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Doctor.Name, Is.EqualTo("Dr. Test"));
        }

        [Test]
        public async Task BookSession_InvalidDoctor_ReturnsNotFound()
        {
            var result = await _controller.BookSession(999, null);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task BookSession_WithDate_UsesProvidedDate()
        {
            var doctor = await _context.Doctors.FirstAsync();
            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

            var result = await _controller.BookSession(doctor.Id, targetDate) as ViewResult;
            var model = result!.Model as SessionsViewModel;
            Assert.That(model!.SelectedDate, Is.EqualTo(targetDate));
        }

        // ── AvailableSessions ──

        [Test]
        public async Task AvailableSessions_ValidDoctor_ReturnsJson()
        {
            var doctor = await _context.Doctors.FirstAsync();
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            var result = await _controller.AvailableSessions(doctor.Id, date) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task AvailableSessions_InvalidDoctor_ReturnsNotFound()
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            var result = await _controller.AvailableSessions(999, date);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        // ── Checkout ──

        [Test]
        public async Task Checkout_InvalidSlots_Redirects()
        {
            var doctor = await _context.Doctors.FirstAsync();

            var result = await _controller.Checkout(doctor.Id, null);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }

        [Test]
        public async Task Checkout_ValidSlots_ReturnsCheckoutView()
        {
            var doctor = await _context.Doctors.FirstAsync();
            var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var slotsJson = $"{{\"doctorId\":{doctor.Id},\"slots\":[{{\"date\":\"{futureDate:yyyy-MM-dd}\",\"time\":\"10:00\"}}]}}";

            var result = await _controller.Checkout(doctor.Id, slotsJson) as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as CheckoutViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.DoctorId, Is.EqualTo(doctor.Id));
        }

        [Test]
        public async Task Checkout_DoctorNotFound_Returns404()
        {
            var slotsJson = "{\"doctorId\":999,\"slots\":[{\"date\":\"2030-01-01\",\"time\":\"10:00\"}]}";
            var result = await _controller.Checkout(999, slotsJson);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        // ── Confirmation ──

        [Test]
        public async Task Confirmation_ReturnsView()
        {
            var doctor = await _context.Doctors.FirstAsync();

            var result = await _controller.Confirmation(doctor.Id, 2) as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as ConfirmationViewModel;
            Assert.That(model!.DoctorName, Is.EqualTo("Dr. Test"));
            Assert.That(model.SessionCount, Is.EqualTo(2));
        }

        [Test]
        public async Task Confirmation_InvalidDoctor_UsesDefaultName()
        {
            var result = await _controller.Confirmation(999, 1) as ViewResult;
            var model = result!.Model as ConfirmationViewModel;
            Assert.That(model!.DoctorName, Is.EqualTo("Doctor"));
        }

        // ── Payment GET ──

        [Test]
        public async Task Payment_Get_NoBookingData_Redirects()
        {
            var result = _controller.Payment();
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
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
