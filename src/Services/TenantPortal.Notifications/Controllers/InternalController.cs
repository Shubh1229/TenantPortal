using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.Controllers
{
    /// <summary>
    /// Internal-only endpoints called by other services within the Docker network.
    /// Not exposed through the Gateway and requires no JWT.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    public class InternalController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public InternalController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Called by the Gateway when a Tester account attempts a write operation.
        /// Sends an email summary to the Super Admin. No authentication required.
        /// </summary>
        [HttpPost("api/notifications/internal/tester-action")]
        public async Task<IActionResult> LogTesterActionAsync([FromBody] TesterActionDTO request)
        {
            await _notificationService.SendTesterActionEmailAsync(request);
            return Ok();
        }

        /// <summary>
        /// Called by other services (e.g. Transactions on Stripe payment failure) to create
        /// an in-app notification for a specific user without going through the Gateway.
        /// </summary>
        [HttpPost("api/notifications/internal/in-app")]
        public async Task<IActionResult> CreateInAppNotificationAsync([FromBody] CreateInAppNotificationRequestDTO request)
        {
            await _notificationService.CreateInAppNotificationAsync(request.UserId, request.Type, request.Message);
            return Ok();
        }

        /// <summary>
        /// Called by the Auth service when Stripe fires <c>customer.subscription.trial_will_end</c>.
        /// Creates an in-app notification and sends an email reminding the admin to add a payment method.
        /// </summary>
        [HttpPost("api/notifications/internal/trial-ending")]
        public async Task<IActionResult> NotifyTrialEndingAsync([FromBody] TrialEndingRequestDTO request)
        {
            var daysText = request.DaysLeft == 1 ? "1 day" : $"{request.DaysLeft} days";
            var message = $"Your free trial ends in {daysText}. Add a payment method to keep your account active and allow tenant payments.";

            await _notificationService.CreateInAppNotificationAsync(request.UserId, NotificationType.TrialEnding, message);

            var subject = $"[Singh Resident Hub] Your trial ends in {daysText}";
            var htmlBody = $@"<p>Hi,</p>
<p>Your Singh Resident Hub free trial ends in <strong>{daysText}</strong>.</p>
<p>Add a payment method before your trial expires to continue using the platform and allow your tenants to submit rent payments.</p>
<p><a href=""https://singhresidenthub.com/admin/subscription"">Manage your subscription</a></p>
<hr>
<p style=""color:#999;font-size:12px"">Singh Resident Hub · singhresidenthub.com</p>";

            await _notificationService.SendEmailAsync(request.UserId, request.Email, subject, htmlBody);

            return Ok();
        }
    }
}
