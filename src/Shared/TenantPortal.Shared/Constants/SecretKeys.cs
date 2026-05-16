namespace TenantPortal.Shared.Constants
{
    /// <summary>
    /// Centralised names for all secrets retrieved via <see cref="TenantPortal.Shared.Interfaces.ISecretsProvider"/>.
    /// Values must match the keys present in <c>secrets.json</c> for local development
    /// and in Azure Key Vault for staging and production.
    /// </summary>
    public static class SecretKeys
    {
        /// <summary>HS256 signing secret used to issue and validate JWTs across all services.</summary>
        public const string JwtSigningKey = "Jwt__SigningKey";

        /// <summary>Stripe secret API key used to create payment intents.</summary>
        public const string StripeSecretKey = "Stripe__SecretKey";

        /// <summary>Stripe webhook signing secret used to verify the <c>Stripe-Signature</c> header.</summary>
        public const string StripeWebhookSecret = "Stripe__WebhookSecret";

        /// <summary>Azure Communication Services connection string used to send transactional emails.</summary>
        public const string AzureCommunicationServices = "AzureCommunicationServices__ConnectionString";

        /// <summary>Stripe Price ID for the $20/month SaaS plan used to create Checkout Sessions.</summary>
        public const string StripePriceId = "Stripe__PriceId";

        /// <summary>
        /// Stripe webhook signing secret for the subscription webhook endpoint.
        /// This is a separate secret from <see cref="StripeWebhookSecret"/> because
        /// subscription events are routed to a different endpoint in Stripe's dashboard.
        /// </summary>
        public const string StripeSubscriptionWebhookSecret = "Stripe__SubscriptionWebhookSecret";

        public const string SuperAdminEmail = "SuperAdmin__Email";
        public const string SuperAdminPassword = "SuperAdmin__Password";
    }
}
