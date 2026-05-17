using System.Security.Cryptography;
using System.Text;
using TenantPortal.Auth.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <summary>
    /// AES-256-GCM implementation of <see cref="ITotpEncryptionService"/>.
    /// Each call to <see cref="Encrypt"/> produces a unique ciphertext because a fresh
    /// random 12-byte nonce is generated per encryption. The stored format is:
    /// <c>ENC:{base64(nonce || ciphertext || tag)}</c> where nonce=12 bytes, tag=16 bytes.
    /// </summary>
    public sealed class AesGcmTotpEncryptionService : ITotpEncryptionService
    {
        private const string Prefix = "ENC:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        /// <param name="keyBase64">Base64-encoded 32-byte AES-256 key, loaded from Key Vault.</param>
        public AesGcmTotpEncryptionService(string keyBase64)
        {
            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
                throw new ArgumentException("TOTP encryption key must be 32 bytes (AES-256).", nameof(keyBase64));
        }

        /// <inheritdoc/>
        public string Encrypt(string plainText)
        {
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var combined = new byte[NonceSize + cipherBytes.Length + TagSize];
            nonce.CopyTo(combined, 0);
            cipherBytes.CopyTo(combined, NonceSize);
            tag.CopyTo(combined, NonceSize + cipherBytes.Length);

            return Prefix + Convert.ToBase64String(combined);
        }

        /// <inheritdoc/>
        public string Decrypt(string value)
        {
            if (!value.StartsWith(Prefix))
                return value;

            var data = Convert.FromBase64String(value[Prefix.Length..]);
            var nonce = data[..NonceSize];
            var tag = data[^TagSize..];
            var cipherBytes = data[NonceSize..^TagSize];

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
