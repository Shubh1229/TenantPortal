namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for step 2 of login — TOTP code validation.</summary>
    public class TotpValidationRequestDTO
    {
        /// <summary>The opaque temporary token returned by the login step 1 endpoint.</summary>
        public required string TemporaryToken { get; set; }

        /// <summary>The 6-digit TOTP code from the user's authenticator app.</summary>
        public required string TotpCode { get; set; }
    }
}
