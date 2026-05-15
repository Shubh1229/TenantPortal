namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Handles RFC 6238 TOTP secret generation, QR code rendering, and token verification.
    /// Compatible with any standard authenticator app (Google Authenticator, Microsoft Authenticator, Authy, etc.).
    /// </summary>
    public interface ITotpService
    {
        /// <summary>
        /// Generates a cryptographically random Base32-encoded TOTP secret for a new user.
        /// Store this value (encrypted at rest) in the user record.
        /// </summary>
        /// <returns>A Base32 string suitable for use with any RFC 6238-compliant TOTP app.</returns>
        string GenerateSecret();

        /// <summary>
        /// Generates an <c>otpauth://</c> URI encoded as a PNG QR code (Base64) for scanning with an authenticator app.
        /// </summary>
        /// <param name="secret">The Base32-encoded TOTP secret.</param>
        /// <param name="email">The user's email address, shown as the account name in the authenticator app.</param>
        /// <returns>A Base64-encoded PNG image of the QR code.</returns>
        string GenerateQrCode(string secret, string email);

        /// <summary>
        /// Verifies a 6-digit TOTP code with a ±2 step window to tolerate clock drift.
        /// </summary>
        /// <param name="secret">The Base32-encoded TOTP secret stored for the user.</param>
        /// <param name="token">The 6-digit code entered by the user.</param>
        /// <returns><c>true</c> if the code is valid within the allowed window; otherwise <c>false</c>.</returns>
        bool ValidateTotpToken(string secret, string token);
    }
}
