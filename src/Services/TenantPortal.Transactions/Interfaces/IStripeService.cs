using TenantPortal.Transactions.DTOs;

namespace TenantPortal.Transactions.Interfaces
{
    /// <summary>
    /// Wraps Stripe API interactions: payment intent creation and webhook event processing.
    /// </summary>
    public interface IStripeService
    {
        /// <summary>
        /// Creates a Stripe PaymentIntent for the specified amount and returns the client secret
        /// for the frontend to complete the card payment flow.
        /// </summary>
        /// <returns>The Stripe client secret, or <c>null</c> if the rent schedule is not found or Stripe returns an error.</returns>
        Task<string?> CreatePaymentIntentAsync(PaymentIntentRequestDTO request, Guid userId);

        /// <summary>
        /// Processes an incoming Stripe webhook event, verifying the <c>Stripe-Signature</c> header
        /// before handling. On <c>payment_intent.succeeded</c>, marks the matching transaction as <c>Confirmed</c>.
        /// </summary>
        /// <returns><c>true</c> if the event was verified and processed; <c>false</c> on signature failure or processing error.</returns>
        Task<bool> HandleWebhookEventAsync(string requestBody, string stripeSignature);
    }
}
