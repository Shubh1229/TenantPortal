namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for POST /api/auth/connect/onboard.</summary>
    public class ConnectOnboardRequestDTO
    {
        /// <summary>URL Stripe redirects the admin to after completing onboarding.</summary>
        public required string ReturnUrl { get; set; }

        /// <summary>URL Stripe redirects to if the onboarding link expires before use.</summary>
        public required string RefreshUrl { get; set; }
    }
}
