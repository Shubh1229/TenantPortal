using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Shared.Helpers
{
    public class AzureVaultSecretsProvider : ISecretsProvider
    {
        private readonly SecretClient _client;

        public AzureVaultSecretsProvider(string vaultUri)
        {
            _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            var kvName = secretName.Replace("__", "--");
            var response = await _client.GetSecretAsync(kvName);
            return response.Value.Value;
        }
    }
}
