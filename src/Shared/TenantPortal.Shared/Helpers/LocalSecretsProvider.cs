using System.Text.Json;
using TenantPortal.Shared.Exceptions;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Shared.Helpers
{
    /// <summary>
    /// <see cref="ISecretsProvider"/> implementation for local development.
    /// Reads secrets from a <c>secrets.json</c> file in the working directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>secrets.json</c> must be present in the project root when running locally
    /// and must be listed in <c>.gitignore</c> — never commit it to source control.
    /// </para>
    /// <para>Expected format:</para>
    /// <code>
    /// {
    ///   "Jwt__SigningKey": "...",
    ///   "Stripe__SecretKey": "...",
    ///   "Stripe__WebhookSecret": "...",
    ///   "AzureCommunicationServices__ConnectionString": "..."
    /// }
    /// </code>
    /// </remarks>
    public class LocalSecretsProvider : ISecretsProvider
    {
        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string secretName)
        {
            var json = await File.ReadAllTextAsync("secrets.json");
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (secrets == null || !secrets.TryGetValue(secretName, out var value))
                throw new NotFoundException($"Secret '{secretName}' not found in secrets.json");
            return value;
        }
    }
}
