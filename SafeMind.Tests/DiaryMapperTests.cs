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

            Assert.AreEqual(journal.Title, vm.Title);
            Assert.AreEqual(journal.Content, vm.Content);
            Assert.AreEqual(journal.Mood, vm.Mood);
            Assert.AreEqual(journal.CreatedAt, vm.CreatedOn);
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

            Assert.AreEqual(check.Notes, vm.Notes);
            Assert.AreEqual(check.Mood, vm.Mood);
            Assert.AreEqual(check.CreatedOn, vm.CreatedOn);
        }
    }
}
