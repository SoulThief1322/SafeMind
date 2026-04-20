using NUnit.Framework;
using SafeMind.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace SafeMind.Tests
{
    [TestFixture]
    public class FutureDateAttributeTests
    {
        private FutureDateAttribute _attribute = null!;

        [SetUp]
        public void Setup()
        {
            _attribute = new FutureDateAttribute();
        }

        [Test]
        public void IsValid_FutureDate_ReturnsSuccess()
        {
            var futureDate = DateTime.UtcNow.AddMonths(6);
            var value = $"{futureDate.Month:D2}/{(futureDate.Year % 100):D2}";

            var result = _attribute.GetValidationResult(value, new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_PastDate_ReturnsError()
        {
            var result = _attribute.GetValidationResult("01/20", new ValidationContext(new object()));

            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_CurrentMonth_ReturnsSuccess()
        {
            var now = DateTime.UtcNow;
            var value = $"{now.Month:D2}/{(now.Year % 100):D2}";

            var result = _attribute.GetValidationResult(value, new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_NullValue_ReturnsSuccess()
        {
            var result = _attribute.GetValidationResult(null, new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_EmptyString_ReturnsSuccess()
        {
            var result = _attribute.GetValidationResult("", new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_FarFuture_ReturnsSuccess()
        {
            var result = _attribute.GetValidationResult("12/99", new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_InvalidFormat_ReturnsSuccess()
        {
            // Non-standard format - not MM/YY
            var result = _attribute.GetValidationResult("not-a-date", new ValidationContext(new object()));

            // Should return success because it doesn't parse as month/year
            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_CustomErrorMessage_UsedInResult()
        {
            _attribute = new FutureDateAttribute { ErrorMessage = "Custom expired message" };

            var result = _attribute.GetValidationResult("01/20", new ValidationContext(new object()));

            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
            Assert.That(result!.ErrorMessage, Is.EqualTo("Custom expired message"));
        }

        [Test]
        public void IsValid_December2025_ReturnsExpired()
        {
            // Dec 2025 is in the past relative to April 2026
            var result = _attribute.GetValidationResult("12/25", new ValidationContext(new object()));

            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        }

        [Test]
        public void IsValid_January2027_ReturnsSuccess()
        {
            var result = _attribute.GetValidationResult("01/27", new ValidationContext(new object()));

            Assert.That(result, Is.EqualTo(ValidationResult.Success));
        }
    }
}
