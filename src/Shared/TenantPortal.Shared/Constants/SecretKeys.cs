namespace TenantPortal.Shared.Constants
{
    public static class SecretKeys
    {
        public const string JwtSigningKey = "Jwt__SigningKey";
        public const string PostgresConnection = "ConnectionStrings__Postgres";
        public const string StripeSecretKey = "Stripe__SecretKey";
        public const string StripeWebhookSecret = "Stripe__WebhookSecret";
        public const string TotpEncryptionKey = "Totp__EncryptionKey";
    }
}
