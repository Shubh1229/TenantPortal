using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Auth.Controllers
{
    /// <summary>
    /// Manages Stripe Connect Express accounts for Admins.
    /// Onboarding links, status checks, and the Connect webhook are handled here.
    /// The internal endpoint is not exposed through the API gateway — it is called
    /// only by the Transactions service to resolve a connected account ID before
    /// creating a destination-charge PaymentIntent.
    /// </summary>
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly IConnectService _connectService;

        public ConnectController(IConnectService connectService)
        {
            _connectService = connectService;
        }

        /// <summary>
        /// Creates (or retrieves) the admin's Stripe Express account and returns
        /// a single-use hosted onboarding URL. The admin is redirected to Stripe
        /// to complete identity verification and bank account setup.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("api/auth/connect/onboard")]
        public async Task<IActionResult> GetOnboardingLinkAsync([FromBody] ConnectOnboardRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var url = await _connectService.GetOrCreateOnboardingLinkAsync(userId.Value, request.ReturnUrl, request.RefreshUrl);
            if (url == null) return NotFound("Admin account not found.");

            return Ok(new { onboardingUrl = url });
        }

        /// <summary>
        /// Returns the current Connect status for the authenticated admin:
        /// whether the account exists, whether charges and payouts are enabled,
        /// and a live Express dashboard login link when the account is active.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpGet("api/auth/connect/status")]
        public async Task<IActionResult> GetConnectStatusAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var status = await _connectService.GetConnectStatusAsync(userId.Value);
            return Ok(status);
        }

        /// <summary>
        /// Receives Stripe Connect account lifecycle webhook events (<c>account.updated</c>).
        /// Updates <c>StripeConnectChargesEnabled</c> so the Transactions service knows
        /// when an admin account is ready to receive destination charges.
        /// Verified via the <c>Stripe-Signature</c> header — does not require a JWT.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("api/webhooks/stripe/connect")]
        public async Task<IActionResult> HandleConnectWebhookAsync()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(stripeSignature))
                return BadRequest("Missing Stripe-Signature header.");

            var result = await _connectService.HandleConnectWebhookAsync(json, stripeSignature);
            if (!result) return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Internal endpoint called by the Transactions service to resolve a connected
        /// account ID when building a destination-charge PaymentIntent.
        /// Returns the account ID only when <c>StripeConnectChargesEnabled</c> is true.
        /// NOT exposed through the API gateway — internal traffic only.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("api/auth/internal/connected-account/{adminId:guid}")]
        public async Task<IActionResult> GetConnectedAccountIdAsync([FromRoute] Guid adminId)
        {
            var accountId = await _connectService.GetConnectedAccountIdAsync(adminId);
            if (accountId == null) return NotFound();
            return Ok(new { connectedAccountId = accountId });
        }

        private Guid? GetUserId() =>
            Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out var id) ? id : null;
    }
}
