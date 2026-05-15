using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Notifications.Controllers
{
    /// <summary>
    /// Handles in-app notifications, notification preferences, and rent reminders.
    /// All endpoints require at minimum the Tenant role.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>Returns all in-app notifications for the authenticated user, newest first.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet]
        public async Task<IActionResult> GetNotificationsAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var response = await _notificationService.GetAllNotificationsAsync(userId.Value);
            if (response == null) return NotFound();
            return Ok(response);
        }

        /// <summary>Returns a single notification by ID. Users may only access their own notifications.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificationAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var response = await _notificationService.GetNotificationAsync(id, userId.Value);
            if (response == null) return NotFound();
            return Ok(response);
        }

        /// <summary>Marks a notification as read. Users may only mark their own notifications.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsReadAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var success = await _notificationService.MarkNotificationAsReadAsync(id, userId.Value);
            if (!success) return NotFound();
            return Ok("Notification marked as read");
        }

        /// <summary>Returns the email notification preference for the authenticated user.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/notification-preferences")]
        public async Task<IActionResult> GetNotificationPreferenceAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var response = await _notificationService.GetNotificationPreferenceAsync(userId.Value);
            if (response == null) return NotFound();
            return Ok(response);
        }

        /// <summary>Updates the email notification preference for the authenticated user.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("/api/notification-preferences")]
        public async Task<IActionResult> UpdateNotificationPreferenceAsync([FromBody] NotificationPreferenceDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var success = await _notificationService.UpdateNotificationPreferenceAsync(userId.Value, request);
            if (!success) return NotFound();
            return Ok("Notification preferences updated");
        }

        /// <summary>Returns all active rent reminders for the authenticated user.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/reminders")]
        public async Task<IActionResult> GetRemindersAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var response = await _notificationService.GetRemindersAsync(userId.Value);
            if (response == null) return NotFound();
            return Ok(response);
        }

        /// <summary>Creates a new rent reminder for the authenticated user.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("/api/reminders")]
        public async Task<IActionResult> CreateReminderAsync([FromBody] CreateReminderDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var success = await _notificationService.CreateReminderAsync(userId.Value, request);
            if (!success) return BadRequest("Failed to create reminder");
            return Ok("Reminder created");
        }

        /// <summary>Soft-deletes (deactivates) a reminder. Users may only delete their own.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpDelete("/api/reminders/{id}")]
        public async Task<IActionResult> DeleteReminderAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            var success = await _notificationService.DeleteReminderAsync(id, userId.Value);
            if (!success) return NotFound();
            return Ok("Reminder deleted");
        }

        /// <summary>Updates a reminder's days-before, send time, or active status.</summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("/api/reminders/{id}")]
        public async Task<IActionResult> UpdateReminderAsync([FromRoute] Guid id, [FromBody] UpdateReminderDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token");

            // Route id is authoritative — prevents a body ReminderId from targeting a different record
            request.ReminderId = id;
            var success = await _notificationService.UpdateReminderAsync(userId.Value, request);
            if (!success) return NotFound();
            return Ok("Reminder updated");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private Guid? GetUserId()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
                return null;
            return userId;
        }
    }
}
