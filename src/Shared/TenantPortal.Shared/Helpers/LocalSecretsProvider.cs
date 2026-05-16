using System.Text.Json;
using TenantPortal.Shared.Exceptions;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Shared.Helpers
{
    /// <summary>
    /// <see cref="ISecretsProvider"/> implementation for local development.
    /// Reads secrets from a <c>.secrets/secrets.json</c> file located by walking
    /// up the directory tree from the current working directory to the solution root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.secrets/secrets.json</c> must exist at the solution root and must be
    /// listed in <c>.gitignore</c> — never commit it to source control.
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
        private static readonly string? SecretsPath = FindSecretsFile();

        private static string? FindSecretsFile()
        {
            var directory = Directory.GetCurrentDirectory();
            while (true)
            {
                var candidate = Path.Combine(directory, ".secrets", "secrets.json");
                if (File.Exists(candidate))
                    return candidate;
                var parent = Directory.GetParent(directory);
                if (parent == null)
                    return null; // file not found, will use env vars
                directory = parent.FullName;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string secretName)
        {
            // Check environment variables first (Docker)
            var envValue = Environment.GetEnvironmentVariable(secretName);
            if (!string.IsNullOrEmpty(envValue))
                return envValue;

            // Fall back to secrets.json (local dev)
            if (SecretsPath == null)
                throw new NotFoundException($"Secret '{secretName}' not found — no secrets.json and no environment variable set");

            var json = await File.ReadAllTextAsync(SecretsPath);
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (secrets == null || !secrets.TryGetValue(secretName, out var value))
                throw new NotFoundException($"Secret '{secretName}' not found in secrets.json");
            return value;
        }
    }
}