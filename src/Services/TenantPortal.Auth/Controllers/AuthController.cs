using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Services;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDTO request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            if (result == null) return Unauthorized("Invalid email or password");
            return Ok(result);
        }

        [HttpPost("login/totp")]
        public async Task<IActionResult> ValidateTotpAsync([FromBody] TotpValidationRequestDTO request)
        {
            var result = await _authService.ValidateTotpAsync(request.TemporaryToken, request.TotpCode);
            if (result == null) return Unauthorized("Invalid TOTP code or temporary token");
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDTO request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null) return BadRequest("Registration failed. Please check your invite token and try again.");
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDTO request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (result == null) return Unauthorized("Invalid refresh token");
            return Ok(result);
        }

        [Authorize(Policy = AppConstants.Policies.RequireAdmin)]
        [HttpPost("invite")]
        public async Task<IActionResult> SendInviteAsync([FromBody] InviteRequestDTO request)
        {
            var (userId, _) = GetUserIdAndRole();
            if (userId == null) return BadRequest("Invalid user ID in token");
            var result = await _authService.SendInviteAsync(request, userId.Value);
            if (!result) return BadRequest("Failed to send invite. Please try again.");
            return Ok("Invite sent successfully");
        }

        [Authorize(Policy = AppConstants.Policies.RequireTenant)]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequestDTO request)
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok("Logout successful");
        }

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