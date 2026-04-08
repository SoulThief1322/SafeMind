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
    public class AdminServiceExtendedTests
    {
        private SafeMindDbContext _context = null!;
        private AdminService _service = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            _service = new AdminService(_context, null!);

            _context.Users.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "p1", UserName = "patient1" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "p2", UserName = "patient2" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "d1", UserName = "doctor1" }
            );

            var doctor = new Doctor
            {
                Name = "Dr. A", UserId = "d1", Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100, Rating = 4.0m
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" };
            _context.SessionContacts.Add(contact);
            await _context.SaveChangesAsync();

            // Recent session (within 30 days)
            _context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "p1",
                StartTime = DateTimeOffset.UtcNow.AddDays(-5),
                EndTime = DateTimeOffset.UtcNow.AddDays(-5).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                TimeOfBooking = DateTimeOffset.UtcNow.AddDays(-10),
                ContactId = contact.Id
            });

            // Old session (more than 6 months)
            _context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "p2",
                StartTime = DateTimeOffset.UtcNow.AddMonths(-8),
                EndTime = DateTimeOffset.UtcNow.AddMonths(-8).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Scheduled,
                PaymentStatus = PaymentStatus.Pending,
                TimeOfBooking = DateTimeOffset.UtcNow.AddMonths(-8),
                ContactId = contact.Id
            });
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown() => _context?.Dispose();

        // ── GetSessionsPerMonthAsync ──

        [Test]
        public async Task GetSessionsPerMonth_ReturnsGroupedByMonth()
        {
            var result = await _service.GetSessionsPerMonthAsync();
            Assert.That(result, Is.Not.Null);
            // Only the recent session (within 6 months) should be included
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetSessionsPerMonth_ExcludesOldSessions()
        {
            var result = await _service.GetSessionsPerMonthAsync(1);
            // Only sessions within last 1 month
            var oldDate = DateTimeOffset.UtcNow.AddMonths(-8).ToString("MMM yyyy");
            Assert.That(result.ContainsKey(oldDate), Is.False);
        }

        // ── GetNewUsersThisMonthAsync ──

        [Test]
        public async Task GetNewUsersThisMonth_ReturnsDistinctPatientCount()
        {
            var count = await _service.GetNewUsersThisMonthAsync();
            // Only p1 has a session booked within last 30 days
            Assert.That(count, Is.EqualTo(1));
        }

        // ── ContactMessages with archived filter ──

        [Test]
        public async Task GetContactMessages_IncludeArchived_ReturnsAll()
        {
            _context.ContactMessages.AddRange(
                new ContactMessage { FullName = "A", Email = "a@e.com", Subject = "S", Message = "M", IsArchived = false },
                new ContactMessage { FullName = "B", Email = "b@e.com", Subject = "S", Message = "M", IsArchived = true }
            );
            await _context.SaveChangesAsync();

            var all = await _service.GetContactMessagesAsync(includeArchived: true);
            Assert.That(all.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetContactMessages_ExcludeArchived_FiltersArchived()
        {
            _context.ContactMessages.AddRange(
                new ContactMessage { FullName = "A", Email = "a@e.com", Subject = "S", Message = "M", IsArchived = false },
                new ContactMessage { FullName = "B", Email = "b@e.com", Subject = "S", Message = "M", IsArchived = true }
            );
            await _context.SaveChangesAsync();

            var filtered = await _service.GetContactMessagesAsync(includeArchived: false);
            Assert.That(filtered.Count, Is.EqualTo(1));
        }

        // ── GetContactMessageAsync ──

        [Test]
        public async Task GetContactMessage_ExistingId_ReturnsMessage()
        {
            _context.ContactMessages.Add(new ContactMessage { FullName = "X", Email = "x@e.com", Subject = "S", Message = "M" });
            await _context.SaveChangesAsync();
            var msg = await _context.ContactMessages.FirstAsync();

            var result = await _service.GetContactMessageAsync(msg.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.FullName, Is.EqualTo("X"));
        }

        [Test]
        public async Task GetContactMessage_NonExistentId_ReturnsNull()
        {
            var result = await _service.GetContactMessageAsync(999);
            Assert.That(result, Is.Null);
        }

        // ── MarkContactAsRead idempotent ──

        [Test]
        public async Task MarkContactAsRead_AlreadyRead_NoChange()
        {
            _context.ContactMessages.Add(new ContactMessage { FullName = "X", Email = "x@e.com", Subject = "S", Message = "M", IsRead = true });
            await _context.SaveChangesAsync();
            var msg = await _context.ContactMessages.FirstAsync();

            await _service.MarkContactAsReadAsync(msg.Id);
            var result = await _context.ContactMessages.FindAsync(msg.Id);
            Assert.That(result!.IsRead, Is.True);
        }

        // ── Mark/Archive/Delete non-existent ──

        [Test]
        public async Task MarkContactAsRead_NonExistent_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => _service.MarkContactAsReadAsync(999));
        }

        [Test]
        public async Task ArchiveContact_NonExistent_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => _service.ArchiveContactAsync(999));
        }

        [Test]
        public async Task DeleteContact_NonExistent_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => _service.DeleteContactAsync(999));
        }
    }
}
