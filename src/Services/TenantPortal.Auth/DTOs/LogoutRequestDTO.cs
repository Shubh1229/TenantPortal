namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for logging out — provides the refresh token to revoke.</summary>
    public class LogoutRequestDTO
    {
        /// <summary>The refresh token to invalidate. Stored tokens are cleared from the database.</summary>
        public required string RefreshToken { get; set; }
    }
}
