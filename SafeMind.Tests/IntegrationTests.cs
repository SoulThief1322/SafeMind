using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ArticleServiceIntegrationTests
    {
        private SafeMindDbContext _context = null!;
        private ArticleService _service = null!;
        private string _testUserId = null!;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SafeMindDbContext(options);

            // Seed a user
            _testUserId = Guid.NewGuid().ToString();
            _context.Users.Add(new IdentityUser
            {
                Id = _testUserId,
                UserName = "testauthor",
                Email = "author@test.com",
                NormalizedUserName = "TESTAUTHOR",
                NormalizedEmail = "AUTHOR@TEST.COM"
            });

            // Seed categories
            _context.Categories.AddRange(
                new Category { Id = 1, Name = "Anxiety" },
                new Category { Id = 2, Name = "Depression" }
            );

            await _context.SaveChangesAsync();

            _service = new ArticleService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task CreateArticleAsync_CreatesArticleWithCategories()
        {
            var article = await _service.CreateArticleAsync(
                "Test Article",
                "Content here",
                _testUserId,
                null,
                new List<int> { 1, 2 });

            Assert.That(article, Is.Not.Null);
            Assert.That(article.Id, Is.GreaterThan(0));
            Assert.That(article.Headline, Is.EqualTo("Test Article"));

            var categories = await _context.ArticleCategories
                .Where(ac => ac.ArticleId == article.Id)
                .ToListAsync();
            Assert.That(categories, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetAllArticlesAsync_ExcludesDeletedArticles()
        {
            _context.Articles.AddRange(
                new Article { Headline = "Active", Content = "c", AuthorId = _testUserId, IsDeleted = false },
                new Article { Headline = "Deleted", Content = "c", AuthorId = _testUserId, IsDeleted = true }
            );
            await _context.SaveChangesAsync();

            var articles = await _service.GetAllArticlesAsync();

            Assert.That(articles, Has.Count.EqualTo(1));
            Assert.That(articles[0].Headline, Is.EqualTo("Active"));
        }

        [Test]
        public async Task ToggleLikeAsync_FirstLike_AddsLike()
        {
            _context.Articles.Add(new Article
            {
                Id = 10,
                Headline = "Likeable",
                Content = "c",
                AuthorId = _testUserId,
                Likes = 0
            });
            await _context.SaveChangesAsync();

            var (hasLiked, likes) = await _service.ToggleLikeAsync(10, _testUserId);

            Assert.That(hasLiked, Is.True);
            Assert.That(likes, Is.EqualTo(1));
        }

        [Test]
        public async Task ToggleLikeAsync_SecondLike_RemovesLike()
        {
            _context.Articles.Add(new Article
            {
                Id = 11,
                Headline = "Unlikeable",
                Content = "c",
                AuthorId = _testUserId,
                Likes = 1
            });
            _context.ArticleLikes.Add(new ArticleLike { ArticleId = 11, UserId = _testUserId });
            await _context.SaveChangesAsync();

            var (hasLiked, likes) = await _service.ToggleLikeAsync(11, _testUserId);

            Assert.That(hasLiked, Is.False);
            Assert.That(likes, Is.EqualTo(0));
        }

        [Test]
        public async Task GetArticlesPagedAsync_PaginatesCorrectly()
        {
            for (int i = 0; i < 15; i++)
            {
                _context.Articles.Add(new Article
                {
                    Headline = $"Article {i}",
                    Content = "c",
                    AuthorId = _testUserId,
                    PublishedOn = DateTimeOffset.UtcNow.AddMinutes(-i)
                });
            }
            await _context.SaveChangesAsync();

            var (page1, total) = await _service.GetArticlesPagedAsync(1, 10);
            var (page2, _) = await _service.GetArticlesPagedAsync(2, 10);

            Assert.That(total, Is.EqualTo(15));
            Assert.That(page1, Has.Count.EqualTo(10));
            Assert.That(page2, Has.Count.EqualTo(5));
        }

        [Test]
        public async Task GetSelectedArticleAsync_ReturnsNull_WhenDeleted()
        {
            _context.Articles.Add(new Article
            {
                Id = 20,
                Headline = "Deleted",
                Content = "c",
                AuthorId = _testUserId,
                IsDeleted = true
            });
            await _context.SaveChangesAsync();

            var result = await _service.GetSelectedArticleAsync(20);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetSelectedArticleAsync_ReturnsArticle_WhenExists()
        {
            _context.Articles.Add(new Article
            {
                Id = 21,
                Headline = "Visible",
                Content = "Content here",
                AuthorId = _testUserId,
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            var result = await _service.GetSelectedArticleAsync(21);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Headline, Is.EqualTo("Visible"));
        }
    }

    [TestFixture]
    public class BookServiceIntegrationTests
    {
        private SafeMindDbContext _context = null!;
        private BookService _service = null!;
        private string _userId = null!;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SafeMindDbContext(options);

            _userId = Guid.NewGuid().ToString();
            var userId2 = Guid.NewGuid().ToString();

            var user1 = new IdentityUser { Id = _userId, UserName = "doctor1", NormalizedUserName = "DOCTOR1" };
            var user2 = new IdentityUser { Id = userId2, UserName = "doctor2", NormalizedUserName = "DOCTOR2" };
            _context.Users.AddRange(user1, user2);

            var spec1 = new Specialty { Id = 1, Name = "CBT" };
            var spec2 = new Specialty { Id = 2, Name = "Trauma" };
            _context.Specialties.AddRange(spec1, spec2);
            await _context.SaveChangesAsync();

            var doctor1 = new Doctor
            {
                Id = 1,
                Name = "Dr. Smith",
                UserId = _userId,
                User = user1,
                Biography = "A qualified therapist with years of experience",
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60,
                Rating = 4.5m,
                Price = 100m,
            };
            var doctor2 = new Doctor
            {
                Id = 2,
                Name = "Dr. Jones",
                UserId = userId2,
                User = user2,
                Biography = "Expert in trauma therapy and mental health wellness",
                WorkStart = new TimeOnly(10, 0),
                WorkEnd = new TimeOnly(18, 0),
                SessionDuration = 45,
                Rating = 4.8m,
                Price = 120m,
            };
            _context.Doctors.AddRange(doctor1, doctor2);

            doctor1.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = 1, SpecialtyId = 1, Doctor = doctor1, Specialty = spec1 });
            doctor2.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = 2, SpecialtyId = 2, Doctor = doctor2, Specialty = spec2 });

            await _context.SaveChangesAsync();
            _service = new BookService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetDoctors_ReturnsAll()
        {
            var query = await _service.GetDoctors();
            var count = await query.CountAsync();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task FilterBySpecialty_FiltersCorrectly()
        {
            var query = await _service.GetDoctors();
            var filtered = _service.FilterBySpecialty(query, "CBT");
            var results = await filtered.ToListAsync();

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("Dr. Smith"));
        }

        [Test]
        public async Task FilterByName_FiltersCorrectly()
        {
            var query = await _service.GetDoctors();
            var filtered = _service.FilterByName(query, "Jones");
            var results = await filtered.ToListAsync();

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("Dr. Jones"));
        }

        [Test]
        public async Task FilterBySpecialty_AndName_ChainsCorrectly()
        {
            var query = await _service.GetDoctors();
            var filtered = _service.FilterBySpecialty(query, "CBT");
            filtered = _service.FilterByName(filtered, "Smith");
            var results = await filtered.ToListAsync();

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("Dr. Smith"));
        }

        [Test]
        public async Task FilterBySpecialty_AndName_NoMatch_ReturnsEmpty()
        {
            var query = await _service.GetDoctors();
            var filtered = _service.FilterBySpecialty(query, "CBT");
            filtered = _service.FilterByName(filtered, "Jones");
            var results = await filtered.ToListAsync();

            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task GetPageDoctors_PaginatesCorrectly()
        {
            var query = await _service.GetDoctors();
            var page1 = await _service.GetPageDoctors(query, 1, 1);
            var page2 = await _service.GetPageDoctors(query, 2, 1);

            Assert.That(await page1.CountAsync(), Is.EqualTo(1));
            Assert.That(await page2.CountAsync(), Is.EqualTo(1));
        }
    }
}
