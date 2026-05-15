using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Notifications.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet]
        public async Task<IActionResult> GetNotificationsAsync()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var response = await _notificationService.GetAllNotificationsAsync(userId.Value);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificationAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var response = await _notificationService.GetNotificationAsync(id, userId.Value);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsReadAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var success = await _notificationService.MarkNotificationAsReadAsync(id, userId.Value);
            if (!success)
            {
                return NotFound();
            }
            return Ok("Notification read");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/notification-preferences")]
        public async Task<IActionResult> GetNotificationPreferenceAsync()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var response = await _notificationService.GetNotificationPreferenceAsync(userId.Value);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("/api/notification-preferences")]
        public async Task<IActionResult> UpdateNotificationPreferenceAsync([FromBody] NotificationPreferenceDTO request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var success = await _notificationService.UpdateNotificationPreferenceAsync(userId.Value, request);
            if (!success)
            {
                return NotFound();
            }
            return Ok("Notification preferences updated");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpGet("/api/reminders")]
        public async Task<IActionResult> GetRemindersAsync()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var response = await _notificationService.GetRemindersAsync(userId.Value);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("/api/reminders")]
        public async Task<IActionResult> CreateReminderAsync([FromBody] CreateReminderDTO request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var success = await _notificationService.CreateReminderAsync(userId.Value, request);
            if (!success)
            {
                return BadRequest("Failed to create reminder");
            }
            return Ok("Reminder created");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpDelete("/api/reminders/{id}")]
        public async Task<IActionResult> DeleteReminderAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var success = await _notificationService.DeleteReminderAsync(id, userId.Value);
            if (!success)
            {
                return NotFound();
            }
            return Ok("Reminder deleted");
        }


        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPatch("/api/reminders/{id}")]
        public async Task<IActionResult> UpdateReminderAsync([FromRoute] Guid id, [FromBody] UpdateReminderDTO request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return BadRequest("Invalid User Id Token");
            }
            var success = await _notificationService.UpdateReminderAsync(userId.Value, request);
            if (!success)
            {
                return NotFound();
            }
            return Ok("Reminder updated");
        }


        private Guid? GetUserId()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
            {
                return null;
            }
            return userId;
        }
    }
}
