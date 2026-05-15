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
    }
}
