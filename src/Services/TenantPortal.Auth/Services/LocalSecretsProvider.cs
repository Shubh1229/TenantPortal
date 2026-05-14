using System.Text.Json;
using TenantPortal.Shared.Exceptions;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    public class LocalSecretsProvider : ISecretsProvider
    {
        public async Task<string> GetSecretAsync(string secretName)
        {
            var json = await File.ReadAllTextAsync("secrets.json");
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (secrets == null || !secrets.TryGetValue(secretName, out var value))
                throw new NotFoundException($"Secret '{secretName}' not found");
            return value;
        }
    }
}

