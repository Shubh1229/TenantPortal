namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Encrypts and decrypts TOTP secrets before they are written to or read from the database.
    /// Uses AES-256-GCM. Encrypted values are prefixed with "ENC:" so legacy plaintext values
    /// (e.g. an already-seeded SuperAdmin) are returned as-is without error.
    /// </summary>
    public interface ITotpEncryptionService
    {
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted TOTP secret. If the value does not carry the "ENC:" prefix
        /// it is treated as a legacy plaintext secret and returned unchanged.
        /// </summary>
        string Decrypt(string value);
    }
}
