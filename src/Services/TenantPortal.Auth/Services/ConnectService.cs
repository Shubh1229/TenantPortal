using Microsoft.EntityFrameworkCore;
using Stripe;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="IConnectService"/>
    public class ConnectService : IConnectService
    {
        private readonly AuthDbContext _context;
        private readonly ISecretsProvider _secretsProvider;
        private readonly ILogger<ConnectService> _logger;

        public ConnectService(
            AuthDbContext context,
            ISecretsProvider secretsProvider,
            ILogger<ConnectService> logger)
        {
            _context = context;
            _secretsProvider = secretsProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string?> GetOrCreateOnboardingLinkAsync(Guid adminId, string returnUrl, string refreshUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && !u.IsDeleted);
            if (user == null) return null;

            await ConfigureStripeAsync();

            if (user.StripeConnectedAccountId == null)
            {
                var account = await new AccountService().CreateAsync(new AccountCreateOptions
                {
                    Type = "express",
                    Email = user.Email,
                    Metadata = new Dictionary<string, string> { { "UserId", adminId.ToString() } }
                });
                user.StripeConnectedAccountId = account.Id;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
            {
                Account = user.StripeConnectedAccountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            });

            return link.Url;
        }

        /// <inheritdoc/>
        public async Task<ConnectStatusDTO> GetConnectStatusAsync(Guid adminId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && !u.IsDeleted);
            if (user?.StripeConnectedAccountId == null)
                return new ConnectStatusDTO { IsConnected = false };

            await ConfigureStripeAsync();

            var account = await new AccountService().GetAsync(user.StripeConnectedAccountId);

            return new ConnectStatusDTO
            {
                IsConnected = true,
                ChargesEnabled = account.ChargesEnabled,
                PayoutsEnabled = account.PayoutsEnabled,
            };
        }

        /// <inheritdoc/>
        public async Task<bool> HandleConnectWebhookAsync(string requestBody, string stripeSignature)
        {
            try
            {
                var webhookSecret = await _secretsProvider.GetSecretAsync(SecretKeys.StripeConnectWebhookSecret);
                var stripeEvent = EventUtility.ConstructEvent(requestBody, stripeSignature, webhookSecret);

                if (stripeEvent.Type == "account.updated")
                {
                    var account = stripeEvent.Data.Object as Stripe.Account;
                    if (account == null) return true;

                    var user = await _context.Users.FirstOrDefaultAsync(u =>
                        u.StripeConnectedAccountId == account.Id && !u.IsDeleted);
                    if (user == null)
                    {
                        _logger.LogWarning("account.updated webhook for unknown connected account {AccountId}.", account.Id);
                        return true;
                    }

                    user.StripeConnectChargesEnabled = account.ChargesEnabled;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Admin {AdminId} Connect account {AccountId}: ChargesEnabled={ChargesEnabled}.",
                        user.Id, account.Id, account.ChargesEnabled);
                }

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Connect webhook signature validation failed.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing Connect webhook.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> GetConnectedAccountIdAsync(Guid adminId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && !u.IsDeleted);
            if (user?.StripeConnectedAccountId == null || !user.StripeConnectChargesEnabled)
                return null;
            return user.StripeConnectedAccountId;
        }

        private async Task ConfigureStripeAsync()
        {
            var stripeKey = await _secretsProvider.GetSecretAsync(SecretKeys.StripeSecretKey);
            StripeConfiguration.ApiKey = stripeKey;
        }
    }
}
