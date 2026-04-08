using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests.Controllers
{
    [TestFixture]
    public class ChatControllerTests
    {
        private SafeMindDbContext _context = null!;
        private ChatService _chatService = null!;
        private ChatController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.Create();
            _chatService = new ChatService(_context);
            _controller = new ChatController(_chatService);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        private void SetUser(string userId, bool isDoctor = false)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            if (isDoctor) claims.Add(new Claim(ClaimTypes.Role, "Doctor"));
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private async Task SeedDoctorAndMessages()
        {
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient1", UserName = "p1" });
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "docuser1", UserName = "d1" });
            var doctor = new Doctor
            {
                Name = "Dr. A", UserId = "docuser1", Biography = "Bio",
                WorkStart = new System.TimeOnly(9, 0), WorkEnd = new System.TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var contact = new SessionContact { FullName = "P", PhoneNumber = "1", Email = "p@e.com" };
            _context.SessionContacts.Add(contact);
            await _context.SaveChangesAsync();

            _context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "patient1",
                StartTime = System.DateTimeOffset.UtcNow.AddDays(-1),
                EndTime = System.DateTimeOffset.UtcNow.AddDays(-1).AddHours(1),
                Price = 100,
                SessionStatus = SafeMind.Data.Enums.SessionStatus.Completed,
                PaymentStatus = SafeMind.Data.Enums.PaymentStatus.Paid,
                ContactId = contact.Id
            });

            _context.ChatMessages.AddRange(
                new ChatMessage { SenderId = "patient1", ReceiverId = "docuser1", Message = "Hello", Timestamp = System.DateTimeOffset.UtcNow.AddMinutes(-5), IsRead = false },
                new ChatMessage { SenderId = "docuser1", ReceiverId = "patient1", Message = "Hi!", Timestamp = System.DateTimeOffset.UtcNow, IsRead = false }
            );
            await _context.SaveChangesAsync();
        }

        // ── GetConversations ──

        [Test]
        public async Task GetConversations_AsPatient_ReturnsJson()
        {
            await SeedDoctorAndMessages();
            SetUser("patient1");

            var result = await _controller.GetConversations() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetConversations_AsDoctor_ReturnsDoctorConversations()
        {
            await SeedDoctorAndMessages();
            SetUser("docuser1", isDoctor: true);

            var result = await _controller.GetConversations() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMessages ──

        [Test]
        public async Task GetMessages_ValidDoctor_ReturnsMessages()
        {
            await SeedDoctorAndMessages();
            SetUser("patient1");
            var doctor = await _context.Doctors.FirstAsync();

            var result = await _controller.GetMessages(doctor.Id) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetMessages_InvalidDoctor_ReturnsEmptyList()
        {
            SetUser("patient1");

            var result = await _controller.GetMessages(999) as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMyDoctors ──

        [Test]
        public async Task GetMyDoctors_ReturnsJson()
        {
            await SeedDoctorAndMessages();
            SetUser("patient1");

            var result = await _controller.GetMyDoctors() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        // ── GetUnreadCount ──

        [Test]
        public async Task GetUnreadCount_ReturnsCount()
        {
            await SeedDoctorAndMessages();
            SetUser("patient1");

            var result = await _controller.GetUnreadCount() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMyPatients ──

        [Test]
        public async Task GetMyPatients_AsDoctor_ReturnsPatients()
        {
            await SeedDoctorAndMessages();
            SetUser("docuser1", isDoctor: true);

            var result = await _controller.GetMyPatients() as JsonResult;
            Assert.That(result, Is.Not.Null);
        }

        // ── GetPatientMessages ──

        [Test]
        public async Task GetPatientMessages_ValidPatient_ReturnsMessages()
        {
            await SeedDoctorAndMessages();
            SetUser("docuser1", isDoctor: true);

            var result = await _controller.GetPatientMessages("patient1") as JsonResult;
            Assert.That(result, Is.Not.Null);
        }
    }
}
