namespace TenantPortal.Shared.Interfaces
{
    /// <summary>
    /// Abstracts secret retrieval so local development and cloud environments
    /// can be swapped without changing service code.
    /// </summary>
    /// <remarks>
    /// Two implementations are provided:
    /// <list type="bullet">
    ///   <item><see cref="TenantPortal.Shared.Helpers.LocalSecretsProvider"/> — reads from <c>secrets.json</c> (local dev only, never committed).</item>
    ///   <item>AzureKeyVaultSecretsProvider — reads from Azure Key Vault (staging and production).</item>
    /// </list>
    /// </remarks>
    public interface ISecretsProvider
    {
        /// <summary>
        /// Retrieves a secret by name from the configured backing store.
        /// </summary>
        /// <param name="secretName">
        /// The key to look up. Use constants from <see cref="TenantPortal.Shared.Constants.SecretKeys"/>
        /// to avoid magic strings.
        /// </param>
        /// <returns>The plain-text secret value.</returns>
        /// <exception cref="TenantPortal.Shared.Exceptions.NotFoundException">
        /// Thrown when the requested key is not present in the backing store.
        /// </exception>
        Task<string> GetSecretAsync(string secretName);
    }
}
