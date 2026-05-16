namespace TenantPortal.Auth.DTOs
{
    /// <summary>
    /// Payload for the self-serve Admin registration endpoint.
    /// No invite token is required — the account is activated once a Stripe subscription is confirmed.
    /// </summary>
    public class AdminRegisterRequestDTO
    {
        /// <summary>Email address that will serve as the Admin's login identifier.</summary>
        public required string Email { get; set; }

        /// <summary>Plain-text password. Stored as a bcrypt hash; never logged or returned.</summary>
        public required string Password { get; set; }

        /// <summary>
        /// Base URL of the frontend (e.g. <c>https://app.tenantportal.com</c>).
        /// Used to construct the Stripe Checkout success and cancel redirect URLs.
        /// </summary>
        public required string ReturnBaseUrl { get; set; }
    }
}
