using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Controllers
{
    /// <summary>
    /// Handles the two-step login flow (password → TOTP), registration via invite,
    /// token refresh, invite sending, and logout.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Step 1 of login. Validates email and password.
        /// Returns a short-lived temporary token to be used in the next step.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDTO request)
        {
            var tempToken = await _authService.LoginAsync(request.Email, request.Password);
            if (tempToken == null) return Unauthorized("Invalid email or password.");
            return Ok(tempToken);
        }

        /// <summary>
        /// Step 2 of login. Validates the TOTP code against the temp token from step 1.
        /// Returns a JWT access token and an opaque refresh token on success.
        /// </summary>
        [HttpPost("login/totp")]
        public async Task<IActionResult> ValidateTotpAsync([FromBody] TotpValidationRequestDTO request)
        {
            var result = await _authService.ValidateTotpAsync(request.TemporaryToken, request.TotpCode);
            if (result == null) return Unauthorized("Invalid or expired TOTP code.");
            return Ok(result);
        }

        /// <summary>
        /// Completes registration for an invited user. Validates the invite token,
        /// creates the account, and returns a TOTP QR code for authenticator app enrollment.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDTO request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null) return BadRequest("Registration failed. The invite token may be invalid or expired.");
            return Ok(result);
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access + refresh token pair.
        /// The old refresh token is invalidated (rotation).
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDTO request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (result == null) return Unauthorized("Invalid or expired refresh token.");
            return Ok(result);
        }

        /// <summary>
        /// Sends an account invite email to the specified address.
        /// Admin and Super Admin only; Admins cannot invite other Admins.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("invite")]
        public async Task<IActionResult> SendInviteAsync([FromBody] InviteRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token.");

            var result = await _authService.SendInviteAsync(request, userId.Value);
            if (!result) return BadRequest("Failed to send invite. The email may already be registered.");
            return Ok("Invite sent successfully.");
        }

        /// <summary>
        /// Logs out the authenticated user by revoking the supplied refresh token.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequestDTO request)
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok("Logout successful.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private (Guid? userId, UserRole? role) GetUserIdAndRole()
        {
            if (!Guid.TryParse(User.FindFirstValue(AppConstants.Claims.UserId), out Guid userId))
                return (null, null);
            if (!Enum.TryParse<UserRole>(User.FindFirstValue(AppConstants.Claims.UserRole), out UserRole role))
                return (null, null);
            return (userId, role);
        }
    }
}
