namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for exchanging a refresh token for a new token pair.</summary>
    public class RefreshTokenRequestDTO
    {
        /// <summary>The opaque refresh token previously issued by the login or refresh endpoint.</summary>
        public required string RefreshToken { get; set; }
    }
}
