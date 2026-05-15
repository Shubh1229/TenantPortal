using TenantPortal.Transactions.DTOs;

namespace TenantPortal.Transactions.Interfaces
{
    public interface IStripeService
    {
        Task<string?> CreatePaymentIntentAsync(PaymentIntentRequestDTO request, Guid userId);
        Task<bool> HandleWebhookEventAsync(string requestBody, string stripeSignature);
    }
}
