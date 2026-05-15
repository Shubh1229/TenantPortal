namespace TenantPortal.Auth.DTOs
{
    /// <summary>Returned after successful TOTP verification. Contains both tokens needed for authenticated sessions.</summary>
    public class LoginResponseDTO
    {
        /// <summary>Short-lived (15-minute) signed JWT. Include as a Bearer token on all authenticated requests.</summary>
        public required string AccessToken { get; set; }

        /// <summary>Long-lived (7-day) opaque token. Store securely (httpOnly cookie recommended) and use to obtain new access tokens.</summary>
        public required string RefreshToken { get; set; }
    }
}
