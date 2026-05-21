namespace TenantPortal.Auth.DTOs
{
    /// <summary>
    /// Response for GET /api/auth/connect/status — describes the admin's Stripe Connect state.
    /// </summary>
    public class ConnectStatusDTO
    {
        /// <summary>True if the admin has started Connect onboarding (account created in Stripe).</summary>
        public bool IsConnected { get; set; }

        /// <summary>True once Stripe has verified the account and enabled payment acceptance.</summary>
        public bool ChargesEnabled { get; set; }

        /// <summary>True once Stripe has enabled payouts to the admin's bank account.</summary>
        public bool PayoutsEnabled { get; set; }

        /// <summary>One-time Express dashboard login URL, valid for a few minutes. Null if not yet enabled.</summary>
        public string? DashboardUrl { get; set; }
    }
}
