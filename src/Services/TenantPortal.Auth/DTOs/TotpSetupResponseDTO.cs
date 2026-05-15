namespace TenantPortal.Auth.DTOs
{
    /// <summary>
    /// Returned after successful registration. Contains everything the user needs to enroll
    /// their TOTP secret into an authenticator app.
    /// </summary>
    public class TotpSetupResponseDTO
    {
        /// <summary>Base32-encoded TOTP secret for manual entry into an authenticator app.</summary>
        public required string ManualEntryKey { get; set; }

        /// <summary>Base64-encoded PNG image of the QR code encoding the <c>otpauth://</c> URI.</summary>
        public required string QrCode { get; set; }
    }
}
