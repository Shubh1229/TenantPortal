using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using Microsoft.Extensions.Hosting;

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
        private readonly IHostEnvironment _env;

        public AuthController(IAuthService authService, IHostEnvironment env)
        {
            _authService = authService;
            _env = env;
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

            var (success, error) = await _authService.SendInviteAsync(request, userId.Value);
            if (!success) return BadRequest(new { error });
            return Ok(new { success = true });
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

        /// <summary>
        /// Dev-only login that skips TOTP verification.
        /// Returns 404 outside of the Development environment.
        /// Only the three hardcoded dev accounts (fakeadmin/faketenant/faketester) are accepted.
        /// </summary>
        [HttpPost("dev-login")]
        public async Task<IActionResult> DevLoginAsync([FromBody] DevLoginRequestDTO request)
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var result = await _authService.DevLoginAsync(request.Email, request.Password);
            if (result == null) return Unauthorized("Invalid credentials or account is not a dev account.");
            return Ok(result);
        }

        /// <summary>
        /// Issues a new access token with a downgraded role for UI testing.
        /// Only callable by SuperAdmin or an already-switched SuperAdmin.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("switch-role")]
        public async Task<IActionResult> SwitchRoleAsync([FromBody] SwitchRoleRequestDTO request)
        {
            var (userId, role) = GetUserIdAndRole();
            if (userId == null || role == null) return BadRequest("Invalid token");

            var isSwitched = User.FindFirstValue(AppConstants.Claims.IsSuperAdminSwitched) == "true";

            if (role != UserRole.SuperAdmin && !isSwitched)
                return Forbid();

            if (!Enum.TryParse<UserRole>(request.TargetRole, out var targetRole))
                return BadRequest($"Unknown role: {request.TargetRole}");

            var newToken = await _authService.SwitchRoleAsync(userId.Value, targetRole);
            if (newToken == null) return BadRequest("Switch failed.");

            return Ok(new { accessToken = newToken });
        }

        /// <summary>
        /// Returns active users, optionally filtered by role.
        /// Admins see only users they invited; SuperAdmin sees all.
        /// </summary>
        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAsync([FromQuery] string? role = null)
        {
            var (userId, callerRole) = GetUserIdAndRole();
            if (userId == null || callerRole == null) return BadRequest("Invalid user ID or role in token");

            UserRole? roleFilter = null;
            if (role != null && Enum.TryParse<UserRole>(role, out var parsed))
                roleFilter = parsed;

            var users = await _authService.GetUsersAsync(roleFilter, userId.Value, callerRole.Value);
            return Ok(users);
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
