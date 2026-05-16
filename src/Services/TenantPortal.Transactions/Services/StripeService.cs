using Microsoft.EntityFrameworkCore;
using TenantPortal.Shared.Interfaces;
using TenantPortal.Transactions.Data;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Interfaces;
using TenantPortal.Transactions.Models;
using Stripe;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.Services
{
    public class StripeService : IStripeService
    {
        private readonly TransactionDbContext _context;
        private readonly ISecretsProvider _secretsProvider;
        public StripeService(TransactionDbContext context, ISecretsProvider secretsProvider)
        {
            _context = context;
            _secretsProvider = secretsProvider;
        }
        public async Task<string?> CreatePaymentIntentAsync(PaymentIntentRequestDTO request, Guid userId)
        {
            try
            {
                var schedule = await _context.RentSchedules.FirstOrDefaultAsync(s => s.Id == request.RentScheduleId);
                if (schedule == null)
                    return null;

                var stripeKey = await _secretsProvider.GetSecretAsync(SecretKeys.StripeSecretKey);
                StripeConfiguration.ApiKey = stripeKey;

                var isAch = request.PaymentMethodType?.Equals("ach", StringComparison.OrdinalIgnoreCase) == true;

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
                    Currency = request.Currency,
                    PaymentMethodTypes = isAch
                        ? new List<string> { "us_bank_account" }
                        : new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId.ToString() },
                        { "RentScheduleId", request.RentScheduleId.ToString() },
                        { "PaymentMethodType", isAch ? "ach" : "card" }
                    }
                };

                // ACH (us_bank_account) requires Financial Connections for instant bank verification
                if (isAch)
                {
                    options.PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                    {
                        UsBankAccount = new PaymentIntentPaymentMethodOptionsUsBankAccountOptions
                        {
                            FinancialConnections = new PaymentIntentPaymentMethodOptionsUsBankAccountFinancialConnectionsOptions
                            {
                                Permissions = new List<string> { "payment_method" }
                            }
                        }
                    };
                }

                var intent = await new PaymentIntentService().CreateAsync(options);
                return intent.ClientSecret;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> HandleWebhookEventAsync(string requestBody, string stripeSignature)
        {
            try
            {
                var webhookSecret = await _secretsProvider.GetSecretAsync(SecretKeys.StripeWebhookSecret);
                var stripeEvent = EventUtility.ConstructEvent(requestBody, stripeSignature, webhookSecret);
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent == null) return false;
                    var transaction = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id);
                    if (transaction == null) return false;
                    transaction.Status = TransactionStatus.Confirmed;
                    transaction.PaidDate = DateTime.UtcNow;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return true;
            } catch (Exception)
            {
                return false;
            }
        }
    }
}
