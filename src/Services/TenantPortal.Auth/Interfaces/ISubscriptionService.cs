using TenantPortal.Auth.DTOs;

namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Manages SaaS subscription lifecycle for Admin accounts.
    /// Handles self-serve registration, Stripe Checkout, billing portal access,
    /// and inbound Stripe webhook events that activate or suspend accounts.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Creates a new Admin account and a Stripe Checkout Session for the $20/month plan.
        /// The account is inactive until <see cref="HandleSubscriptionWebhookAsync"/> receives
        /// a <c>customer.subscription.created</c> event confirming payment.
        /// </summary>
        /// <param name="request">Registration payload including email, password, and frontend base URL.</param>
        /// <returns>
        /// Checkout URL to redirect the user to, plus TOTP setup data to display simultaneously.
        /// Returns <c>null</c> if the email is already registered.
        /// </returns>
        Task<AdminRegisterResponseDTO?> RegisterAdminAsync(AdminRegisterRequestDTO request);

        /// <summary>
        /// Creates a Stripe Billing Portal session, allowing the Admin to manage their
        /// subscription, update payment methods, and download invoices without leaving Stripe.
        /// </summary>
        /// <param name="adminId">The Admin's user ID.</param>
        /// <param name="returnUrl">URL the portal redirects back to after the admin is done.</param>
        /// <returns>The Stripe Billing Portal session URL, or <c>null</c> if the admin has no Stripe customer record.</returns>
        Task<string?> CreateCustomerPortalSessionAsync(Guid adminId, string returnUrl);

        /// <summary>
        /// Returns the current subscription status and tenant usage for the specified Admin.
        /// </summary>
        /// <param name="adminId">The Admin's user ID.</param>
        /// <returns><c>null</c> if no such user exists.</returns>
        Task<SubscriptionStatusResponseDTO?> GetSubscriptionStatusAsync(Guid adminId);

        /// <summary>
        /// Processes inbound Stripe subscription webhook events.
        /// Activates or suspends admin accounts in response to subscription lifecycle changes.
        /// </summary>
        /// <param name="requestBody">Raw request body (must not be read from a stream — Stripe signature validation requires the exact bytes).</param>
        /// <param name="stripeSignature">Value of the <c>Stripe-Signature</c> header.</param>
        /// <returns><c>true</c> if the event was valid and processed; <c>false</c> on signature mismatch or unexpected error.</returns>
        Task<bool> HandleSubscriptionWebhookAsync(string requestBody, string stripeSignature);
    }
}
