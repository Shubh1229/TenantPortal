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

        /// <summary>Stripe Price ID for the Starter plan — $20/month (5 tenants).</summary>
        public const string StripePriceIdStarterMonthly = "Stripe__PriceIdStarterMonthly";

        /// <summary>Stripe Price ID for the Starter plan — $200/year (5 tenants).</summary>
        public const string StripePriceIdStarterYearly = "Stripe__PriceIdStarterYearly";

        /// <summary>Stripe Price ID for the Standard plan — $30/month (10 tenants).</summary>
        public const string StripePriceIdStandardMonthly = "Stripe__PriceIdStandardMonthly";

        /// <summary>Stripe Price ID for the Standard plan — $300/year (10 tenants).</summary>
        public const string StripePriceIdStandardYearly = "Stripe__PriceIdStandardYearly";

        /// <summary>Stripe Price ID for the Unlimited plan — $50/month (unlimited tenants).</summary>
        public const string StripePriceIdUnlimitedMonthly = "Stripe__PriceIdUnlimitedMonthly";

        /// <summary>Stripe Price ID for the Unlimited plan — $500/year (unlimited tenants).</summary>
        public const string StripePriceIdUnlimitedYearly = "Stripe__PriceIdUnlimitedYearly";

        /// <summary>
        /// Stripe webhook signing secret for the subscription webhook endpoint.
        /// This is a separate secret from <see cref="StripeWebhookSecret"/> because
        /// subscription events are routed to a different endpoint in Stripe's dashboard.
        /// </summary>
        public const string StripeSubscriptionWebhookSecret = "Stripe__SubscriptionWebhookSecret";

        /// <summary>Azure Blob Storage connection string used by the Contracts service to store lease PDFs.</summary>
        public const string AzureBlobConnectionString = "AzureBlobStorage__ConnectionString";

        /// <summary>Sender address used for outbound emails via Azure Communication Services (e.g. DoNotReply@xxx.azurecomm.net).</summary>
        public const string AzureEmailSenderAddress = "AzureEmail__SenderAddress";

        public const string SuperAdminEmail = "SuperAdmin__Email";
        public const string SuperAdminPassword = "SuperAdmin__Password";

        /// <summary>Fixed TOTP secret for the SuperAdmin account. Stored in Key Vault so it survives DB wipes.</summary>
        public const string SuperAdminTotpSecret = "SuperAdmin__TotpSecret";

        /// <summary>Base64-encoded 32-byte AES-256 key used to encrypt TOTP secrets at rest.</summary>
        public const string TotpEncryptionKey = "Totp__EncryptionKey";

        /// <summary>
        /// Stripe webhook signing secret for the Connect account webhook endpoint.
        /// Separate from <see cref="StripeSubscriptionWebhookSecret"/> — registered in the
        /// Stripe dashboard to listen for <c>account.updated</c> Connect events.
        /// </summary>
        public const string StripeConnectWebhookSecret = "Stripe__ConnectWebhookSecret";
    }
}
