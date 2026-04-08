using NUnit.Framework;
using SafeMind.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace SafeMind.Tests
{
    [TestFixture]
    public class DeterministicHasherTests
    {
        private DeterministicHasher _hasher = null!;

        [SetUp]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Hashing:Key", "TestSecretKey12345" }
                })
                .Build();
            _hasher = new DeterministicHasher(config);
        }

        [Test]
        public void Hash_SameInput_ReturnsSameHash()
        {
            var hash1 = _hasher.Hash("doctor-license-123");
            var hash2 = _hasher.Hash("doctor-license-123");

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void Hash_DifferentInputs_ReturnsDifferentHashes()
        {
            var hash1 = _hasher.Hash("input-1");
            var hash2 = _hasher.Hash("input-2");

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void Hash_EmptyInput_ReturnsEmpty()
        {
            var result = _hasher.Hash("");
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Hash_NullInput_ReturnsEmpty()
        {
            var result = _hasher.Hash(null!);
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Hash_WhitespaceInput_ReturnsEmpty()
        {
            var result = _hasher.Hash("   ");
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Hash_TrimsInput()
        {
            var hash1 = _hasher.Hash("  test  ");
            var hash2 = _hasher.Hash("test");

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void Hash_ReturnsHexString()
        {
            var result = _hasher.Hash("test-input");

            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Length, Is.EqualTo(64)); // HMAC-SHA256 = 32 bytes = 64 hex chars
            Assert.That(result, Does.Match("^[0-9A-F]+$"));
        }

        [Test]
        public void Constructor_MissingKey_ThrowsException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            Assert.Throws<System.InvalidOperationException>(() => new DeterministicHasher(config));
        }

        [Test]
        public void Constructor_EmptyKey_ThrowsException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Hashing:Key", "" }
                })
                .Build();

            Assert.Throws<System.InvalidOperationException>(() => new DeterministicHasher(config));
        }
    }
}
