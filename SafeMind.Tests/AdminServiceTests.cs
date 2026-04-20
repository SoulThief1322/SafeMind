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
    public class AdminServiceTests
    {
        private async Task<SafeMind.Data.SafeMindDbContext> CreateSeededContext()
        {
            var context = TestDbContextFactory.Create();

            // Seed identity users (needed for Patient navigation in Sessions)
            context.Users.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "p1", UserName = "patient1", Email = "p1@e.com" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "p2", UserName = "patient2", Email = "p2@e.com" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "d1", UserName = "doctor1", Email = "d1@e.com" },
                new Microsoft.AspNetCore.Identity.IdentityUser { Id = "d2", UserName = "doctor2", Email = "d2@e.com" }
            );

            // Seed articles
            context.Articles.AddRange(
                new Article { Headline = "Article 1", Content = "C1", AuthorId = "a1", IsDeleted = false },
                new Article { Headline = "Article 2", Content = "C2", AuthorId = "a1", IsDeleted = false },
                new Article { Headline = "Deleted", Content = "C3", AuthorId = "a1", IsDeleted = true }
            );

            // Seed contact messages
            context.ContactMessages.AddRange(
                new ContactMessage { FullName = "User 1", Email = "u1@e.com", Subject = "S1", Message = "M1", IsRead = false, IsArchived = false },
                new ContactMessage { FullName = "User 2", Email = "u2@e.com", Subject = "S2", Message = "M2", IsRead = true, IsArchived = false },
                new ContactMessage { FullName = "User 3", Email = "u3@e.com", Subject = "S3", Message = "M3", IsRead = false, IsArchived = true }
            );

            // Seed doctors
            context.Doctors.AddRange(
                new Doctor { Name = "Dr. A", UserId = "d1", Biography = "Bio", WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0), SessionDuration = 60, Price = 100, Rating = 4.0m },
                new Doctor { Name = "Dr. B", UserId = "d2", Biography = "Bio", WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0), SessionDuration = 60, Price = 120, Rating = 4.5m }
            );
            await context.SaveChangesAsync();

            // Seed sessions
            var doctor1 = await context.Doctors.FirstAsync();
            context.Sessions.AddRange(
                new Session
                {
                    DoctorId = doctor1.Id, PatientId = "p1", StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                    EndTime = DateTimeOffset.UtcNow.AddDays(-1).AddHours(1), Price = 100,
                    SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid,
                    Contact = new SessionContact { FullName = "P1", PhoneNumber = "1", Email = "p@e.com" }
                },
                new Session
                {
                    DoctorId = doctor1.Id, PatientId = "p2", StartTime = DateTimeOffset.UtcNow.AddDays(1),
                    EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1), Price = 100,
                    SessionStatus = SessionStatus.Scheduled, PaymentStatus = PaymentStatus.Pending,
                    Contact = new SessionContact { FullName = "P2", PhoneNumber = "2", Email = "p2@e.com" }
                }
            );

            // Seed goal templates
            context.GoalTemplates.AddRange(
                new GoalTemplate { Description = "Goal 1" },
                new GoalTemplate { Description = "Goal 2" }
            );

            await context.SaveChangesAsync();
            return context;
        }

        [Test]
        public async Task GetTotalDoctors_ReturnsCorrectCount()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var count = await service.GetTotalDoctorsAsync();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetTotalSessions_ReturnsCorrectCount()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var count = await service.GetTotalSessionsAsync();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetTotalArticles_ExcludesDeleted()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var count = await service.GetTotalArticlesAsync();
            Assert.That(count, Is.EqualTo(2)); // 1 deleted excluded
        }

        [Test]
        public async Task GetUnreadContactCount_ExcludesArchivedAndRead()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var count = await service.GetUnreadContactCountAsync();
            Assert.That(count, Is.EqualTo(1)); // Only the first one
        }

        [Test]
        public async Task GetCompletedSessionsCount_ReturnsCorrect()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var count = await service.GetCompletedSessionsCountAsync();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetContactMessages_ExcludesArchived()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var messages = await service.GetContactMessagesAsync(false);
            Assert.That(messages.Count, Is.EqualTo(2)); // Archived excluded
        }

        [Test]
        public async Task GetContactMessages_IncludesArchived()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var messages = await service.GetContactMessagesAsync(true);
            Assert.That(messages.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetContactMessage_ExistingId_ReturnsMessage()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var first = await context.ContactMessages.FirstAsync();
            var msg = await service.GetContactMessageAsync(first.Id);

            Assert.That(msg, Is.Not.Null);
            Assert.That(msg!.FullName, Is.EqualTo("User 1"));
        }

        [Test]
        public async Task GetContactMessage_NonExistentId_ReturnsNull()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var msg = await service.GetContactMessageAsync(9999);
            Assert.That(msg, Is.Null);
        }

        [Test]
        public async Task MarkContactAsRead_SetsIsReadTrue()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var msg = await context.ContactMessages.FirstAsync(c => !c.IsRead);
            await service.MarkContactAsReadAsync(msg.Id);

            var updated = await context.ContactMessages.FindAsync(msg.Id);
            Assert.That(updated!.IsRead, Is.True);
        }

        [Test]
        public async Task MarkContactAsRead_AlreadyRead_NoChange()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var msg = await context.ContactMessages.FirstAsync(c => c.IsRead);
            await service.MarkContactAsReadAsync(msg.Id);

            var updated = await context.ContactMessages.FindAsync(msg.Id);
            Assert.That(updated!.IsRead, Is.True);
        }

        [Test]
        public async Task ArchiveContact_SetsIsArchivedTrue()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var msg = await context.ContactMessages.FirstAsync(c => !c.IsArchived);
            await service.ArchiveContactAsync(msg.Id);

            var updated = await context.ContactMessages.FindAsync(msg.Id);
            Assert.That(updated!.IsArchived, Is.True);
        }

        [Test]
        public async Task DeleteContact_RemovesMessage()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var msg = await context.ContactMessages.FirstAsync();
            var id = msg.Id;
            await service.DeleteContactAsync(id);

            var deleted = await context.ContactMessages.FindAsync(id);
            Assert.That(deleted, Is.Null);
        }

        [Test]
        public async Task DeleteContact_NonExistent_NoError()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            Assert.DoesNotThrowAsync(async () => await service.DeleteContactAsync(9999));
        }

        // ── Goal Templates ──

        [Test]
        public async Task GetGoalTemplates_ReturnsAll()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var templates = await service.GetGoalTemplatesAsync();
            Assert.That(templates.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task AddGoalTemplate_AddsToDb()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            await service.AddGoalTemplateAsync("New Goal");

            var templates = await context.GoalTemplates.ToListAsync();
            Assert.That(templates.Any(t => t.Description == "New Goal"), Is.True);
        }

        [Test]
        public async Task UpdateGoalTemplate_UpdatesDescription()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var template = await context.GoalTemplates.FirstAsync();
            await service.UpdateGoalTemplateAsync(template.Id, "Updated Description");

            var updated = await context.GoalTemplates.FindAsync(template.Id);
            Assert.That(updated!.Description, Is.EqualTo("Updated Description"));
        }

        [Test]
        public async Task UpdateGoalTemplate_NonExistent_NoError()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            Assert.DoesNotThrowAsync(async () => await service.UpdateGoalTemplateAsync(9999, "New"));
        }

        [Test]
        public async Task DeleteGoalTemplate_RemovesFromDb()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var template = await context.GoalTemplates.FirstAsync();
            var id = template.Id;
            await service.DeleteGoalTemplateAsync(id);

            var deleted = await context.GoalTemplates.FindAsync(id);
            Assert.That(deleted, Is.Null);
        }

        // ── Reports ──

        [Test]
        public async Task GetSessionsByStatus_ReturnsGroupedCounts()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var result = await service.GetSessionsByStatusAsync();

            Assert.That(result.ContainsKey(SessionStatus.Completed.ToString()), Is.True);
            Assert.That(result[SessionStatus.Completed.ToString()], Is.EqualTo(1));
        }

        [Test]
        public async Task GetRecentSessions_ReturnsRequestedCount()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var recent = await service.GetRecentSessionsAsync(1);
            Assert.That(recent.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRecentSessions_DefaultCount_ReturnsUpTo5()
        {
            using var context = await CreateSeededContext();
            var service = new AdminService(context, null!);

            var recent = await service.GetRecentSessionsAsync();
            Assert.That(recent.Count, Is.LessThanOrEqualTo(5));
        }
    }
}
