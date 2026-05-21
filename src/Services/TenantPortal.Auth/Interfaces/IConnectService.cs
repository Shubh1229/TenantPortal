using TenantPortal.Auth.DTOs;

namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Manages Stripe Connect Express accounts for Admins so tenant payments
    /// are transferred directly to the admin's connected bank account.
    /// </summary>
    public interface IConnectService
    {
        /// <summary>
        /// Creates a Stripe Express account for the admin if one does not already exist,
        /// then generates a single-use AccountLink URL for hosted onboarding.
        /// </summary>
        Task<string?> GetOrCreateOnboardingLinkAsync(Guid adminId, string returnUrl, string refreshUrl);

        /// <summary>
        /// Returns the current Connect status for the admin, including whether
        /// charges and payouts are enabled and a live Express dashboard login link.
        /// </summary>
        Task<ConnectStatusDTO> GetConnectStatusAsync(Guid adminId);

        /// <summary>
        /// Processes an <c>account.updated</c> Connect webhook event.
        /// Updates <c>StripeConnectChargesEnabled</c> in the database.
        /// </summary>
        Task<bool> HandleConnectWebhookAsync(string requestBody, string stripeSignature);

        /// <summary>
        /// Returns the Stripe Connected Account ID for the given admin
        /// only if their account has charges enabled. Used by the Transactions
        /// service to add destination charges to PaymentIntents.
        /// Returns <c>null</c> if the admin has not completed Connect onboarding.
        /// </summary>
        Task<string?> GetConnectedAccountIdAsync(Guid adminId);
    }
}
