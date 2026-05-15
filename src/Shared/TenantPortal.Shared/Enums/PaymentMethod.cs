namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// How a transaction was or will be paid.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>Paid through the in-app Stripe card flow.</summary>
        Stripe,

        /// <summary>Paid outside the app (Zelle, cheque, bank transfer, etc.) and reported by the tenant.</summary>
        External,

        /// <summary>Recorded manually by an Admin or Super Admin (e.g. historical backfill).</summary>
        Manual,

        /// <summary>ACH Direct Debit via Stripe (us_bank_account). Cheaper than card: 0.8% capped at $5.00.</summary>
        Ach
    }
}
