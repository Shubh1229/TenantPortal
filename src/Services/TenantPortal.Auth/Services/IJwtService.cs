
using TenantPortal.Auth.Models;

namespace TenantPortal.Auth.Services
{
    public interface IJwtService
    {
        public string CreateAccessToken(User user);
        public Guid? ValidateRefreshToken(string token);
        public string GenerateRefreshToken();

    }
}
