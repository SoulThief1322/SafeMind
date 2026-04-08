using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;

namespace SafeMind.Tests
{
    [TestFixture]
    public class CustomUserValidatorTests
    {
        private CustomUserValidator _validator = null!;
        private Mock<UserManager<IdentityUser>> _userManagerMock = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new CustomUserValidator();
            var store = new Mock<IUserStore<IdentityUser>>();
            var identityOptions = Options.Create(new IdentityOptions
            {
                User = new UserOptions { RequireUniqueEmail = false }
            });
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                store.Object,
                identityOptions,
                new Mock<IPasswordHasher<IdentityUser>>().Object,
                new IUserValidator<IdentityUser>[0],
                new IPasswordValidator<IdentityUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<IdentityUser>>>().Object
            );

            // Setup virtual methods called by base UserValidator
            _userManagerMock.Setup(m => m.GetUserNameAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync((IdentityUser u) => u.UserName);
            _userManagerMock.Setup(m => m.GetEmailAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync((IdentityUser u) => u.Email);
        }

        [Test]
        public async Task Validate_ValidUser_Succeeds()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@example.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task Validate_ShortUsername_Fails()
        {
            var user = new IdentityUser { UserName = "ab", Email = "test@example.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("4 characters")));
        }

        [Test]
        public async Task Validate_NullUsername_Fails()
        {
            var user = new IdentityUser { UserName = null, Email = "test@example.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task Validate_EmptyEmail_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("Email is required")));
        }

        [Test]
        public async Task Validate_NullEmail_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = null };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task Validate_EmailTooLong_Fails()
        {
            var longEmail = new string('a', 250) + "@b.com";
            var user = new IdentityUser { UserName = "testuser", Email = longEmail };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("too long")));
        }

        [Test]
        public async Task Validate_DomainWithoutDot_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@localhost" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("domain must contain")));
        }

        [Test]
        public async Task Validate_DisposableEmail_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@mailinator.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("Disposable")));
        }

        [Test]
        public async Task Validate_TempMailDomain_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@tempmail.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task Validate_GuerrillaMailDomain_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@guerrillamail.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task Validate_10MinuteMailDomain_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "test@10minutemail.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task Validate_LocalPartTooLong_Fails()
        {
            var longLocal = new string('a', 65) + "@example.com";
            var user = new IdentityUser { UserName = "testuser", Email = longLocal };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<IdentityError>(e => e.Description.Contains("local part is too long")));
        }

        [Test]
        public async Task Validate_ValidGmailAddress_Succeeds()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "user@gmail.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task Validate_ExactlyFourCharUsername_Succeeds()
        {
            var user = new IdentityUser { UserName = "test", Email = "user@example.com" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task Validate_InvalidEmailFormat_Fails()
        {
            var user = new IdentityUser { UserName = "testuser", Email = "not-an-email" };
            var result = await _validator.ValidateAsync(_userManagerMock.Object, user);

            Assert.That(result.Succeeded, Is.False);
        }
    }
}
