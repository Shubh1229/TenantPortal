using Microsoft.EntityFrameworkCore;
using Stripe;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="ISubscriptionService"/>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AuthDbContext _context;
        private readonly ITotpService _totpService;
        private readonly ISecretsProvider _secretsProvider;

        /// <summary>
        /// Number of active tenants included in the base $20/month plan.
        /// Increase when higher-tier pricing is added.
        /// </summary>
        private const int BasePlanMaxTenants = 10;

        public SubscriptionService(
            AuthDbContext context,
            ITotpService totpService,
            ISecretsProvider secretsProvider)
        {
            _context = context;
            _totpService = totpService;
            _secretsProvider = secretsProvider;
        }

        /// <inheritdoc/>
        public async Task<AdminRegisterResponseDTO?> RegisterAdminAsync(AdminRegisterRequestDTO request)
        {
            request.Email = request.Email.Trim().ToLowerInvariant();

            // Reject duplicate emails before touching Stripe
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && !u.IsDeleted))
                return null;

            await ConfigureStripeAsync();

            // Create the Stripe customer first so the checkout session can be tied to it.
            // Storing UserId in metadata lets us correlate back to our DB without relying on email.
            var userId = Guid.NewGuid();
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Email = request.Email,
                Metadata = new Dictionary<string, string> { { "UserId", userId.ToString() } }
            });

            // Generate the TOTP secret now so the admin can set up their authenticator
            // while checkout is in progress. The account stays inactive until the subscription
            // webhook fires and activates it.
            var totpSecret = _totpService.GenerateSecret();

            var user = new User
            {
                Id = userId,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                TotpSecret = totpSecret,
                Role = UserRole.Admin,
                IsActive = false,           // activated by customer.subscription.created webhook
                IsDeleted = false,
                StripeCustomerId = customer.Id,
                SubscriptionStatus = SubscriptionStatus.None,
                MaxTenants = BasePlanMaxTenants,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Build a Checkout Session for the subscription plan.
            // success_url includes {CHECKOUT_SESSION_ID} so the frontend can display a
            // "payment confirmed" message while it waits for the webhook to activate the account.
            var priceId = await _secretsProvider.GetSecretAsync(SecretKeys.StripePriceId);
            var session = await new Stripe.Checkout.SessionService().CreateAsync(new Stripe.Checkout.SessionCreateOptions
            {
                Customer = customer.Id,
                Mode = "subscription",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                SuccessUrl = $"{request.ReturnBaseUrl}/subscription/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{request.ReturnBaseUrl}/register/admin?canceled=true"
            });

            return new AdminRegisterResponseDTO
            {
                CheckoutUrl = session.Url,
                TotpSetup = new TotpSetupResponseDTO
                {
                    ManualEntryKey = totpSecret,
                    QrCode = _totpService.GenerateQrCode(totpSecret, request.Email)
                }
            };
        }

        /// <inheritdoc/>
        public async Task<string?> CreateCustomerPortalSessionAsync(Guid adminId, string returnUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && !u.IsDeleted);
            if (user?.StripeCustomerId == null)
                return null;

            await ConfigureStripeAsync();

            var session = await new Stripe.BillingPortal.SessionService().CreateAsync(
                new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = user.StripeCustomerId,
                    ReturnUrl = returnUrl
                });

            return session.Url;
        }

        /// <inheritdoc/>
        public async Task<SubscriptionStatusResponseDTO?> GetSubscriptionStatusAsync(Guid adminId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && !u.IsDeleted);
            if (user == null)
                return null;

            // Count tenants invited by this admin who are still active
            var tenantCount = await _context.Users.CountAsync(u =>
                u.InvitedBy == adminId &&
                u.Role == UserRole.Tenant &&
                u.IsActive &&
                !u.IsDeleted);

            return new SubscriptionStatusResponseDTO
            {
                Status = user.SubscriptionStatus,
                IsActive = user.IsActive,
                MaxTenants = user.MaxTenants,
                CurrentTenantCount = tenantCount
            };
        }

        /// <inheritdoc/>
        public async Task<bool> HandleSubscriptionWebhookAsync(string requestBody, string stripeSignature)
        {
            try
            {
                var webhookSecret = await _secretsProvider.GetSecretAsync(SecretKeys.StripeSubscriptionWebhookSecret);
                var stripeEvent = EventUtility.ConstructEvent(requestBody, stripeSignature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    // Subscription created (checkout completed) or updated (plan change, trial end, etc.)
                    case "customer.subscription.created":
                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                        break;

                    // Subscription ended — admin canceled or all retries exhausted
                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                        break;

                    // Payment failed — suspend immediately; Stripe will retry automatically
                    case "invoice.payment_failed":
                        await HandlePaymentFailedAsync(stripeEvent.Data.Object as Stripe.Invoice);
                        break;

                    // Payment succeeded — re-activate if previously suspended due to failed payment
                    case "invoice.payment_succeeded":
                        await HandlePaymentSucceededAsync(stripeEvent.Data.Object as Stripe.Invoice);
                        break;
                }

                return true;
            }
            catch (StripeException)
            {
                // Signature mismatch or malformed event — return false so the controller returns 400
                return false;
            }
        }

        // ── Private Webhook Handlers ─────────────────────────────────────────────

        /// <summary>
        /// Activates the account on subscription creation; updates status on any subsequent change.
        /// </summary>
        private async Task HandleSubscriptionUpdatedAsync(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.StripeCustomerId == subscription.CustomerId && !u.IsDeleted);
            if (user == null) return;

            user.StripeSubscriptionId = subscription.Id;
            user.SubscriptionStatus = MapStripeStatus(subscription.Status);

            // Only active and trialing subscriptions grant access
            user.IsActive = subscription.Status is "active" or "trialing";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Permanently suspends the account when the subscription is deleted.
        /// </summary>
        private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.StripeCustomerId == subscription.CustomerId && !u.IsDeleted);
            if (user == null) return;

            user.SubscriptionStatus = SubscriptionStatus.Canceled;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Suspends the account immediately when a subscription invoice payment fails.
        /// Stripe will retry; the account is re-activated by <see cref="HandlePaymentSucceededAsync"/>.
        /// </summary>
        private async Task HandlePaymentFailedAsync(Stripe.Invoice? invoice)
        {
            if (invoice == null) return;
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.StripeCustomerId == invoice.CustomerId && !u.IsDeleted);
            if (user == null) return;

            user.SubscriptionStatus = SubscriptionStatus.PastDue;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Re-activates a PastDue account when Stripe successfully collects a retry payment.
        /// Because this handler is only registered on the subscription webhook endpoint,
        /// all invoice.payment_succeeded events arriving here are subscription-related.
        /// </summary>
        private async Task HandlePaymentSucceededAsync(Stripe.Invoice? invoice)
        {
            if (invoice == null) return;

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.StripeCustomerId == invoice.CustomerId && !u.IsDeleted);
            if (user == null || user.SubscriptionStatus != SubscriptionStatus.PastDue) return;

            user.SubscriptionStatus = SubscriptionStatus.Active;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Loads the Stripe secret key from secrets and sets it globally for this request.</summary>
        private async Task ConfigureStripeAsync()
        {
            var stripeKey = await _secretsProvider.GetSecretAsync(SecretKeys.StripeSecretKey);
            StripeConfiguration.ApiKey = stripeKey;
        }

        /// <summary>Translates a Stripe subscription status string to the internal <see cref="SubscriptionStatus"/> enum.</summary>
        private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" or "unpaid" => SubscriptionStatus.PastDue,
            "canceled" or "incomplete_expired" => SubscriptionStatus.Canceled,
            _ => SubscriptionStatus.None
        };
    }
}
