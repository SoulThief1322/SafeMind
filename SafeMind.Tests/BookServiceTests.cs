using NUnit.Framework;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SafeMind.Tests
{
    [TestFixture]
    public class BookServiceTests
    {
        private SafeMindDbContext _context = null!;
        private BookService _service = null!;

        [SetUp]
        public async Task Setup()
        {
            _context = TestDbContextFactory.Create();
            _service = new BookService(_context);

            _context.Doctors.AddRange(
                new Doctor
                {
                    Name = "Dr. Alpha", UserId = "d1", Biography = "Bio A",
                    WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(17, 0),
                    SessionDuration = 60, Price = 100, Rating = 4.0m,
                    DoctorSpecialties = new System.Collections.Generic.List<DoctorSpecialty>
                    {
                        new() { Specialty = new Specialty { Name = "Anxiety" } }
                    },
                    DoctorLanguages = new System.Collections.Generic.List<DoctorLanguage>
                    {
                        new() { Language = new Language { Name = "English" } }
                    }
                },
                new Doctor
                {
                    Name = "Dr. Beta", UserId = "d2", Biography = "Bio B",
                    WorkStart = new TimeOnly(10, 0), WorkEnd = new TimeOnly(18, 0),
                    SessionDuration = 30, Price = 80, Rating = 4.5m,
                    DoctorSpecialties = new System.Collections.Generic.List<DoctorSpecialty>
                    {
                        new() { Specialty = new Specialty { Name = "Depression" } }
                    }
                },
                new Doctor
                {
                    Name = "Dr. Gamma", UserId = "d3", Biography = "Bio G",
                    WorkStart = new TimeOnly(8, 0), WorkEnd = new TimeOnly(16, 0),
                    SessionDuration = 60, Price = 120, Rating = 3.5m,
                    DoctorSpecialties = new System.Collections.Generic.List<DoctorSpecialty>
                    {
                        new() { Specialty = new Specialty { Name = "Anxiety" } }
                    }
                }
            );
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown() => _context?.Dispose();

        // ── GetDoctors ──

        [Test]
        public async Task GetDoctors_ReturnsAllDoctors()
        {
            var query = await _service.GetDoctors();
            var count = await query.CountAsync();
            Assert.That(count, Is.EqualTo(3));
        }

        // ── DoctorsWithSpecialty ──

        [Test]
        public async Task DoctorsWithSpecialty_FiltersCorrectly()
        {
            var query = await _service.DoctorsWithSpecialty("Anxiety");
            var list = await query.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task DoctorsWithSpecialty_NoMatch_ReturnsEmpty()
        {
            var query = await _service.DoctorsWithSpecialty("NonExistent");
            var list = await query.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(0));
        }

        // ── DoctorsWithName ──

        [Test]
        public async Task DoctorsWithName_PartialMatch_Finds()
        {
            var query = await _service.DoctorsWithName("Alpha");
            var list = await query.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Name, Is.EqualTo("Dr. Alpha"));
        }

        [Test]
        public async Task DoctorsWithName_NoMatch_ReturnsEmpty()
        {
            var query = await _service.DoctorsWithName("Nobody");
            var list = await query.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task DoctorsWithName_CommonPrefix_FindsMultiple()
        {
            var query = await _service.DoctorsWithName("Dr.");
            var list = await query.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(3));
        }

        // ── GetPageDoctors ──

        [Test]
        public async Task GetPageDoctors_FirstPage_ReturnsCorrectCount()
        {
            var all = await _service.GetDoctors();
            var page = await _service.GetPageDoctors(all, 1, 2);
            var list = await page.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetPageDoctors_SecondPage_ReturnsRemainder()
        {
            var all = await _service.GetDoctors();
            var page = await _service.GetPageDoctors(all, 2, 2);
            var list = await page.ToListAsync();
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetPageDoctors_OrderedByName()
        {
            var all = await _service.GetDoctors();
            var page = await _service.GetPageDoctors(all, 1, 10);
            var list = await page.ToListAsync();
            Assert.That(list[0].Name, Is.EqualTo("Dr. Alpha"));
            Assert.That(list[1].Name, Is.EqualTo("Dr. Beta"));
            Assert.That(list[2].Name, Is.EqualTo("Dr. Gamma"));
        }

        // ── GetSpecialties ──

        [Test]
        public async Task GetSpecialties_ReturnsDistinctSpecialties()
        {
            var specialties = await _service.GetSpecialties();
            var list = await specialties.Distinct().ToListAsync();
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(list, Does.Contain("Anxiety"));
            Assert.That(list, Does.Contain("Depression"));
        }
    }
}
