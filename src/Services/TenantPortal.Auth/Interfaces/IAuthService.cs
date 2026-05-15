using TenantPortal.Auth.DTOs;

namespace TenantPortal.Auth.Interfaces
{
    public interface IAuthService
    {
        public Task<string?> LoginAsync(string email, string password);
        public Task<LoginResponseDTO?> ValidateTotpAsync(string tempToken, string totpCode);
        public Task<TotpSetupResponseDTO?> RegisterAsync(RegisterRequestDTO request);
        public Task<bool> SendInviteAsync(InviteRequestDTO request, Guid createdBy);
        public Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken);
        public Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
