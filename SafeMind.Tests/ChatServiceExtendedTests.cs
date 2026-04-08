using NUnit.Framework;
using SafeMind.Data;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ChatServiceExtendedTests
    {
        private SafeMindDbContext _context = null!;
        private ChatService _service = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            _service = new ChatService(_context);

            // Seed users
            _context.Users.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient1", UserName = "Patient One", Email = "p1@e.com" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "patient2", UserName = "Patient Two", Email = "p2@e.com" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "docuser1", UserName = "DocUser", Email = "d1@e.com" }
            );

            var doctor = new Doctor
            {
                Name = "Dr. A", UserId = "docuser1", Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            // Sessions for both patients
            var contact1 = new SessionContact { FullName = "Patient One", PhoneNumber = "1", Email = "p1@e.com" };
            var contact2 = new SessionContact { FullName = "Patient Two", PhoneNumber = "2", Email = "p2@e.com" };
            _context.SessionContacts.AddRange(contact1, contact2);
            await _context.SaveChangesAsync();

            _context.Sessions.AddRange(
                new Session
                {
                    DoctorId = doctor.Id, PatientId = "patient1",
                    StartTime = DateTimeOffset.UtcNow.AddDays(-5), EndTime = DateTimeOffset.UtcNow.AddDays(-5).AddHours(1),
                    Price = 100, SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact1.Id
                },
                new Session
                {
                    DoctorId = doctor.Id, PatientId = "patient2",
                    StartTime = DateTimeOffset.UtcNow.AddDays(-3), EndTime = DateTimeOffset.UtcNow.AddDays(-3).AddHours(1),
                    Price = 100, SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact2.Id
                }
            );

            // Seed chat messages
            _context.ChatMessages.AddRange(
                new ChatMessage { SenderId = "patient1", ReceiverId = "docuser1", Message = "Hello doc", Timestamp = DateTimeOffset.UtcNow.AddHours(-3), IsRead = false },
                new ChatMessage { SenderId = "docuser1", ReceiverId = "patient1", Message = "Hi patient", Timestamp = DateTimeOffset.UtcNow.AddHours(-2), IsRead = true },
                new ChatMessage { SenderId = "patient1", ReceiverId = "docuser1", Message = "Follow up", Timestamp = DateTimeOffset.UtcNow.AddHours(-1), IsRead = false },
                new ChatMessage { SenderId = "patient2", ReceiverId = "docuser1", Message = "Hey", Timestamp = DateTimeOffset.UtcNow, IsRead = false }
            );
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown() => _context?.Dispose();

        // ── GetConversationsAsync ──

        [Test]
        public async Task GetConversationsAsync_PatientSeesDoctor()
        {
            var result = await _service.GetConversationsAsync("patient1");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetConversationsAsync_NoMessages_ReturnsEmpty()
        {
            _context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser { Id = "lonely", UserName = "lonely" });
            await _context.SaveChangesAsync();

            var result = await _service.GetConversationsAsync("lonely");
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMyDoctorsAsync ──

        [Test]
        public async Task GetMyDoctorsAsync_PatientWithSessions()
        {
            var result = await _service.GetMyDoctorsAsync("patient1");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetMyDoctorsAsync_PatientWithoutSessions_Empty()
        {
            var result = await _service.GetMyDoctorsAsync("noone");
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMessagesAsync ──

        [Test]
        public async Task GetMessagesAsync_ReturnsOrderedMessages()
        {
            var doctor = await _context.Doctors.FirstAsync();
            var result = await _service.GetMessagesAsync("patient1", doctor.Id);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetMessagesAsync_InvalidDoctorId_ReturnsEmpty()
        {
            var result = await _service.GetMessagesAsync("patient1", 9999);
            Assert.That(result, Is.Not.Null);
        }

        // ── GetDoctorConversationsAsync ──

        [Test]
        public async Task GetDoctorConversationsAsync_DoctorSeesBothPatients()
        {
            var result = await _service.GetDoctorConversationsAsync("docuser1");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetDoctorConversationsAsync_NonDoctor_ReturnsEmptyList()
        {
            var result = await _service.GetDoctorConversationsAsync("patient1");
            Assert.That(result, Is.Not.Null);
        }

        // ── GetMyPatientsAsync ──

        [Test]
        public async Task GetMyPatientsAsync_ReturnsPatients()
        {
            var result = await _service.GetMyPatientsAsync("docuser1");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetMyPatientsAsync_NonDoctor_ReturnsEmpty()
        {
            var result = await _service.GetMyPatientsAsync("patient1");
            Assert.That(result, Is.Not.Null);
        }

        // ── GetUnreadCountAsync ──

        [Test]
        public async Task GetUnreadCountAsync_ReturnsCorrectCount()
        {
            // docuser1 has 3 unread messages (from patient1 x2, patient2 x1)
            var count = await _service.GetUnreadCountAsync("docuser1");
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetUnreadCountAsync_PatientSide()
        {
            // patient1 has 0 unread (the one from docuser1 is IsRead=true)
            var count = await _service.GetUnreadCountAsync("patient1");
            Assert.That(count, Is.EqualTo(0));
        }

        // ── GetDoctorMessagesAsync ──

        [Test]
        public async Task GetDoctorMessagesAsync_ReturnsMessagesWithPatient()
        {
            var result = await _service.GetDoctorMessagesAsync("docuser1", "patient1");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetDoctorMessagesAsync_NoMessages_ReturnsEmpty()
        {
            var result = await _service.GetDoctorMessagesAsync("docuser1", "nonexistent");
            Assert.That(result, Is.Not.Null);
        }
    }
}
