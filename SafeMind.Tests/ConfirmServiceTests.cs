using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Data.Enums;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ConfirmServiceTests
    {
        [Test]
        public async Task AddSessionToDb_CreatesSession()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Confirm", UserId = "doc-1", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var service = new ConfirmService(context);
            var slot = new SlotsService.NormalizedSlot
            {
                StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero)
            };
            var contact = new SessionContact
            {
                FullName = "Patient Name", PhoneNumber = "123456789", Email = "patient@test.com"
            };

            await service.AddSessionToDb(doctor, slot, "patient-1", PaymentStatus.Paid, contact);

            var session = await context.Sessions.FirstOrDefaultAsync();
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.DoctorId, Is.EqualTo(doctor.Id));
            Assert.That(session.PatientId, Is.EqualTo("patient-1"));
            Assert.That(session.Price, Is.EqualTo(100));
            Assert.That(session.SessionStatus, Is.EqualTo(SessionStatus.Scheduled));
            Assert.That(session.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
        }

        [Test]
        public async Task AddSessionToDb_PendingPayment_SessionCreated()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Pending", UserId = "doc-2", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 80
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var service = new ConfirmService(context);
            var slot = new SlotsService.NormalizedSlot
            {
                StartTime = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.Zero)
            };
            var contact = new SessionContact
            {
                FullName = "P", PhoneNumber = "1", Email = "p@e.com"
            };

            await service.AddSessionToDb(doctor, slot, "patient-2", PaymentStatus.Pending, contact);

            var session = await context.Sessions.FirstAsync();
            Assert.That(session.PaymentStatus, Is.EqualTo(PaymentStatus.Pending));
        }

        [Test]
        public async Task GetConflicts_NoConflicts_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. NoConflict", UserId = "doc-3", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var service = new ConfirmService(context);
            var requestedStarts = new List<DateTimeOffset>
            {
                new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)
            };

            var hasConflict = await service.GetConflicts(doctor, requestedStarts);
            Assert.That(hasConflict, Is.False);
        }

        [Test]
        public async Task GetConflicts_WithConflict_ReturnsTrue()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Conflict", UserId = "doc-4", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
            context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "p1", StartTime = startTime,
                EndTime = startTime.AddHours(1), Price = 100,
                SessionStatus = SessionStatus.Scheduled, PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "1", Email = "p@e.com" }
            });
            await context.SaveChangesAsync();

            var service = new ConfirmService(context);
            var hasConflict = await service.GetConflicts(doctor, new List<DateTimeOffset> { startTime });

            Assert.That(hasConflict, Is.True);
        }

        [Test]
        public async Task GetConflicts_CancelledSession_NoConflict()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Cancelled", UserId = "doc-5", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
            context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "p2", StartTime = startTime,
                EndTime = startTime.AddHours(1), Price = 100,
                SessionStatus = SessionStatus.Cancelled, PaymentStatus = PaymentStatus.Refunded,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "1", Email = "p@e.com" }
            });
            await context.SaveChangesAsync();

            var service = new ConfirmService(context);
            var hasConflict = await service.GetConflicts(doctor, new List<DateTimeOffset> { startTime });

            Assert.That(hasConflict, Is.False);
        }
    }
}
