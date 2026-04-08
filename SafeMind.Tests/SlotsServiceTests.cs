using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeMind.Tests
{
    [TestFixture]
    public class SlotsServiceTests
    {
        private SlotsService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new SlotsService();
        }

        // ── TryParseSlots ──

        [Test]
        public void TryParseSlots_NullInput_ReturnsFalse()
        {
            var result = _service.TryParseSlots(null, out var doctorId, out var slots, out var error);
            Assert.That(result, Is.False);
            Assert.That(doctorId, Is.EqualTo(0));
            Assert.That(slots, Is.Null);
        }

        [Test]
        public void TryParseSlots_EmptyString_ReturnsFalse()
        {
            var result = _service.TryParseSlots("", out _, out _, out _);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryParseSlots_WhitespaceOnly_ReturnsFalse()
        {
            var result = _service.TryParseSlots("   ", out _, out _, out _);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryParseSlots_ValidArrayFormat_ReturnsTrue()
        {
            var json = @"[{""date"":""2026-05-01"",""time"":""09:00""}]";
            var result = _service.TryParseSlots(json, out var doctorId, out var slots, out _);

            Assert.That(result, Is.True);
            Assert.That(slots, Is.Not.Null);
            Assert.That(slots!.Count, Is.EqualTo(1));
            Assert.That(slots[0].Date, Is.EqualTo(new DateTime(2026, 5, 1)));
            Assert.That(slots[0].Time, Is.EqualTo(TimeSpan.FromHours(9)));
        }

        [Test]
        public void TryParseSlots_ValidObjectFormat_ReturnsTrueWithDoctorId()
        {
            var json = @"{""doctorId"":42,""slots"":[{""date"":""2026-06-15"",""time"":""14:30""}]}";
            var result = _service.TryParseSlots(json, out var doctorId, out var slots, out _);

            Assert.That(result, Is.True);
            Assert.That(doctorId, Is.EqualTo(42));
            Assert.That(slots, Is.Not.Null);
            Assert.That(slots!.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryParseSlots_MultipleSlots_ReturnsAllSorted()
        {
            var json = @"[
                {""date"":""2026-05-02"",""time"":""14:00""},
                {""date"":""2026-05-01"",""time"":""09:00""},
                {""date"":""2026-05-01"",""time"":""10:00""}
            ]";
            var result = _service.TryParseSlots(json, out _, out var slots, out _);

            Assert.That(result, Is.True);
            Assert.That(slots!.Count, Is.EqualTo(3));
            // Should be sorted by date then time
            Assert.That(slots[0].Date, Is.EqualTo(new DateTime(2026, 5, 1)));
            Assert.That(slots[0].Time, Is.EqualTo(TimeSpan.FromHours(9)));
            Assert.That(slots[1].Time, Is.EqualTo(TimeSpan.FromHours(10)));
            Assert.That(slots[2].Date, Is.EqualTo(new DateTime(2026, 5, 2)));
        }

        [Test]
        public void TryParseSlots_DuplicateSlots_Deduplicated()
        {
            var json = @"[
                {""date"":""2026-05-01"",""time"":""09:00""},
                {""date"":""2026-05-01"",""time"":""09:00""}
            ]";
            var result = _service.TryParseSlots(json, out _, out var slots, out _);

            Assert.That(result, Is.True);
            Assert.That(slots!.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryParseSlots_InvalidJson_ReturnsFalse()
        {
            var result = _service.TryParseSlots("not json at all", out _, out _, out var error);

            Assert.That(result, Is.False);
            Assert.That(error, Is.EqualTo("Invalid slot selection."));
        }

        [Test]
        public void TryParseSlots_EmptyArray_ReturnsFalse()
        {
            var result = _service.TryParseSlots("[]", out _, out _, out _);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryParseSlots_InvalidDateInSlot_FiltersOut()
        {
            var json = @"[{""date"":""not-a-date"",""time"":""09:00""}]";
            var result = _service.TryParseSlots(json, out _, out _, out _);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryParseSlots_InvalidTimeInSlot_FiltersOut()
        {
            var json = @"[{""date"":""2026-05-01"",""time"":""not-time""}]";
            var result = _service.TryParseSlots(json, out _, out _, out _);
            Assert.That(result, Is.False);
        }

        // ── NormalizeSlots ──

        [Test]
        public void NormalizeSlots_SingleSlot_ReturnsCorrectStartEnd()
        {
            var slots = new List<SlotVM>
            {
                new SlotVM { Date = new DateTime(2026, 5, 1), Time = TimeSpan.FromHours(10) }
            };

            var result = _service.NormalizeSlots(slots, 60);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].StartTime, Is.EqualTo(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero)));
            Assert.That(result[0].EndTime, Is.EqualTo(new DateTimeOffset(2026, 5, 1, 11, 0, 0, TimeSpan.Zero)));
        }

        [Test]
        public void NormalizeSlots_30MinDuration_EndTimeCorrect()
        {
            var slots = new List<SlotVM>
            {
                new SlotVM { Date = new DateTime(2026, 5, 1), Time = TimeSpan.FromHours(14) }
            };

            var result = _service.NormalizeSlots(slots, 30);

            Assert.That(result[0].EndTime, Is.EqualTo(new DateTimeOffset(2026, 5, 1, 14, 30, 0, TimeSpan.Zero)));
        }

        [Test]
        public void NormalizeSlots_MultipleSlots_AllNormalized()
        {
            var slots = new List<SlotVM>
            {
                new SlotVM { Date = new DateTime(2026, 5, 1), Time = TimeSpan.FromHours(9) },
                new SlotVM { Date = new DateTime(2026, 5, 1), Time = TimeSpan.FromHours(10) }
            };

            var result = _service.NormalizeSlots(slots, 45);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].EndTime - result[0].StartTime, Is.EqualTo(TimeSpan.FromMinutes(45)));
            Assert.That(result[1].EndTime - result[1].StartTime, Is.EqualTo(TimeSpan.FromMinutes(45)));
        }

        [Test]
        public void NormalizeSlots_EmptyList_ReturnsEmpty()
        {
            var result = _service.NormalizeSlots(new List<SlotVM>(), 60);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        // ── BuildSlots ──

        [Test]
        public void BuildSlots_NoBookings_ReturnsAllSlots()
        {
            var doctor = new Doctor
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(12, 0),
                SessionDuration = 60
            };

            // Use a far future date to avoid "today" filtering
            var date = new DateOnly(2099, 1, 1);
            var booked = Enumerable.Empty<TimeOnly>();

            var result = _service.BuildSlots(doctor, date, booked);

            Assert.That(result.Count, Is.EqualTo(3)); // 09:00, 10:00, 11:00
            Assert.That(result, Does.Contain("09:00"));
            Assert.That(result, Does.Contain("10:00"));
            Assert.That(result, Does.Contain("11:00"));
        }

        [Test]
        public void BuildSlots_WithBookedSlot_ExcludesBooked()
        {
            var doctor = new Doctor
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(12, 0),
                SessionDuration = 60
            };

            var date = new DateOnly(2099, 1, 1);
            var booked = new[] { new TimeOnly(10, 0) };

            var result = _service.BuildSlots(doctor, date, booked);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Not.Contain("10:00"));
            Assert.That(result, Does.Contain("09:00"));
            Assert.That(result, Does.Contain("11:00"));
        }

        [Test]
        public void BuildSlots_AllBooked_ReturnsEmpty()
        {
            var doctor = new Doctor
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(11, 0),
                SessionDuration = 60
            };

            var date = new DateOnly(2099, 1, 1);
            var booked = new[] { new TimeOnly(9, 0), new TimeOnly(10, 0) };

            var result = _service.BuildSlots(doctor, date, booked);

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildSlots_30MinDuration_GeneratesCorrectSlots()
        {
            var doctor = new Doctor
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(10, 0),
                SessionDuration = 30
            };

            var date = new DateOnly(2099, 1, 1);
            var result = _service.BuildSlots(doctor, date, Enumerable.Empty<TimeOnly>());

            Assert.That(result.Count, Is.EqualTo(2)); // 09:00, 09:30
            Assert.That(result, Does.Contain("09:00"));
            Assert.That(result, Does.Contain("09:30"));
        }

        [Test]
        public void BuildSlots_SessionDoesNotFitBeforeEnd_Excluded()
        {
            var doctor = new Doctor
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(9, 45),
                SessionDuration = 60
            };

            var date = new DateOnly(2099, 1, 1);
            var result = _service.BuildSlots(doctor, date, Enumerable.Empty<TimeOnly>());

            // 60 min session doesn't fit in 45 min
            Assert.That(result.Count, Is.EqualTo(0));
        }
    }
}
