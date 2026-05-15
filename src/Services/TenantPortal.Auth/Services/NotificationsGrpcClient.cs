using Grpc.Net.Client;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Grpc;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="INotificationsGrpcClient"/>
    public class NotificationsGrpcClient : INotificationsGrpcClient, IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly NotificationGrpcService.NotificationGrpcServiceClient _client;

        /// <param name="grpcUrl">
        /// Base URL of the Notifications service gRPC endpoint (e.g. <c>http://localhost:5004</c>).
        /// Loaded from the <c>Notifications:GrpcUrl</c> configuration key.
        /// Must be HTTP (not HTTPS) for cleartext HTTP/2 (h2c) inside the Docker network.
        /// </param>
        public NotificationsGrpcClient(string grpcUrl)
        {
            // Enable unencrypted HTTP/2 — required for h2c container-to-container gRPC.
            // HTTPS is handled at the load-balancer / ingress layer, not between internal services.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            _channel = GrpcChannel.ForAddress(grpcUrl);
            _client = new NotificationGrpcService.NotificationGrpcServiceClient(_channel);
        }

        /// <inheritdoc/>
        public async Task<bool> SendInviteEmailAsync(
            string toEmail,
            string inviteToken,
            string role,
            string frontendBaseUrl)
        {
            try
            {
                var result = await _client.SendInviteEmailAsync(new SendInviteEmailRequest
                {
                    ToEmail = toEmail,
                    InviteToken = inviteToken,
                    Role = role,
                    FrontendBaseUrl = frontendBaseUrl
                });
                return result.Success;
            }
            catch (Exception)
            {
                // gRPC failure must not block the invite flow — log and degrade gracefully.
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CreateInAppNotificationAsync(Guid userId, int notificationType, string message)
        {
            try
            {
                var result = await _client.CreateInAppNotificationAsync(new CreateInAppNotificationRequest
                {
                    UserId = userId.ToString(),
                    NotificationType = notificationType,
                    Message = message
                });
                return result.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose() => _channel.Dispose();
    }
}
