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
    public class BookSessionServiceTests
    {
        [Test]
        public async Task GetSelectedDoctor_ValidId_ReturnsDoctor()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Book", UserId = "doc-1", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100,
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var bookService = new BookService(context);
            var service = new BookSessionService(context, bookService);

            var result = await service.GetSelectedDoctor(doctor.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Dr. Book"));
        }

        [Test]
        public async Task GetSelectedDoctor_InvalidId_ReturnsNull()
        {
            using var context = TestDbContextFactory.Create();
            var bookService = new BookService(context);
            var service = new BookSessionService(context, bookService);

            var result = await service.GetSelectedDoctor(9999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetTakenSessions_ReturnsTakenTimes()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Taken", UserId = "doc-2", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var dayStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
            var dayEnd = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero);

            context.Sessions.AddRange(
                new Session
                {
                    DoctorId = doctor.Id, PatientId = "p1",
                    StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
                    EndTime = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero),
                    Price = 100, SessionStatus = SessionStatus.Scheduled, PaymentStatus = PaymentStatus.Paid,
                    Contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" }
                },
                new Session
                {
                    DoctorId = doctor.Id, PatientId = "p2",
                    StartTime = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero),
                    EndTime = new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.Zero),
                    Price = 100, SessionStatus = SessionStatus.Confirmed, PaymentStatus = PaymentStatus.Paid,
                    Contact = new SessionContact { FullName = "P2", PhoneNumber = "2", Email = "p2@e.com" }
                }
            );
            await context.SaveChangesAsync();

            var bookService = new BookService(context);
            var service = new BookSessionService(context, bookService);

            var taken = await service.GetTakenSessions(dayStart, dayEnd, doctor.Id);

            Assert.That(taken.Count, Is.EqualTo(2));
            Assert.That(taken, Does.Contain(new TimeOnly(10, 0)));
            Assert.That(taken, Does.Contain(new TimeOnly(14, 0)));
        }

        [Test]
        public async Task GetTakenSessions_ExcludesCancelled()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Cancel", UserId = "doc-3", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var dayStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
            var dayEnd = new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero);

            context.Sessions.Add(new Session
            {
                DoctorId = doctor.Id, PatientId = "p1",
                StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero),
                Price = 100, SessionStatus = SessionStatus.Cancelled, PaymentStatus = PaymentStatus.Refunded,
                Contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" }
            });
            await context.SaveChangesAsync();

            var bookService = new BookService(context);
            var service = new BookSessionService(context, bookService);

            var taken = await service.GetTakenSessions(dayStart, dayEnd, doctor.Id);

            Assert.That(taken.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetTakenSessions_NoSessions_ReturnsEmpty()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Empty", UserId = "doc-4", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var bookService = new BookService(context);
            var service = new BookSessionService(context, bookService);

            var taken = await service.GetTakenSessions(
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero),
                doctor.Id);

            Assert.That(taken.Count, Is.EqualTo(0));
        }
    }
}
