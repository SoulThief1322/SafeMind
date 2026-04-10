using NUnit.Framework;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class RatingServiceTests
    {
        [Test]
        public async Task SubmitRating_ValidRating_Succeeds()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Test", UserId = "doc-1", Rating = 0, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-1",
                StartTime = DateTimeOffset.UtcNow.AddDays(-2),
                EndTime = DateTimeOffset.UtcNow.AddDays(-2).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "Patient", PhoneNumber = "123", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var service = new RatingService(context);
            var (success, error) = await service.SubmitRatingAsync(session.Id, "patient-1", 5);

            Assert.That(success, Is.True);
            Assert.That(error, Is.Empty);

            var rating = await context.SessionRatings.FirstAsync();
            Assert.That(rating.Stars, Is.EqualTo(5));
        }

        [Test]
        public async Task SubmitRating_InvalidStars_TooLow_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var service = new RatingService(context);

            var (success, error) = await service.SubmitRatingAsync(1, "patient-1", 0);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Rating must be between 1 and 5."));
        }

        [Test]
        public async Task SubmitRating_InvalidStars_TooHigh_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var service = new RatingService(context);

            var (success, error) = await service.SubmitRatingAsync(1, "patient-1", 6);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Rating must be between 1 and 5."));
        }

        [Test]
        public async Task SubmitRating_SessionNotFound_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var service = new RatingService(context);

            var (success, error) = await service.SubmitRatingAsync(999, "patient-1", 5);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Session not found."));
        }

        [Test]
        public async Task SubmitRating_SessionNotEnded_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Future", UserId = "doc-2", Rating = 0, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-2",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Scheduled,
                PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "123", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var service = new RatingService(context);
            var (success, error) = await service.SubmitRatingAsync(session.Id, "patient-2", 4);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Session has not ended yet."));
        }

        [Test]
        public async Task SubmitRating_PastRatingWindow_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Old", UserId = "doc-3", Rating = 0, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-3",
                StartTime = DateTimeOffset.UtcNow.AddDays(-60),
                EndTime = DateTimeOffset.UtcNow.AddDays(-60).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "123", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var service = new RatingService(context);
            var (success, error) = await service.SubmitRatingAsync(session.Id, "patient-3", 3);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("30-day rating window"));
        }

        [Test]
        public async Task SubmitRating_AlreadyRated_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Rated", UserId = "doc-4", Rating = 0, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-4",
                StartTime = DateTimeOffset.UtcNow.AddDays(-5),
                EndTime = DateTimeOffset.UtcNow.AddDays(-5).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "123", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            context.SessionRatings.Add(new SessionRating
            {
                SessionId = session.Id, PatientId = "patient-4", Stars = 4
            });
            await context.SaveChangesAsync();

            var service = new RatingService(context);
            var (success, error) = await service.SubmitRatingAsync(session.Id, "patient-4", 5);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("already rated"));
        }

        [Test]
        public async Task SubmitRating_RecalculatesDoctorRating()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Recalc", UserId = "doc-5", Rating = 0, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            // Two sessions with different patients
            var session1 = new Session
            {
                DoctorId = doctor.Id, PatientId = "p1",
                StartTime = DateTimeOffset.UtcNow.AddDays(-3), EndTime = DateTimeOffset.UtcNow.AddDays(-3).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p1@e.com" }
            };
            var session2 = new Session
            {
                DoctorId = doctor.Id, PatientId = "p2",
                StartTime = DateTimeOffset.UtcNow.AddDays(-2), EndTime = DateTimeOffset.UtcNow.AddDays(-2).AddHours(1),
                Price = 100, SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P2", PhoneNumber = "2", Email = "p2@e.com" }
            };
            context.Sessions.AddRange(session1, session2);
            await context.SaveChangesAsync();

            var service = new RatingService(context);

            // First rating: 4 stars
            await service.SubmitRatingAsync(session1.Id, "p1", 4);
            // Second rating: 2 stars
            await service.SubmitRatingAsync(session2.Id, "p2", 2);

            var updatedDoctor = await context.Doctors.FindAsync(doctor.Id);
            Assert.That(updatedDoctor!.Rating, Is.EqualTo(3.0m));
        }
    }
}
