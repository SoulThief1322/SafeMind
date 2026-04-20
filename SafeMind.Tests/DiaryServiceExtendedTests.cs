using NUnit.Framework;
using SafeMind.Services;
using SafeMind.Data.Enums;
using SafeMind.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DiaryServiceExtendedTests
    {
        private DiaryService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new DiaryService();
        }

        // ── CalculateStreak edge cases ──

        [Test]
        public async Task CalculateStreak_SingleDay_ReturnsOne()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var checks = new List<DailyCheck>
            {
                new DailyCheck { CreatedOn = today }
            };

            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task CalculateStreak_BrokenStreak_ReturnsLastStreak()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var checks = new List<DailyCheck>
            {
                new DailyCheck { CreatedOn = today.AddDays(-5) },
                new DailyCheck { CreatedOn = today.AddDays(-4) },
                // gap on day -3
                new DailyCheck { CreatedOn = today.AddDays(-2) },
                new DailyCheck { CreatedOn = today.AddDays(-1) },
                new DailyCheck { CreatedOn = today }
            };

            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(3)); // today, -1, -2
        }

        [Test]
        public async Task CalculateStreak_MultipleSameDay_CountsAsOne()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var checks = new List<DailyCheck>
            {
                new DailyCheck { CreatedOn = today },
                new DailyCheck { CreatedOn = today }, // duplicate
                new DailyCheck { CreatedOn = today.AddDays(-1) }
            };

            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public async Task CalculateStreak_OldCheck_NotConnectedToToday_ReturnsOne()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var checks = new List<DailyCheck>
            {
                new DailyCheck { CreatedOn = today.AddDays(-30) },
                new DailyCheck { CreatedOn = today }
            };

            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(1));
        }

        // ── MapMoodScore full coverage ──

        [Test]
        public void MapMoodScore_Calm_Returns4()
        {
            Assert.That(_service.MapMoodScore(JournalMood.Calm), Is.EqualTo(4.0));
        }

        [Test]
        public void MapMoodScore_Anxious_Returns2()
        {
            Assert.That(_service.MapMoodScore(JournalMood.Anxious), Is.EqualTo(2.0));
        }

        [Test]
        public void MapMoodScore_Sad_Returns1_5()
        {
            Assert.That(_service.MapMoodScore(JournalMood.Sad), Is.EqualTo(1.5));
        }

        [Test]
        public void MapMoodScore_UnknownValue_Returns3()
        {
            Assert.That(_service.MapMoodScore((JournalMood)999), Is.EqualTo(3.0));
        }

        // ── GetMoodDistribution edge cases ──

        [Test]
        public async Task GetMoodDistribution_EmptyInputs_ReturnsEmpty()
        {
            var journals = new List<Journal>().AsQueryable();
            var checks = new List<DailyCheck>().AsQueryable();

            var dist = await _service.GetMoodDistribution(journals, checks);

            Assert.That(dist.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetMoodDistribution_OnlyJournals_Works()
        {
            var journals = new List<Journal>
            {
                new Journal { Mood = JournalMood.Happy },
                new Journal { Mood = JournalMood.Happy },
                new Journal { Mood = JournalMood.Sad }
            }.AsQueryable();
            var checks = new List<DailyCheck>().AsQueryable();

            var dist = await _service.GetMoodDistribution(journals, checks);

            Assert.That(dist["Happy"], Is.EqualTo(2));
            Assert.That(dist["Sad"], Is.EqualTo(1));
        }

        [Test]
        public async Task GetMoodDistribution_OnlyChecks_Works()
        {
            var journals = new List<Journal>().AsQueryable();
            var checks = new List<DailyCheck>
            {
                new DailyCheck { Mood = JournalMood.Calm },
                new DailyCheck { Mood = JournalMood.Calm }
            }.AsQueryable();

            var dist = await _service.GetMoodDistribution(journals, checks);

            Assert.That(dist["Calm"], Is.EqualTo(2));
        }

        // ── GetMoodScores edge cases ──

        [Test]
        public async Task GetMoodScores_EmptyInputs_ReturnsEmpty()
        {
            var journals = new List<Journal>().AsQueryable();
            var checks = new List<DailyCheck>().AsQueryable();

            var scores = await _service.GetMoodScores(journals, _service, checks);

            Assert.That(scores.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetMoodScores_MultipleEntries_ReturnsAllScores()
        {
            var journals = new List<Journal>
            {
                new Journal { Mood = JournalMood.Happy },
                new Journal { Mood = JournalMood.Angry }
            }.AsQueryable();
            var checks = new List<DailyCheck>
            {
                new DailyCheck { Mood = JournalMood.Calm }
            }.AsQueryable();

            var scores = await _service.GetMoodScores(journals, _service, checks);

            Assert.That(scores.Count, Is.EqualTo(3));
            Assert.That(scores, Does.Contain(5.0));  // Happy
            Assert.That(scores, Does.Contain(1.0));  // Angry
            Assert.That(scores, Does.Contain(4.0));  // Calm
        }
    }
}
