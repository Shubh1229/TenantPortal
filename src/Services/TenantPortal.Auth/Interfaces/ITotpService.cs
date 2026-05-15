using TenantPortal.Auth.Models;

namespace TenantPortal.Auth.Interfaces
{
    public interface ITotpService
    {
        public string GenerateSecret();
        public string GenerateQrCode(string secret, string email);
        public bool ValidateTotpToken(string secret, string token);
    }
}
