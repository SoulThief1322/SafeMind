using NUnit.Framework;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Services;

using System;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DiaryMapperTests
    {
        [Test]
        public void Journal_ToViewModel_MapsFields()
        {
            var journal = new Journal
            {
                Id = 1,
                Title = "Test",
                Content = "Hello",
                CreatedAt = DateTimeOffset.UtcNow,
                Mood = JournalMood.Happy,
                Category = JournalCategories.Personal,
                UserId = "user1"
            };

            var vm = DiaryMapper.ToViewModel(journal);

            Assert.That(vm.Title, Is.EqualTo(journal.Title));
            Assert.That(vm.Content, Is.EqualTo(journal.Content));
            Assert.That(vm.Mood, Is.EqualTo(journal.Mood));
            Assert.That(vm.CreatedOn, Is.EqualTo(journal.CreatedAt));
        }

        [Test]
        public void DailyCheck_ToViewModel_MapsFields()
        {
            var check = new DailyCheck
            {
                Id = 2,
                CreatedOn = DateTimeOffset.UtcNow,
                Mood = JournalMood.Calm,
                Energy = EnergyLevel.Medium,
                Stress = StressLevel.Medium,
                Sleep = SleepQuality.Fair,
                Notes = "Note",
                UserId = "user1"
            };

            var vm = DiaryMapper.ToViewModel(check);

            Assert.That(vm.Notes, Is.EqualTo(check.Notes));
            Assert.That(vm.Mood, Is.EqualTo(check.Mood));
            Assert.That(vm.CreatedOn, Is.EqualTo(check.CreatedOn));
        }
    }
}
