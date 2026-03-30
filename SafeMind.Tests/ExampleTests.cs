using NUnit.Framework;
using System;
using SafeMind; // keep to ensure project reference compiles — adjust namespaces if needed

namespace SafeMind.Tests
{
    [TestFixture]
    public class ExampleTests
    {
        [Test]
        public void SanityTest()
        {
            Assert.Pass("Test project wired up");
        }
    }
}
