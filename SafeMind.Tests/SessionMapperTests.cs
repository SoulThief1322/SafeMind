using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;

namespace SafeMind.Tests
{
    [TestFixture]
    public class SessionMapperTests
    {
        [Test]
        public void ToViewModel_MapsDoctor()
        {
            var doctor = new Doctor
            {
                Id = 1,
                Name = "Dr. Test",
                SessionDuration = 60,
                Price = 80m,
                Rating = 4.0m,
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(17, 0),
                Biography = "Bio",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };
            var date = new DateOnly(2026, 5, 1);
            var slots = new List<string> { "09:00", "10:00" }.AsReadOnly();

            var vm = SessionMapper.ToViewModel(doctor, date, slots);

            Assert.That(vm.Doctor.Id, Is.EqualTo(1));
            Assert.That(vm.Doctor.Name, Is.EqualTo("Dr. Test"));
        }

        [Test]
        public void ToViewModel_MapsDate()
        {
            var doctor = new Doctor
            {
                Id = 1, Name = "Dr. Test",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };
            var date = new DateOnly(2026, 7, 15);

            var vm = SessionMapper.ToViewModel(doctor, date, Array.Empty<string>());

            Assert.That(vm.SelectedDate, Is.EqualTo(new DateOnly(2026, 7, 15)));
        }

        [Test]
        public void ToViewModel_MapsAvailableSlots()
        {
            var doctor = new Doctor
            {
                Id = 1, Name = "Dr. Test",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };
            var slots = new List<string> { "09:00", "10:00", "14:30" }.AsReadOnly();

            var vm = SessionMapper.ToViewModel(doctor, new DateOnly(2026, 5, 1), slots);

            Assert.That(vm.AvailableSlots.Count, Is.EqualTo(3));
        }

        [Test]
        public void ToViewModel_EmptySlots_ReturnsEmptyCollection()
        {
            var doctor = new Doctor
            {
                Id = 1, Name = "Dr. Test",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };

            var vm = SessionMapper.ToViewModel(doctor, new DateOnly(2026, 5, 1), Array.Empty<string>());

            Assert.That(vm.AvailableSlots.Count, Is.EqualTo(0));
        }
    }
}
