using NUnit.Framework;
using SafeMind.Models;
using System;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DoctorViewModelTests
    {
        [Test]
        public void Initials_TwoPartName_ReturnsBothInitials()
        {
            var vm = new DoctorViewModel { Name = "Jane Smith" };
            Assert.That(vm.Initials, Is.EqualTo("JS"));
        }

        [Test]
        public void Initials_ThreePartName_ReturnsThreeInitials()
        {
            var vm = new DoctorViewModel { Name = "Dr Jane Smith" };
            Assert.That(vm.Initials, Is.EqualTo("DJS"));
        }

        [Test]
        public void Initials_SingleName_ReturnsSingleInitial()
        {
            var vm = new DoctorViewModel { Name = "Jane" };
            Assert.That(vm.Initials, Is.EqualTo("J"));
        }

        [Test]
        public void Initials_LowercaseName_ReturnsUppercaseInitials()
        {
            var vm = new DoctorViewModel { Name = "jane smith" };
            Assert.That(vm.Initials, Is.EqualTo("JS"));
        }

        [Test]
        public void Initials_MultipleSpaces_HandledCorrectly()
        {
            var vm = new DoctorViewModel { Name = "Jane   Smith" };
            Assert.That(vm.Initials, Is.EqualTo("JS"));
        }

        [Test]
        public void AvailabilityRange_FormattedCorrectly()
        {
            var vm = new DoctorViewModel
            {
                WorkStart = new TimeOnly(9, 0),
                WorkEnd = new TimeOnly(17, 0)
            };
            Assert.That(vm.AvailabilityRange, Is.EqualTo("09:00 - 17:00"));
        }

        [Test]
        public void AvailabilityRange_WithMinutes_FormattedCorrectly()
        {
            var vm = new DoctorViewModel
            {
                WorkStart = new TimeOnly(8, 30),
                WorkEnd = new TimeOnly(16, 45)
            };
            Assert.That(vm.AvailabilityRange, Is.EqualTo("08:30 - 16:45"));
        }
    }
}
