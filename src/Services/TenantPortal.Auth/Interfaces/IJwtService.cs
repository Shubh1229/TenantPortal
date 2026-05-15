using TenantPortal.Auth.Models;

namespace TenantPortal.Auth.Interfaces
{
    public interface IJwtService
    {
        public string CreateAccessToken(User user);
        public Guid? ValidateRefreshToken(string token);
        public string GenerateRefreshToken();

    }
}
