using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VgAuto.Core.Application.Configuration;

namespace VgAuto.Core.Application.Email
{
    /// <summary>
    /// Encrypts the passwords and client secrets of the email settings stored in the database
    /// (AES-GCM with a key derived from JwtOptions:Secret). A changed server secret makes them unreadable:
    /// they then have to be entered again.
    /// </summary>
    public class SecretProtector
    {
        private const string Prefix = "v1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private readonly byte[] key;

        public SecretProtector(IOptions<JwtOptions> jwt) : this(jwt.Value?.Secret) { }

        public SecretProtector(string serverSecret)
        {
            if (string.IsNullOrEmpty(serverSecret)) throw new InvalidOperationException("JwtOptions:Secret is required to store email passwords.");
            key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(serverSecret), 32,
                info: Encoding.UTF8.GetBytes("vgauto email secrets"));
        }

        public string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return null;
            var data = Encoding.UTF8.GetBytes(plain);
            var result = new byte[NonceSize + data.Length + TagSize];
            var nonce = result.AsSpan(0, NonceSize);
            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, data, result.AsSpan(NonceSize, data.Length), result.AsSpan(NonceSize + data.Length, TagSize));
            return Prefix + Convert.ToBase64String(result);
        }

        /// <returns>false when the value was encrypted with another key or is damaged</returns>
        public bool TryUnprotect(string stored, out string plain)
        {
            plain = null;
            if (string.IsNullOrEmpty(stored)) return true;
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            try
            {
                var bytes = Convert.FromBase64String(stored[Prefix.Length..]);
                if (bytes.Length < NonceSize + TagSize) return false;
                var length = bytes.Length - NonceSize - TagSize;
                var data = new byte[length];
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(bytes.AsSpan(0, NonceSize), bytes.AsSpan(NonceSize, length), bytes.AsSpan(NonceSize + length, TagSize), data);
                plain = Encoding.UTF8.GetString(data);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                return false;
            }
        }
    }
}
