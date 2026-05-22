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

        /// <summary>
        /// Plan selected by the user: <c>"Starter"</c> (5 tenants), <c>"Standard"</c> (10 tenants),
        /// or <c>"Unlimited"</c>. Defaults to <c>"Standard"</c>.
        /// </summary>
        public string Plan { get; set; } = "Standard";

        /// <summary>
        /// Billing period selected by the user. Either <c>"Monthly"</c> or <c>"Yearly"</c>.
        /// Defaults to <c>"Monthly"</c> if not specified.
        /// </summary>
        public string BillingPeriod { get; set; } = "Monthly";
    }
}
