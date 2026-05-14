
using TenantPortal.Auth.Models;

namespace TenantPortal.Auth.Services
{
    public interface ITotpService
    {
        public string GenerateSecret();
        public string GenerateQrCode(string secret, string email);
        public bool ValidateTotpToken(string secret, string token);
    }
}
