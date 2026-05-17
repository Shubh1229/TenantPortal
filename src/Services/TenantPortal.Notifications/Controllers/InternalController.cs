using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;

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
    }
}
