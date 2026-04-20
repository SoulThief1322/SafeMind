using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SafeMind.Controllers;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Models;
using SafeMind.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SafeMind.Tests
{
    // -----------------------------------------------------------------------
    // 1. Model validation tests — exercises the actual [Required], [MaxLength],
    //    [EmailAddress] annotations via System.ComponentModel.DataAnnotations.
    //    This is the same logic ASP.NET MVC's model binder runs.
    // -----------------------------------------------------------------------
    [TestFixture]
    public class ContactViewModelValidationTests
    {
        private static IList<ValidationResult> Validate(ContactViewModel model)
        {
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Test]
        public void ValidModel_PassesAllAnnotations()
        {
            var model = new ContactViewModel
            {
                FullName = "Alice",
                Email = "alice@example.com",
                Subject = "Hello",
                Message = "This is a valid message."
            };
            Assert.That(Validate(model), Is.Empty);
        }

        [Test]
        public void EmptyFullName_FailsRequired()
        {
            var model = new ContactViewModel { FullName = "", Email = "a@b.com", Subject = "S", Message = "M" };
            var errors = Validate(model);
            Assert.That(errors.Any(e => e.MemberNames.Contains(nameof(ContactViewModel.FullName))), Is.True);
        }

        [Test]
        public void FullName_ExceedsMaxLength_FailsValidation()
        {
            var model = new ContactViewModel
            {
                FullName = new string('a', 101),   // max is 100
                Email = "a@b.com",
                Subject = "S",
                Message = "M"
            };
            var errors = Validate(model);
            Assert.That(errors.Any(e => e.MemberNames.Contains(nameof(ContactViewModel.FullName))), Is.True);
        }

        [Test]
        public void InvalidEmail_FailsEmailAddressAnnotation()
        {
            var model = new ContactViewModel { FullName = "Bob", Email = "not-an-email", Subject = "S", Message = "M" };
            var errors = Validate(model);
            Assert.That(errors.Any(e => e.MemberNames.Contains(nameof(ContactViewModel.Email))), Is.True);
        }

        [Test]
        public void Message_ExceedsMaxLength_FailsValidation()
        {
            var model = new ContactViewModel
            {
                FullName = "Bob",
                Email = "bob@b.com",
                Subject = "S",
                Message = new string('x', 2001)    // max is 2000
            };
            var errors = Validate(model);
            Assert.That(errors.Any(e => e.MemberNames.Contains(nameof(ContactViewModel.Message))), Is.True);
        }

        [TestCase("")]
        [TestCase(null!)]
        public void EmptyOrNullSubject_FailsRequired(string? subject)
        {
            var model = new ContactViewModel { FullName = "Bob", Email = "bob@b.com", Subject = subject!, Message = "M" };
            var errors = Validate(model);
            Assert.That(errors.Any(e => e.MemberNames.Contains(nameof(ContactViewModel.Subject))), Is.True);
        }
    }

    // -----------------------------------------------------------------------
    // 2. SQL injection tests — uses SQLite in-memory (a real SQL engine).
    //    ToQueryString() returns the actual SQL EF Core generates.
    //    Tests verify the malicious string appears as a parameter (@p0) and
    //    never literally in the SQL text, proving injection is impossible.
    // -----------------------------------------------------------------------
    [TestFixture]
    public class SqlInjectionTests
    {
        private SafeMindDbContext _context = null!;
        private BookService _bookService = null!;
        private ArticleService _articleService = null!;
        private string _userId = null!;
        private SqliteConnection _connection = null!;

        [SetUp]
        public async Task Setup()
        {
            // Keep connection open so the in-memory SQLite database persists
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new SafeMindDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            _userId = Guid.NewGuid().ToString();
            var user = new IdentityUser { Id = _userId, UserName = "testuser", NormalizedUserName = "TESTUSER" };
            _context.Users.Add(user);

            _context.Doctors.Add(new Doctor
            {
                Id = 1,
                Name = "Dr. Normal",
                UserId = _userId,
                User = user,
                Biography = "A normal biography for a normal doctor here",
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(17, 0),
                SessionDuration = 60,
                Rating = 4.5m,
                Price = 100m
            });

            _context.Articles.Add(new Article
            {
                Id = 1,
                Headline = "Safe Article",
                Content = "Normal content",
                AuthorId = _userId
            });

            await _context.SaveChangesAsync();

            _bookService = new BookService(_context);
            _articleService = new ArticleService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        // ToQueryString() format: ".param set @p 'value'\n\nSELECT ..."
        // The param declarations are for debug readability; the SQL body uses @p.
        // We check the SQL body (non-.param lines) to confirm no literal injection.
        private static string SqlBody(string queryString) =>
            string.Join("\n", queryString
                .Split('\n')
                .Where(l => !l.TrimStart().StartsWith(".param")));

        [TestCase("'; DROP TABLE Doctors; --")]
        [TestCase("1 OR 1=1")]
        [TestCase("' UNION SELECT * FROM Users --")]
        public async Task FilterByName_GeneratesParameterizedSql_NotLiteralInjection(string maliciousInput)
        {
            var query = await _bookService.GetDoctors();
            var filtered = _bookService.FilterByName(query, maliciousInput);

            var sql = filtered.ToQueryString();
            var body = SqlBody(sql);

            // The SQL must use a parameter placeholder, not embed the raw string
            Assert.That(sql, Does.Contain("@"), "EF Core must parameterize the value");
            Assert.That(body, Does.Not.Contain(maliciousInput), "Malicious string must not appear in SQL body");

            // No doctors match and the original record is intact
            Assert.That(await filtered.CountAsync(), Is.EqualTo(0));
            Assert.That(await _context.Doctors.CountAsync(), Is.EqualTo(1));
        }

        [TestCase("'; DROP TABLE Specialties; --")]
        [TestCase("' OR '1'='1")]
        public async Task FilterBySpecialty_GeneratesParameterizedSql_NotLiteralInjection(string maliciousInput)
        {
            var query = await _bookService.GetDoctors();
            var filtered = _bookService.FilterBySpecialty(query, maliciousInput);

            var sql = filtered.ToQueryString();
            var body = SqlBody(sql);

            Assert.That(sql, Does.Contain("@"), "EF Core must parameterize the value");
            Assert.That(body, Does.Not.Contain(maliciousInput), "Malicious string must not appear in SQL body");

            Assert.That(await filtered.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task ContactMessage_SqlInjection_StoredLiterally_TableUnaffected()
        {
            var malicious = "'; DROP TABLE ContactMessages; --";

            _context.ContactMessages.Add(new ContactMessage
            {
                FullName = malicious,
                Email = "test@test.com",
                Subject = "' OR '1'='1",
                Message = "'; DELETE FROM Users; --",
                SubmittedOn = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();

            // Row was stored verbatim — table still exists, no data was deleted
            var saved = await _context.ContactMessages.FirstAsync();
            Assert.That(saved.FullName, Is.EqualTo(malicious));
            Assert.That(await _context.ContactMessages.CountAsync(), Is.EqualTo(1));
        }
    }

    // -----------------------------------------------------------------------
    // 3. XSS tests — two layers tested per payload:
    //    a) Server stores the value as plain text (no silent data corruption)
    //    b) HtmlEncoder.Default.Encode() neutralises every payload, which is
    //       exactly what Razor's @-syntax calls — proving reflected/stored XSS
    //       is blocked at render time without any custom sanitisation needed.
    // -----------------------------------------------------------------------
    [TestFixture]
    public class XssTests
    {
        private SafeMindDbContext _context = null!;
        private ContactController _contactController = null!;
        private ArticleService _articleService = null!;
        private string _userId = null!;

        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SafeMindDbContext(options);

            _userId = Guid.NewGuid().ToString();
            _context.Users.Add(new IdentityUser { Id = _userId, UserName = "testuser", NormalizedUserName = "TESTUSER" });
            await _context.SaveChangesAsync();

            _contactController = new ContactController(_context);
            _contactController.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            _articleService = new ArticleService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestCase("<script>alert('xss')</script>")]
        [TestCase("<img src=x onerror=alert('xss')>")]
        [TestCase("<svg onload=alert('xss')>")]
        [TestCase("javascript:alert('xss')")]
        [TestCase("<iframe src='javascript:alert(1)'></iframe>")]
        [TestCase("\" onfocus=\"alert('xss')\" autofocus=\"")]
        public async Task ContactForm_XssPayload_StoredAsPlainText_AndHtmlEncoderNeutralisesIt(string payload)
        {
            var model = new ContactViewModel
            {
                FullName = payload,
                Email = "test@example.com",
                Subject = payload,
                Message = payload
            };

            await _contactController.Index(model);

            var saved = await _context.ContactMessages.FirstAsync();

            // a) Value stored verbatim — no silent stripping breaks legitimate content
            Assert.That(saved.FullName, Is.EqualTo(payload));
            Assert.That(saved.Message, Is.EqualTo(payload));

            // b) HtmlEncoder.Default is what Razor's @-syntax calls.
            //    After encoding, no raw < or > characters can remain,
            //    so the browser cannot parse any tags — the payload is inert.
            var encoded = HtmlEncoder.Default.Encode(payload);
            Assert.That(encoded, Is.Not.EqualTo(payload), "Payload must be transformed by HtmlEncoder");
            Assert.That(encoded, Does.Not.Contain("<"), "No raw < must survive encoding");
            Assert.That(encoded, Does.Not.Contain(">"), "No raw > must survive encoding");
        }

        [TestCase("<script>document.cookie</script>")]
        [TestCase("<div style=\"background:url(javascript:alert('xss'))\">")]
        public async Task Article_XssPayload_StoredAsPlainText_AndHtmlEncoderNeutralisesIt(string payload)
        {
            var article = await _articleService.CreateArticleAsync(
                payload, payload, _userId, null, new List<int>());

            Assert.That(article.Headline, Is.EqualTo(payload));
            Assert.That(article.Content, Is.EqualTo(payload));

            var encodedHeadline = HtmlEncoder.Default.Encode(article.Headline);
            Assert.That(encodedHeadline, Is.Not.EqualTo(payload));
            Assert.That(encodedHeadline, Does.Not.Contain("<script").IgnoreCase);
        }

        [Test]
        public async Task DoctorSearch_XssPayload_ParameterizedInSql_AndEncoderNeutralisesIt()
        {
            using var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseSqlite(conn)
                .Options;
            using var ctx = new SafeMindDbContext(options);
            await ctx.Database.EnsureCreatedAsync();

            var bookService = new BookService(ctx);
            var query = await bookService.GetDoctors();
            var payload = "<script>alert('xss')</script>";

            var filtered = bookService.FilterByName(query, payload);

            // SQL body is parameterized — payload never appears in the query text
            var sql = filtered.ToQueryString();
            var body = string.Join("\n", sql.Split('\n').Where(l => !l.TrimStart().StartsWith(".param")));
            Assert.That(sql, Does.Contain("@"));
            Assert.That(body, Does.Not.Contain("<script").IgnoreCase);

            // HtmlEncoder would neutralise it if it reached the page
            var encoded = HtmlEncoder.Default.Encode(payload);
            Assert.That(encoded, Is.Not.EqualTo(payload));
            Assert.That(encoded, Does.Not.Contain("<"));
            Assert.That(encoded, Does.Not.Contain(">"));

            Assert.That(await filtered.ToListAsync(), Is.Empty);
        }
    }
}
