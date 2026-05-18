using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Grpc;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="INotificationsGrpcClient"/>
    public class NotificationsGrpcClient : INotificationsGrpcClient, IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly NotificationGrpcService.NotificationGrpcServiceClient _client;
        private readonly ILogger<NotificationsGrpcClient> _logger;

        public NotificationsGrpcClient(string grpcUrl, ILogger<NotificationsGrpcClient> logger)
        {
            _logger = logger;
            // Enable h2c (HTTP/2 cleartext) — TLS is terminated at ingress, not between containers.
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC SendInviteEmail failed for {Email}", toEmail);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "gRPC CreateInAppNotification failed for user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Connected, string Detail)> PingAsync()
        {
            try
            {
                // Send a request with an invalid UserId — the server will return InvalidArgument,
                // but any gRPC status other than Unavailable/DeadlineExceeded proves the channel is up.
                await _client.CreateInAppNotificationAsync(
                    new CreateInAppNotificationRequest { UserId = "ping", NotificationType = 0, Message = "system-test-ping" },
                    deadline: DateTime.UtcNow.AddSeconds(3));
                return (true, "Ready");
            }
            catch (RpcException ex) when (ex.StatusCode != StatusCode.Unavailable && ex.StatusCode != StatusCode.DeadlineExceeded)
            {
                // Got a gRPC-level error (e.g. InvalidArgument) — channel IS connected
                return (true, $"Connected (server status: {ex.StatusCode})");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public void Dispose() => _channel.Dispose();
    }
}
