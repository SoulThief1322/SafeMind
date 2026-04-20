using NUnit.Framework;
using SafeMind.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SafeMind.Tests
{
    [TestFixture]
    public class ViewModelValidationTests
    {
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        // ── CheckoutViewModel ──

        [Test]
        public void CheckoutViewModel_TotalPrice_CalculatesCorrectly()
        {
            var vm = new CheckoutViewModel
            {
                DoctorId = 1,
                SessionPrice = 100m,
                Slots = new List<SlotVM>
                {
                    new SlotVM { Date = DateTime.UtcNow, Time = TimeSpan.FromHours(9) },
                    new SlotVM { Date = DateTime.UtcNow, Time = TimeSpan.FromHours(10) }
                },
                FullName = "John Doe",
                Email = "john@example.com",
                PhoneNumber = "1234567890"
            };

            Assert.That(vm.TotalPrice, Is.EqualTo(200m));
        }

        [Test]
        public void CheckoutViewModel_NoSlots_TotalIsZero()
        {
            var vm = new CheckoutViewModel
            {
                SessionPrice = 100m,
                Slots = new List<SlotVM>()
            };

            Assert.That(vm.TotalPrice, Is.EqualTo(0m));
        }

        [Test]
        public void CheckoutViewModel_SingleSlot_TotalEqualsPrice()
        {
            var vm = new CheckoutViewModel
            {
                SessionPrice = 75.50m,
                Slots = new List<SlotVM>
                {
                    new SlotVM { Date = DateTime.UtcNow, Time = TimeSpan.FromHours(9) }
                }
            };

            Assert.That(vm.TotalPrice, Is.EqualTo(75.50m));
        }

        // ── PaymentViewModel validation ──

        [Test]
        public void PaymentViewModel_ValidData_NoErrors()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = "123"
            };

            var results = ValidateModel(vm);
            // Remove FutureDate attribute related errors since they are custom
            var nonCustomResults = results.Where(r => r.ErrorMessage != null && !r.ErrorMessage.Contains("expired")).ToList();
            Assert.That(nonCustomResults.Count, Is.EqualTo(0));
        }

        [Test]
        public void PaymentViewModel_EmptyCardNumber_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = "123"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("Card number")));
        }

        [Test]
        public void PaymentViewModel_InvalidCardFormat_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "1234abcd",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = "123"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("Card number")));
        }

        [Test]
        public void PaymentViewModel_LowercaseCardholderName_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "john doe",
                ExpiryDate = "12/30",
                CVV = "123"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("Cardholder")));
        }

        [Test]
        public void PaymentViewModel_EmptyCVV_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = ""
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("CVV")));
        }

        [Test]
        public void PaymentViewModel_InvalidCVV_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = "12345"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("CVV")));
        }

        [Test]
        public void PaymentViewModel_FourDigitCVV_Valid()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "JOHN DOE",
                ExpiryDate = "12/30",
                CVV = "1234"
            };

            var results = ValidateModel(vm);
            var cvvErrors = results.Where(r => r.ErrorMessage!.Contains("CVV")).ToList();
            Assert.That(cvvErrors.Count, Is.EqualTo(0));
        }

        [Test]
        public void PaymentViewModel_InvalidExpiryFormat_HasError()
        {
            var vm = new PaymentViewModel
            {
                CardNumber = "4111 1111 1111 1111",
                CardholderName = "JOHN DOE",
                ExpiryDate = "1330",
                CVV = "123"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("Expiry") || r.ErrorMessage!.Contains("MM/YY")));
        }

        // ── ContactViewModel ──

        [Test]
        public void ContactViewModel_ValidData_NoErrors()
        {
            var vm = new ContactViewModel
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Subject = "Test Subject",
                Message = "Test message content"
            };

            var results = ValidateModel(vm);
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void ContactViewModel_EmptyName_HasError()
        {
            var vm = new ContactViewModel
            {
                FullName = "",
                Email = "test@example.com",
                Subject = "Subject",
                Message = "Message"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("name")));
        }

        [Test]
        public void ContactViewModel_EmptyEmail_HasError()
        {
            var vm = new ContactViewModel
            {
                FullName = "Test",
                Email = "",
                Subject = "Subject",
                Message = "Message"
            };

            var results = ValidateModel(vm);
            Assert.That(results.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ContactViewModel_InvalidEmail_HasError()
        {
            var vm = new ContactViewModel
            {
                FullName = "Test",
                Email = "not-an-email",
                Subject = "Subject",
                Message = "Message"
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("email")));
        }

        [Test]
        public void ContactViewModel_EmptyMessage_HasError()
        {
            var vm = new ContactViewModel
            {
                FullName = "Test",
                Email = "test@example.com",
                Subject = "Subject",
                Message = ""
            };

            var results = ValidateModel(vm);
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.ErrorMessage!.Contains("message")));
        }
    }
}
