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
            Assert.AreEqual(0, result);
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
            Assert.AreEqual(3, result);
        }

        [Test]
        public void MapMoodScore_KnownValues_ReturnsExpected()
        {
            Assert.AreEqual(5.0, _service.MapMoodScore(JournalMood.Happy));
            Assert.AreEqual(4.5, _service.MapMoodScore(JournalMood.Excited));
            Assert.AreEqual(1.0, _service.MapMoodScore(JournalMood.Angry));
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
            Assert.AreEqual(3, dist.Count);
            Assert.AreEqual(2, dist[JournalMood.Happy.ToString()]);
            Assert.IsTrue(dist.ContainsKey(JournalMood.Sad.ToString()));
            Assert.IsTrue(dist.ContainsKey(JournalMood.Anxious.ToString()));
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
            Assert.IsNotNull(scores);
            Assert.AreEqual(2, scores.Count);
        }
    }
}
