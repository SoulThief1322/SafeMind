using NUnit.Framework;
using SafeMind.Services;
using SafeMind.Data.Enums;
using System.Collections.Generic;
using System.Linq;
using System;
using SafeMind.Data.Models;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DiaryServiceTests
    {
        private DiaryService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new DiaryService();
        }

        [Test]
        public async System.Threading.Tasks.Task CalculateStreak_NoChecks_ReturnsZero()
        {
            var checks = new List<DailyCheck>();
            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async System.Threading.Tasks.Task CalculateStreak_ConsecutiveDays_ReturnsCorrect()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var checks = new List<DailyCheck>
            {
                new DailyCheck { CreatedOn = today.AddDays(-2) },
                new DailyCheck { CreatedOn = today.AddDays(-1) },
                new DailyCheck { CreatedOn = today }
            };
            var result = await _service.CalculateStreak(checks);
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void MapMoodScore_KnownValues_ReturnsExpected()
        {
            Assert.That(_service.MapMoodScore(JournalMood.Happy), Is.EqualTo(5.0));
            Assert.That(_service.MapMoodScore(JournalMood.Excited), Is.EqualTo(4.5));
            Assert.That(_service.MapMoodScore(JournalMood.Angry), Is.EqualTo(1.0));
        }

        [Test]
        public async System.Threading.Tasks.Task GetMoodDistribution_CombinesJournalsAndChecks()
        {
            var journals = new List<Journal>
            {
                new Journal { Mood = JournalMood.Happy },
                new Journal { Mood = JournalMood.Sad }
            }.AsQueryable();

            var checks = new List<DailyCheck>
            {
                new DailyCheck { Mood = JournalMood.Happy },
                new DailyCheck { Mood = JournalMood.Anxious }
            }.AsQueryable();

            var dist = await _service.GetMoodDistribution(journals, checks);
            Assert.That(dist.Count, Is.EqualTo(3));
            Assert.That(dist[JournalMood.Happy.ToString()], Is.EqualTo(2));
            Assert.That(dist.ContainsKey(JournalMood.Sad.ToString()), Is.True);
            Assert.That(dist.ContainsKey(JournalMood.Anxious.ToString()), Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task GetMoodScores_ReturnsScores()
        {
            var journals = new List<Journal>
            {
                new Journal { Mood = JournalMood.Calm }
            }.AsQueryable();
            var checks = new List<DailyCheck>
            {
                new DailyCheck { Mood = JournalMood.Happy }
            }.AsQueryable();

            var scores = await _service.GetMoodScores(journals, _service, checks);
            Assert.That(scores, Is.Not.Null);
            Assert.That(scores.Count, Is.EqualTo(2));
        }
    }
}
