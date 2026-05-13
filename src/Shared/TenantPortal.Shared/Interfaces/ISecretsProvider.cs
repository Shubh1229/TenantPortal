
using System.Collections.Specialized;
using System.Text;

namespace TenantPortal.Shared.Interfaces
{
    public interface ISecretsProvider
    {
        Task<string> GetSecretAsync(string secretName);
    }
}