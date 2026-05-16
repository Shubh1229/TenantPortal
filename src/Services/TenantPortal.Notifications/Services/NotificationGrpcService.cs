using Grpc.Core;
using TenantPortal.Grpc;
using TenantPortal.Notifications.Data;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;
using TenantPortal.Shared.Constants;
using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;

namespace TenantPortal.Notifications.Services
{
    /// <summary>
    /// gRPC server implementation for the NotificationGrpcService proto.
    /// Handles inter-service notification requests (invite emails, in-app alerts, transactional emails)
    /// from Auth, Transactions, and Contracts services over HTTP/2 on the same Kestrel port.
    /// </summary>
    public class NotificationGrpcService : TenantPortal.Grpc.NotificationGrpcService.NotificationGrpcServiceBase
    {
        private readonly INotificationService _notificationService;
        private readonly ISecretsProvider _secretsProvider;
        private readonly NotificationDbContext _context;

        public NotificationGrpcService(
            INotificationService notificationService,
            ISecretsProvider secretsProvider,
            NotificationDbContext context)
        {
            _notificationService = notificationService;
            _secretsProvider = secretsProvider;
            _context = context;
        }

        /// <summary>
        /// Sends an account invitation email.
        /// Invite emails bypass the EmailEnabled preference — they are always sent.
        /// </summary>
        public override async Task<GrpcResult> SendInviteEmail(
            SendInviteEmailRequest request,
            ServerCallContext context)
        {
            try
            {
                var registrationUrl = $"{request.FrontendBaseUrl}/register?token={request.InviteToken}";

                var subject = "You've been invited to Tenant Portal";
                var body = $@"
                    <h2>Welcome to Tenant Portal</h2>
                    <p>You have been invited as a <strong>{request.Role}</strong>.</p>
                    <p>Click the link below to complete your registration. This link expires in 48 hours.</p>
                    <p><a href=""{registrationUrl}"">Complete Registration</a></p>
                    <p>Or copy this URL: {registrationUrl}</p>";

                var connectionString = await _secretsProvider.GetSecretAsync(SecretKeys.AzureCommunicationServices);
                var emailClient = new EmailClient(connectionString);

                await emailClient.SendAsync(
                    Azure.WaitUntil.Completed,
                    senderAddress: "noreply@tenantportal.com",
                    recipientAddress: request.ToEmail,
                    subject: subject,
                    htmlContent: body);

                return new GrpcResult { Success = true };
            }
            catch (Exception ex)
            {
                return new GrpcResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Creates an in-app notification for the specified user.
        /// Always delivered regardless of email preferences.
        /// </summary>
        public override async Task<GrpcResult> CreateInAppNotification(
            CreateInAppNotificationRequest request,
            ServerCallContext context)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new GrpcResult { Success = false, Error = "Invalid user ID format." };

            var success = await _notificationService.CreateInAppNotificationAsync(
                userId,
                (NotificationType)request.NotificationType,
                request.Message);

            return new GrpcResult { Success = success };
        }

        /// <summary>
        /// Sends a transactional email, respecting the recipient's email-enabled preference.
        /// </summary>
        public override async Task<GrpcResult> SendTransactionalEmail(
            SendTransactionalEmailRequest request,
            ServerCallContext context)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new GrpcResult { Success = false, Error = "Invalid user ID format." };

            var success = await _notificationService.SendEmailAsync(
                userId,
                request.ToEmail,
                request.Subject,
                request.HtmlBody);

            return new GrpcResult { Success = success };
        }
    }
}
