using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Auth.Controllers
{
    /// <summary>
    /// Handles self-serve Admin registration and SaaS subscription management.
    /// </summary>
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Self-serve Admin registration. Creates an inactive Admin account and returns
        /// a Stripe Checkout URL to complete payment, plus a TOTP QR code for authenticator setup.
        /// The account is activated automatically once Stripe confirms the subscription via webhook.
        /// </summary>
        /// <remarks>
        /// The caller should simultaneously display the TOTP QR code and redirect to the checkout URL
        /// so the admin can enrol their authenticator app before completing payment.
        /// </remarks>
        [HttpPost("api/auth/register/admin")]
        public async Task<IActionResult> RegisterAdminAsync([FromBody] AdminRegisterRequestDTO request)
        {
            var result = await _subscriptionService.RegisterAdminAsync(request);
            if (result == null)
                return Conflict("An account with this email address already exists.");
            return Ok(result);
        }

        /// <summary>
        /// Returns the current subscription status and tenant usage for the authenticated Admin.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpGet("api/auth/subscription/status")]
        public async Task<IActionResult> GetSubscriptionStatusAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var status = await _subscriptionService.GetSubscriptionStatusAsync(userId.Value);
            if (status == null) return NotFound("Subscription record not found.");
            return Ok(status);
        }

        /// <summary>
        /// Creates a Stripe Billing Portal session for the authenticated Admin.
        /// The portal lets them update payment methods, download invoices, and cancel their subscription.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("api/auth/subscription/portal")]
        public async Task<IActionResult> GetBillingPortalAsync([FromBody] BillingPortalRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var portalUrl = await _subscriptionService.CreateCustomerPortalSessionAsync(userId.Value, request.ReturnUrl);
            if (portalUrl == null)
                return BadRequest("No Stripe customer record found for this account. Self-registered Admin accounts only.");
            return Ok(new { portalUrl });
        }

        /// <summary>
        /// Receives Stripe subscription lifecycle webhook events.
        /// Activates or suspends Admin accounts based on subscription status changes.
        /// Verified via the <c>Stripe-Signature</c> header — does not require a JWT.
        /// </summary>
        [HttpPost("api/webhooks/stripe/subscription")]
        public async Task<IActionResult> HandleSubscriptionWebhookAsync()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrEmpty(stripeSignature))
                return BadRequest("Missing Stripe-Signature header.");

            var result = await _subscriptionService.HandleSubscriptionWebhookAsync(json, stripeSignature);
            if (!result) return BadRequest();
            return Ok();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private Guid? GetUserId()
        {
            return Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out var id) ? id : null;
        }
    }
}
