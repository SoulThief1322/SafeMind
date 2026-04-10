using NUnit.Framework;
using SafeMind.Data;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class MySessionServiceExtendedTests
    {
        private SafeMindDbContext _context = null!;
        private MySessionService _service = null!;
        private SlotsService _slotsService = null!;
        private BookSessionService _bookSessionService = null!;

        private Doctor _doctor = null!;
        private SessionContact _contact = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            var bookService = new BookService(_context);
            _slotsService = new SlotsService();
            _bookSessionService = new BookSessionService(_context, bookService);
            _service = new MySessionService(_context, _bookSessionService, _slotsService);

            // Seed users
            _context.Users.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient1", UserName = "p1" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "doc1", UserName = "d1" }
            );

            _doctor = new Doctor
            {
                Name = "Dr. X", UserId = "doc1", Biography = "B",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m
            };
            _context.Doctors.Add(_doctor);
            await _context.SaveChangesAsync();

            _contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" };
            _context.SessionContacts.Add(_contact);
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown() => _context?.Dispose();

        private async Task<Session> AddSession(int daysFromNow = 5, SessionStatus status = SessionStatus.Scheduled, PaymentStatus pay = PaymentStatus.Pending)
        {
            var session = new Session
            {
                DoctorId = _doctor.Id,
                PatientId = "patient1",
                StartTime = DateTimeOffset.UtcNow.AddDays(daysFromNow),
                EndTime = DateTimeOffset.UtcNow.AddDays(daysFromNow).AddHours(1),
                Price = 100,
                SessionStatus = status,
                PaymentStatus = pay,
                ContactId = _contact.Id
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        // ── GetSessions ──

        [Test]
        public async Task GetSessions_PatientView_ReturnsPatientSessions()
        {
            await AddSession();
            var list = await _service.GetSessions("patient1", false);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSessions_DoctorView_ReturnsDoctorSessions()
        {
            await AddSession();
            var list = await _service.GetSessions("doc1", true);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSessions_ExcludesCancelled()
        {
            await AddSession(status: SessionStatus.Cancelled);
            var list = await _service.GetSessions("patient1", false);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        // ── GetSessionForUser ──

        [Test]
        public async Task GetSessionForUser_PatientFinds()
        {
            var session = await AddSession();
            var found = await _service.GetSessionForUser(session.Id, "patient1", false);
            Assert.That(found, Is.Not.Null);
        }

        [Test]
        public async Task GetSessionForUser_DoctorFinds()
        {
            var session = await AddSession();
            var found = await _service.GetSessionForUser(session.Id, "doc1", true);
            Assert.That(found, Is.Not.Null);
        }

        [Test]
        public async Task GetSessionForUser_WrongUser_ReturnsNull()
        {
            var session = await AddSession();
            var found = await _service.GetSessionForUser(session.Id, "other", false);
            Assert.That(found, Is.Null);
        }

        // ── GetSessionWithDoctorDetails ──

        [Test]
        public async Task GetSessionWithDoctorDetails_PatientFinds()
        {
            var session = await AddSession();
            var found = await _service.GetSessionWithDoctorDetails(session.Id, "patient1", false);
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Doctor, Is.Not.Null);
        }

        [Test]
        public async Task GetSessionWithDoctorDetails_DoctorFinds()
        {
            var session = await AddSession();
            var found = await _service.GetSessionWithDoctorDetails(session.Id, "doc1", true);
            Assert.That(found, Is.Not.Null);
        }

        [Test]
        public async Task GetSessionWithDoctorDetails_WrongUser_Null()
        {
            var session = await AddSession();
            var found = await _service.GetSessionWithDoctorDetails(session.Id, "wrong", false);
            Assert.That(found, Is.Null);
        }

        // ── ConfirmPostponeAsync ──

        [Test]
        public async Task ConfirmPostpone_NullSlotJson_ReturnsFalse()
        {
            var session = await AddSession(daysFromNow: 5);

            var (success, msg, _) = await _service.ConfirmPostponeAsync(session.Id, _doctor.Id, null, "patient1", false);
            Assert.That(success, Is.False);
        }

        [Test]
        public async Task ConfirmPostpone_SessionNotFound_ReturnsFalse()
        {
            var (success, msg, _) = await _service.ConfirmPostponeAsync(999, _doctor.Id, null, "patient1", false);
            Assert.That(success, Is.False);
            Assert.That(msg, Does.Contain("not found"));
        }

        [Test]
        public async Task ConfirmPostpone_TooClose_ReturnsFalse()
        {
            var session = await AddSession(daysFromNow: 0); // less than 24h away

            var slotsJson = "[{\"date\":\"2030-01-01\",\"time\":\"10:00\"}]";
            var (success, msg, _) = await _service.ConfirmPostponeAsync(session.Id, _doctor.Id, slotsJson, "patient1", false);
            Assert.That(success, Is.False);
            Assert.That(msg, Does.Contain("24 hours"));
        }

        [Test]
        public async Task ConfirmPostpone_ValidSlot_Succeeds()
        {
            var session = await AddSession(daysFromNow: 5);
            var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var slotsJson = $"[{{\"date\":\"{futureDate:yyyy-MM-dd}\",\"time\":\"10:00\"}}]";

            var (success, msg, newSlot) = await _service.ConfirmPostponeAsync(session.Id, _doctor.Id, slotsJson, "patient1", false);
            Assert.That(success, Is.True);
            Assert.That(newSlot, Is.Not.Null);

            // Old session should be cancelled
            var old = await _context.Sessions.FindAsync(session.Id);
            Assert.That(old!.SessionStatus, Is.EqualTo(SessionStatus.Cancelled));

            // New session should exist
            var total = await _context.Sessions.CountAsync();
            Assert.That(total, Is.EqualTo(2));
        }

        [Test]
        public async Task ConfirmPostpone_AsDoctor_Works()
        {
            var session = await AddSession(daysFromNow: 5);
            var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var slotsJson = $"[{{\"date\":\"{futureDate:yyyy-MM-dd}\",\"time\":\"14:00\"}}]";

            var (success, _, _) = await _service.ConfirmPostponeAsync(session.Id, _doctor.Id, slotsJson, "doc1", true);
            Assert.That(success, Is.True);
        }
    }
}
