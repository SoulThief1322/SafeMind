using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SafeMind.Services
{
    public interface IDeterministicHasher
    {
        string Hash(string input);
    }

    public sealed class DeterministicHasher : IDeterministicHasher
    {
        private readonly byte[] _key;

        public DeterministicHasher(IConfiguration configuration)
        {
            var key = configuration["Hashing:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Hashing:Key is not configured. Add a strong secret key in configuration or user secrets.");
            }

            _key = Encoding.UTF8.GetBytes(key);
        }

        public string Hash(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            using var hmac = new HMACSHA256(_key);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input.Trim()));
            return Convert.ToHexString(bytes);
        }
    }
}
