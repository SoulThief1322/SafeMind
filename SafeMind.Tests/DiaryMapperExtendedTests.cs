using NUnit.Framework;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using SafeMind.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DiaryMapperExtendedTests
    {
        // ── ToViewModels (Journal collection) ──

        [Test]
        public void ToViewModels_Journals_MapsAll()
        {
            var journals = new List<Journal>
            {
                new Journal { Id = 1, Title = "Day 1", Content = "Content 1", CreatedAt = DateTimeOffset.UtcNow, Mood = JournalMood.Happy, Category = JournalCategories.Personal, UserId = "u1" },
                new Journal { Id = 2, Title = "Day 2", Content = "Content 2", CreatedAt = DateTimeOffset.UtcNow, Mood = JournalMood.Sad, Category = JournalCategories.Work, UserId = "u1" }
            };

            var vms = DiaryMapper.ToViewModels(journals).ToList();

            Assert.That(vms.Count, Is.EqualTo(2));
            Assert.That(vms[0].Title, Is.EqualTo("Day 1"));
            Assert.That(vms[1].Title, Is.EqualTo("Day 2"));
        }

        [Test]
        public void ToViewModels_EmptyJournals_ReturnsEmpty()
        {
            var result = DiaryMapper.ToViewModels(new List<Journal>()).ToList();
            Assert.That(result.Count, Is.EqualTo(0));
        }

        // ── ToViewModels (DailyCheck collection) ──

        [Test]
        public void ToViewModels_DailyChecks_MapsAll()
        {
            var checks = new List<DailyCheck>
            {
                new DailyCheck { Id = 1, Mood = JournalMood.Calm, Energy = EnergyLevel.High, Stress = StressLevel.Low, Sleep = SleepQuality.Good, Notes = "Note 1", CreatedOn = DateTimeOffset.UtcNow, UserId = "u1" },
                new DailyCheck { Id = 2, Mood = JournalMood.Anxious, Energy = EnergyLevel.Low, Stress = StressLevel.High, Sleep = SleepQuality.Poor, Notes = "Note 2", CreatedOn = DateTimeOffset.UtcNow, UserId = "u1" }
            };

            var vms = DiaryMapper.ToViewModels(checks).ToList();

            Assert.That(vms.Count, Is.EqualTo(2));
            Assert.That(vms[0].Notes, Is.EqualTo("Note 1"));
            Assert.That(vms[1].Notes, Is.EqualTo("Note 2"));
        }

        // ── ToEntity (Journal) ──

        [Test]
        public void ToEntity_Journal_MapsFieldsCorrectly()
        {
            var request = new NewJournalEntryRequest
            {
                Title = "  My Title  ",
                Mood = JournalMood.Excited,
                Category = JournalCategories.Health,
                Content = "  Some content  "
            };

            var entity = DiaryMapper.ToEntity(request, "user-123");

            Assert.That(entity.UserId, Is.EqualTo("user-123"));
            Assert.That(entity.Title, Is.EqualTo("My Title"));
            Assert.That(entity.Content, Is.EqualTo("Some content"));
            Assert.That(entity.Mood, Is.EqualTo(JournalMood.Excited));
            Assert.That(entity.Category, Is.EqualTo(JournalCategories.Health));
        }

        [Test]
        public void ToEntity_Journal_NullTitle_DefaultsToEmpty()
        {
            var request = new NewJournalEntryRequest
            {
                Title = null!,
                Content = "content"
            };

            var entity = DiaryMapper.ToEntity(request, "user-1");
            Assert.That(entity.Title, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToEntity_Journal_NullContent_DefaultsToEmpty()
        {
            var request = new NewJournalEntryRequest
            {
                Title = "title",
                Content = null!
            };

            var entity = DiaryMapper.ToEntity(request, "user-1");
            Assert.That(entity.Content, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToEntity_Journal_SetsCreatedAt()
        {
            var before = DateTimeOffset.UtcNow;
            var entity = DiaryMapper.ToEntity(new NewJournalEntryRequest { Title = "t", Content = "c" }, "u1");
            var after = DateTimeOffset.UtcNow;

            Assert.That(entity.CreatedAt, Is.GreaterThanOrEqualTo(before));
            Assert.That(entity.CreatedAt, Is.LessThanOrEqualTo(after));
        }

        // ── ToEntity (DailyCheck) ──

        [Test]
        public void ToEntity_DailyCheck_MapsFieldsCorrectly()
        {
            var request = new SaveDailyCheckRequest
            {
                Mood = JournalMood.Angry,
                Energy = EnergyLevel.Low,
                Stress = StressLevel.High,
                Sleep = SleepQuality.Poor,
                Notes = "  Trimmed notes  "
            };

            var entity = DiaryMapper.ToEntity(request, "user-456");

            Assert.That(entity.UserId, Is.EqualTo("user-456"));
            Assert.That(entity.Mood, Is.EqualTo(JournalMood.Angry));
            Assert.That(entity.Energy, Is.EqualTo(EnergyLevel.Low));
            Assert.That(entity.Stress, Is.EqualTo(StressLevel.High));
            Assert.That(entity.Sleep, Is.EqualTo(SleepQuality.Poor));
            Assert.That(entity.Notes, Is.EqualTo("Trimmed notes"));
        }

        [Test]
        public void ToEntity_DailyCheck_NullNotes_DefaultsToEmpty()
        {
            var request = new SaveDailyCheckRequest
            {
                Mood = JournalMood.Happy,
                Notes = null!
            };

            var entity = DiaryMapper.ToEntity(request, "u1");
            Assert.That(entity.Notes, Is.EqualTo(string.Empty));
        }

        // ── Insights ToViewModel ──

        [Test]
        public void ToViewModel_Insights_MapsAllFields()
        {
            var dist = new Dictionary<string, int>
            {
                { "Happy", 5 }, { "Sad", 2 }
            };

            var vm = DiaryMapper.ToViewModel(10, 8, 3, 3.5, dist, 7);

            Assert.That(vm.TotalJournals, Is.EqualTo(10));
            Assert.That(vm.TotalCheckIns, Is.EqualTo(8));
            Assert.That(vm.TotalGoals, Is.EqualTo(3));
            Assert.That(vm.AverageMoodScore, Is.EqualTo(3.5));
            Assert.That(vm.MoodDistribution.Count, Is.EqualTo(2));
            Assert.That(vm.DayStreak, Is.EqualTo(7));
        }

        [Test]
        public void ToViewModel_Insights_NullAverageMood()
        {
            var vm = DiaryMapper.ToViewModel(0, 0, 0, null, new Dictionary<string, int>(), 0);

            Assert.That(vm.AverageMoodScore, Is.Null);
            Assert.That(vm.TotalJournals, Is.EqualTo(0));
        }
    }
}
