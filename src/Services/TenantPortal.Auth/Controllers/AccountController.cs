using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Auth.Controllers
{
    [ApiController]
    [Route("api/auth/account")]
    [Authorize(Policy = AppConstants.Policies.RequireTenant)]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // ── Profile ──────────────────────────────────────────────────────────────────

        /// <summary>Returns the caller's full profile including name, phone, emergency contact, and notification emails.</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileAsync()
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var profile = await _authService.GetUserProfileAsync(userId.Value);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        /// <summary>Creates or updates the caller's personal profile. Marks IsProfileComplete = true.</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateUserProfileRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var error = await _authService.UpdateUserProfileAsync(userId.Value, request);
            if (error != null) return BadRequest(new { error });
            return Ok(new { success = true });
        }

        // ── Notification Emails ───────────────────────────────────────────────────────

        /// <summary>Adds a secondary notification email address.</summary>
        [HttpPost("notification-emails")]
        public async Task<IActionResult> AddNotificationEmailAsync([FromBody] AddNotificationEmailRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var error = await _authService.AddNotificationEmailAsync(userId.Value, request.Email);
            if (error != null) return BadRequest(new { error });
            return Ok(new { success = true });
        }

        /// <summary>Removes a notification email by its ID. At least the primary email always remains.</summary>
        [HttpDelete("notification-emails/{id}")]
        public async Task<IActionResult> DeleteNotificationEmailAsync([FromRoute] Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var ok = await _authService.DeleteNotificationEmailAsync(userId.Value, id);
            if (!ok) return NotFound("Notification email not found.");
            return Ok(new { success = true });
        }

        // ── Primary Email ─────────────────────────────────────────────────────────────

        [HttpPut("primary-email")]
        public async Task<IActionResult> UpdatePrimaryEmailAsync([FromBody] UpdatePrimaryEmailRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var error = await _authService.UpdatePrimaryEmailAsync(userId.Value, request.NewEmail, request.CurrentPassword);
            if (error != null) return BadRequest(new { error });
            return Ok(new { success = true, message = "Email updated. Please log in again with your new email." });
        }

        // ── Password ──────────────────────────────────────────────────────────────────

        [HttpPut("password")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var error = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            if (error != null) return BadRequest(new { error });
            return Ok(new { success = true, message = "Password changed. Please log in again." });
        }

        // ── Delete Account ────────────────────────────────────────────────────────────

        [HttpDelete]
        public async Task<IActionResult> DeleteAccountAsync([FromBody] DeleteAccountRequestDTO request)
        {
            var userId = GetUserId();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var error = await _authService.DeleteAccountAsync(userId.Value, request.ConfirmEmail);
            if (error != null) return BadRequest(new { error });
            return Ok(new { success = true });
        }

        private Guid? GetUserId()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out var id))
                return null;
            return id;
        }
    }
}
