namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Thin wrapper around the generated gRPC client for the Notifications service.
    /// Abstracts the underlying channel so <see cref="Services.AuthService"/> can be unit-tested
    /// without a live gRPC server.
    /// </summary>
    public interface INotificationsGrpcClient
    {
        /// <summary>
        /// Dispatches an invite email via the Notifications service gRPC endpoint.
        /// Invite emails bypass the recipient's email-enabled preference (they are always sent).
        /// </summary>
        /// <param name="toEmail">Recipient address.</param>
        /// <param name="inviteToken">Plain-text token for the registration link.</param>
        /// <param name="role">Role name (e.g. "Admin", "Tenant") included in the email body.</param>
        /// <param name="frontendBaseUrl">Frontend base URL used to build the registration link.</param>
        /// <returns><c>true</c> on success; <c>false</c> if the Notifications service returned an error or is unreachable.</returns>
        Task<bool> SendInviteEmailAsync(string toEmail, string inviteToken, string role, string frontendBaseUrl);

        /// <summary>
        /// Creates an in-app notification for a user via the Notifications service.
        /// </summary>
        Task<bool> CreateInAppNotificationAsync(Guid userId, int notificationType, string message);

        /// <summary>
        /// Probes the gRPC channel with a 3-second deadline.
        /// Returns connected=true even if the server returns a protocol-level error (e.g. invalid argument),
        /// because that still proves the channel is up. Returns false only on Unavailable or timeout.
        /// </summary>
        Task<(bool Connected, string Detail)> PingAsync();
    }
}
