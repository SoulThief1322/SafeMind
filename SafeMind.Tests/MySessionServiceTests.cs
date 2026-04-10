using NUnit.Framework;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class MySessionServiceTests
    {
        private MySessionService CreateService(SafeMind.Data.SafeMindDbContext context)
        {
            var bookService = new BookService(context);
            var bookSessionService = new BookSessionService(context, bookService);
            var slotsService = new SlotsService();
            return new MySessionService(context, bookSessionService, slotsService);
        }

        private async Task<(Doctor doctor, Session session)> SeedDoctorAndSession(
            SafeMind.Data.SafeMindDbContext context,
            SessionStatus status = SessionStatus.Scheduled,
            PaymentStatus payment = PaymentStatus.Paid,
            int daysOffset = 5)
        {
            var doctor = new Doctor
            {
                Name = "Dr. Test", UserId = "doc-user-1", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-1",
                StartTime = DateTimeOffset.UtcNow.AddDays(daysOffset),
                EndTime = DateTimeOffset.UtcNow.AddDays(daysOffset).AddHours(1),
                Price = 100, SessionStatus = status, PaymentStatus = payment,
                Contact = new SessionContact { FullName = "Patient One", PhoneNumber = "123456", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            return (doctor, session);
        }

        // ── Cancel ──

        [Test]
        public async Task CancelSession_ValidSession_Succeeds()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, daysOffset: 5);
            var service = CreateService(context);

            var (success, message) = await service.CancelSessionAsync(session.Id, "patient-1", false);

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("cancelled successfully"));
            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.SessionStatus, Is.EqualTo(SessionStatus.Cancelled));
        }

        [Test]
        public async Task CancelSession_PaidSession_GetsRefunded()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, payment: PaymentStatus.Paid, daysOffset: 5);
            var service = CreateService(context);

            await service.CancelSessionAsync(session.Id, "patient-1", false);

            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.PaymentStatus, Is.EqualTo(PaymentStatus.Refunded));
        }

        [Test]
        public async Task CancelSession_AlreadyCancelled_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, status: SessionStatus.Cancelled);
            var service = CreateService(context);

            var (success, message) = await service.CancelSessionAsync(session.Id, "patient-1", false);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("already cancelled"));
        }

        [Test]
        public async Task CancelSession_LessThan24Hours_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var doctor = new Doctor
            {
                Name = "Dr. Test", UserId = "doc-user-2", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            var session = new Session
            {
                DoctorId = doctor.Id, PatientId = "patient-2",
                StartTime = DateTimeOffset.UtcNow.AddHours(12), // Less than 24 hours
                EndTime = DateTimeOffset.UtcNow.AddHours(13),
                Price = 100, SessionStatus = SessionStatus.Scheduled, PaymentStatus = PaymentStatus.Paid,
                Contact = new SessionContact { FullName = "P", PhoneNumber = "1", Email = "p@e.com" }
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var (success, message) = await service.CancelSessionAsync(session.Id, "patient-2", false);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("24 hours"));
        }

        [Test]
        public async Task CancelSession_NotFound_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var service = CreateService(context);

            var (success, message) = await service.CancelSessionAsync(999, "patient-1", false);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }

        // ── Confirm ──

        [Test]
        public async Task ConfirmSession_ValidSession_Succeeds()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var (success, message) = await service.ConfirmSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("confirmed successfully"));
            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.SessionStatus, Is.EqualTo(SessionStatus.Confirmed));
        }

        [Test]
        public async Task ConfirmSession_AlreadyConfirmed_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, status: SessionStatus.Confirmed);
            var service = CreateService(context);

            var (success, message) = await service.ConfirmSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("already been confirmed"));
        }

        [Test]
        public async Task ConfirmSession_WrongDoctor_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var (success, message) = await service.ConfirmSessionAsync(session.Id, "wrong-doc-user");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }

        // ── Complete ──

        [Test]
        public async Task CompleteSession_ValidPastSession_Succeeds()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, status: SessionStatus.Confirmed, daysOffset: -2);
            var service = CreateService(context);

            var (success, message) = await service.CompleteSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("completed"));
            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.SessionStatus, Is.EqualTo(SessionStatus.Completed));
        }

        [Test]
        public async Task CompleteSession_FutureSession_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, daysOffset: 5);
            var service = CreateService(context);

            var (success, message) = await service.CompleteSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("past sessions"));
        }

        [Test]
        public async Task CompleteSession_AlreadyCompleted_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, status: SessionStatus.Completed, daysOffset: -2);
            var service = CreateService(context);

            var (success, message) = await service.CompleteSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("already completed"));
        }

        [Test]
        public async Task CompleteSession_CancelledSession_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, status: SessionStatus.Cancelled, daysOffset: -2);
            var service = CreateService(context);

            var (success, message) = await service.CompleteSessionAsync(session.Id, "doc-user-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("Cancelled"));
        }

        // ── Payment ──

        [Test]
        public async Task ProcessPayment_ValidSession_Succeeds()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, payment: PaymentStatus.Pending);
            var service = CreateService(context);

            var (success, message) = await service.ProcessPaymentAsync(session.Id, "patient-1");

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("Payment successful"));
            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.That(updated!.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
        }

        [Test]
        public async Task ProcessPayment_AlreadyPaid_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, payment: PaymentStatus.Paid);
            var service = CreateService(context);

            var (success, message) = await service.ProcessPaymentAsync(session.Id, "patient-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("already been paid"));
        }

        [Test]
        public async Task ProcessPayment_SessionNotFound_Fails()
        {
            using var context = TestDbContextFactory.Create();
            var service = CreateService(context);

            var (success, message) = await service.ProcessPaymentAsync(999, "patient-1");

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }

        // ── GetSessionForUser ──

        [Test]
        public async Task GetSessionForUser_Patient_ReturnsSession()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var found = await service.GetSessionForUser(session.Id, "patient-1", false);

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Id, Is.EqualTo(session.Id));
        }

        [Test]
        public async Task GetSessionForUser_Doctor_ReturnsSession()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var found = await service.GetSessionForUser(session.Id, "doc-user-1", true);

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Id, Is.EqualTo(session.Id));
        }

        [Test]
        public async Task GetSessionForUser_WrongUser_ReturnsNull()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var found = await service.GetSessionForUser(session.Id, "wrong-user", false);

            Assert.That(found, Is.Null);
        }

        // ── GetPayableSession ──

        [Test]
        public async Task GetPayableSession_ReturnsSessionForPatient()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context, payment: PaymentStatus.Pending);
            var service = CreateService(context);

            var found = await service.GetPayableSession(session.Id, "patient-1");

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.PatientId, Is.EqualTo("patient-1"));
        }

        [Test]
        public async Task GetPayableSession_WrongPatient_ReturnsNull()
        {
            using var context = TestDbContextFactory.Create();
            var (doctor, session) = await SeedDoctorAndSession(context);
            var service = CreateService(context);

            var found = await service.GetPayableSession(session.Id, "not-the-patient");

            Assert.That(found, Is.Null);
        }
    }
}
