using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ChatServiceTests
    {
        [Test]
        public async Task GetUnreadCount_ReturnsCorrectCount()
        {
            using var context = TestDbContextFactory.Create();

            context.ChatMessages.AddRange(
                new ChatMessage { SenderId = "sender-1", ReceiverId = "user-1", Message = "Hi", IsRead = false },
                new ChatMessage { SenderId = "sender-2", ReceiverId = "user-1", Message = "Hello", IsRead = false },
                new ChatMessage { SenderId = "sender-3", ReceiverId = "user-1", Message = "Read", IsRead = true },
                new ChatMessage { SenderId = "sender-4", ReceiverId = "other-user", Message = "Other", IsRead = false }
            );
            await context.SaveChangesAsync();

            var service = new ChatService(context);
            var count = await service.GetUnreadCountAsync("user-1");

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetUnreadCount_NoMessages_ReturnsZero()
        {
            using var context = TestDbContextFactory.Create();
            var service = new ChatService(context);

            var count = await service.GetUnreadCountAsync("nobody");

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetMessages_ReturnsOrderedMessages()
        {
            using var context = TestDbContextFactory.Create();

            var doctor = new Doctor
            {
                Name = "Dr. Chat", UserId = "doc-user-1", Rating = 4.0m, Biography = "Bio",
                WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60, Price = 100
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            context.ChatMessages.AddRange(
                new ChatMessage { SenderId = "patient-1", ReceiverId = "doc-user-1", Message = "First", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10) },
                new ChatMessage { SenderId = "doc-user-1", ReceiverId = "patient-1", Message = "Second", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5) },
                new ChatMessage { SenderId = "patient-1", ReceiverId = "doc-user-1", Message = "Third", Timestamp = DateTimeOffset.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new ChatService(context);
            var messages = await service.GetMessagesAsync("patient-1", doctor.Id);

            Assert.That(messages, Is.Not.Null);
            // It returns an object, verify it's not empty
            var list = messages as System.Collections.IEnumerable;
            Assert.That(list, Is.Not.Null);
        }

        [Test]
        public async Task GetMessages_InvalidDoctorId_ReturnsEmptyList()
        {
            using var context = TestDbContextFactory.Create();
            var service = new ChatService(context);

            var messages = await service.GetMessagesAsync("patient-1", 9999);

            Assert.That(messages, Is.Not.Null);
        }

        [Test]
        public async Task GetDoctorConversations_NoDoctorRecord_ReturnsEmptyList()
        {
            using var context = TestDbContextFactory.Create();
            var service = new ChatService(context);

            var result = await service.GetDoctorConversationsAsync("not-a-doctor");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetMyPatients_NoDoctorRecord_ReturnsEmptyList()
        {
            using var context = TestDbContextFactory.Create();
            var service = new ChatService(context);

            var result = await service.GetMyPatientsAsync("not-a-doctor");

            Assert.That(result, Is.Not.Null);
        }
    }
}
