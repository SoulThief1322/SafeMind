using NUnit.Framework;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DoctorMapperTests
    {
        [Test]
        public void ToViewModel_MapsAllBasicFields()
        {
            var doctor = new Doctor
            {
                Id = 1,
                Name = "Dr. Jane Smith",
                SessionDuration = 60,
                Price = 100.00m,
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(17, 0),
                Rating = 4.5m,
                Biography = "Expert therapist",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };

            var vm = DoctorMapper.ToViewModel(doctor);

            Assert.That(vm.Id, Is.EqualTo(1));
            Assert.That(vm.Name, Is.EqualTo("Dr. Jane Smith"));
            Assert.That(vm.SessionDuration, Is.EqualTo(60));
            Assert.That(vm.Price, Is.EqualTo(100.00m));
            Assert.That(vm.WorkStart, Is.EqualTo(new TimeOnly(9, 0)));
            Assert.That(vm.WorkEnd, Is.EqualTo(new TimeOnly(17, 0)));
            Assert.That(vm.Rating, Is.EqualTo(4.5m));
            Assert.That(vm.Biography, Is.EqualTo("Expert therapist"));
        }

        [Test]
        public void ToViewModel_MapsSpecialties()
        {
            var doctor = new Doctor
            {
                Id = 2,
                Name = "Dr. Test",
                DoctorSpecialties = new List<DoctorSpecialty>
                {
                    new DoctorSpecialty { Specialty = new Specialty { Name = "CBT" } },
                    new DoctorSpecialty { Specialty = new Specialty { Name = "Trauma" } }
                },
                DoctorLanguages = new List<DoctorLanguage>()
            };

            var vm = DoctorMapper.ToViewModel(doctor);
            var specialties = vm.Specialties.ToList();

            Assert.That(specialties.Count, Is.EqualTo(2));
            Assert.That(specialties, Does.Contain("CBT"));
            Assert.That(specialties, Does.Contain("Trauma"));
        }

        [Test]
        public void ToViewModel_MapsLanguages()
        {
            var doctor = new Doctor
            {
                Id = 3,
                Name = "Dr. Lang",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>
                {
                    new DoctorLanguage { Language = new Language { Name = "English" } },
                    new DoctorLanguage { Language = new Language { Name = "Bulgarian" } }
                }
            };

            var vm = DoctorMapper.ToViewModel(doctor);
            var languages = vm.Languages.ToList();

            Assert.That(languages.Count, Is.EqualTo(2));
            Assert.That(languages, Does.Contain("English"));
            Assert.That(languages, Does.Contain("Bulgarian"));
        }

        [Test]
        public void ToViewModel_NullSpecialtyFiltered()
        {
            var doctor = new Doctor
            {
                Id = 4,
                Name = "Dr. Null",
                DoctorSpecialties = new List<DoctorSpecialty>
                {
                    new DoctorSpecialty { Specialty = null! },
                    new DoctorSpecialty { Specialty = new Specialty { Name = "Valid" } }
                },
                DoctorLanguages = new List<DoctorLanguage>()
            };

            var vm = DoctorMapper.ToViewModel(doctor);
            Assert.That(vm.Specialties.Count(), Is.EqualTo(1));
            Assert.That(vm.Specialties.First(), Is.EqualTo("Valid"));
        }

        [Test]
        public void ToViewModel_NullLanguageFiltered()
        {
            var doctor = new Doctor
            {
                Id = 5,
                Name = "Dr. NullLang",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>
                {
                    new DoctorLanguage { Language = null! },
                    new DoctorLanguage { Language = new Language { Name = "English" } }
                }
            };

            var vm = DoctorMapper.ToViewModel(doctor);
            Assert.That(vm.Languages.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ToViewModel_EmptySpecialtiesAndLanguages()
        {
            var doctor = new Doctor
            {
                Id = 6,
                Name = "Dr. Empty",
                DoctorSpecialties = new List<DoctorSpecialty>(),
                DoctorLanguages = new List<DoctorLanguage>()
            };

            var vm = DoctorMapper.ToViewModel(doctor);
            Assert.That(vm.Specialties.Count(), Is.EqualTo(0));
            Assert.That(vm.Languages.Count(), Is.EqualTo(0));
        }
    }
}
