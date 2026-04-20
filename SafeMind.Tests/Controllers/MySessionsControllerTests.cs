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
    public class MySessionsControllerTests
    {
        private SafeMindDbContext _context = null!;
        private MySessionService _mySessionService = null!;
        private BookSessionService _bookSessionService = null!;
        private SlotsService _slotsService = null!;
        private RatingService _ratingService = null!;
        private MySessionsController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            var bookService = new BookService(_context);
            _slotsService = new SlotsService();
            _bookSessionService = new BookSessionService(_context, bookService);
            _mySessionService = new MySessionService(_context, _bookSessionService, _slotsService);
            _ratingService = new RatingService(_context);
            var logger = new Mock<ILogger<MySessionsController>>();
            _controller = new MySessionsController(logger.Object, _mySessionService, _bookSessionService, _slotsService, _ratingService);
            SetUser("patient1", false);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        private void SetUser(string userId, bool isDoctor)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            if (isDoctor) claims.Add(new Claim(ClaimTypes.Role, "Doctor"));
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                _controller.ControllerContext.HttpContext, new MockTempDataProvider());
        }

        private async Task<(Doctor doctor, Session session)> SeedSession(SessionStatus status = SessionStatus.Scheduled, PaymentStatus payStatus = PaymentStatus.Pending, int daysFromNow = 5)
        {
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient1", UserName = "p1" });
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "doc1", UserName = "d1" });
            var doctor = new Doctor
            {
                Name = "Dr. Test", UserId = "doc1", Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" };
            _context.SessionContacts.Add(contact);
            await _context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id,
                PatientId = "patient1",
                StartTime = DateTimeOffset.UtcNow.AddDays(daysFromNow),
                EndTime = DateTimeOffset.UtcNow.AddDays(daysFromNow).AddHours(1),
                Price = 100,
                SessionStatus = status,
                PaymentStatus = payStatus,
                ContactId = contact.Id
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return (doctor, session);
        }

        [Test]
        public async Task Index_ReturnsViewWithSessions()
        {
            await SeedSession();

            var result = await _controller.Index() as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as MySessionsPageViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Upcoming.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Index_DoctorView_ShowsSessions()
        {
            await SeedSession();
            SetUser("doc1", true);

            var result = await _controller.Index() as ViewResult;
            var model = result!.Model as MySessionsPageViewModel;
            Assert.That(model!.Upcoming.Count, Is.GreaterThan(0));
        }

        // ── Cancel ──

        [Test]
        public async Task Cancel_ValidSession_RedirectsWithSuccess()
        {
            var (_, session) = await SeedSession();

            var result = await _controller.Cancel(session.Id) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ActionName, Is.EqualTo("Index"));
            Assert.That(_controller.TempData["Success"], Is.Not.Null);
        }

        [Test]
        public async Task Cancel_NonExistent_RedirectsWithError()
        {
            var result = await _controller.Cancel(999) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // ── Confirm (doctor) ──

        [Test]
        public async Task Confirm_AsDoctor_ConfirmsSession()
        {
            var (_, session) = await SeedSession();
            SetUser("doc1", true);

            var result = await _controller.Confirm(session.Id) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"], Is.Not.Null);

            var updated = await _context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.SessionStatus, Is.EqualTo(SessionStatus.Confirmed));
        }

        [Test]
        public async Task Confirm_WrongDoctor_ReturnsError()
        {
            await SeedSession();
            SetUser("wrongdoctor", true);

            var unknownSessionId = (await _context.Sessions.FirstAsync()).Id;
            await _controller.Confirm(unknownSessionId);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // ── Complete ──

        [Test]
        public async Task Complete_PastSession_Completes()
        {
            var (_, session) = await SeedSession(daysFromNow: -2);
            SetUser("doc1", true);

            await _controller.Complete(session.Id);
            Assert.That(_controller.TempData["Success"], Is.Not.Null);

            var updated = await _context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.SessionStatus, Is.EqualTo(SessionStatus.Completed));
        }

        [Test]
        public async Task Complete_FutureSession_ReturnsError()
        {
            var (_, session) = await SeedSession(daysFromNow: 5);
            SetUser("doc1", true);

            await _controller.Complete(session.Id);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // ── Payment ──

        [Test]
        public async Task Payment_Get_PendingSession_ReturnsView()
        {
            var (_, session) = await SeedSession(payStatus: PaymentStatus.Pending);

            var result = await _controller.Payment(session.Id) as ViewResult;
            Assert.That(result, Is.Not.Null);
            var model = result!.Model as PaymentViewModel;
            Assert.That(model!.TotalAmount, Is.EqualTo(100));
        }

        [Test]
        public async Task Payment_Get_AlreadyPaid_Redirects()
        {
            var (_, session) = await SeedSession(payStatus: PaymentStatus.Paid);

            var result = await _controller.Payment(session.Id);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }

        [Test]
        public async Task Payment_Get_NotFound_Returns404()
        {
            var result = await _controller.Payment(999);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ProcessPayment_Valid_PaysSession()
        {
            var (_, session) = await SeedSession(payStatus: PaymentStatus.Pending);
            var model = new PaymentViewModel
            {
                SessionId = session.Id,
                TotalAmount = 100,
                CardNumber = "4111111111111111",
                CardholderName = "TEST USER",
                CVV = "123",
                ExpiryDate = "12/30"
            };

            var result = await _controller.ProcessPayment(model) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"], Is.Not.Null);

            var updated = await _context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
        }

        [Test]
        public async Task ProcessPayment_InvalidModel_ReturnsPaymentView()
        {
            _controller.ModelState.AddModelError("CardNumber", "Required");
            var model = new PaymentViewModel();

            var result = await _controller.ProcessPayment(model) as ViewResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ViewName, Is.EqualTo("Payment"));
        }

        [Test]
        public async Task ProcessPayment_NullSessionId_RedirectsWithError()
        {
            var model = new PaymentViewModel { SessionId = null };

            var result = await _controller.ProcessPayment(model) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // ── RateSession ──

        [Test]
        public async Task RateSession_ValidRating_ReturnsSuccess()
        {
            var (doctor, session) = await SeedSession(SessionStatus.Completed, PaymentStatus.Paid, -2);

            var result = await _controller.RateSession(session.Id, 5) as JsonResult;
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
