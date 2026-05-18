using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using TenantPortal.Notifications.Data;
using TenantPortal.Notifications.DTOs;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Notifications.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly ISecretsProvider _secretsProvider;
        public NotificationService(NotificationDbContext context, ISecretsProvider secretsProvider)
        {
            _context = context;
            _secretsProvider = secretsProvider;
        }
        public async Task<bool> CreateInAppNotificationAsync(Guid userId, NotificationType type, string message)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = type,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<bool> CreateReminderAsync(Guid userId, CreateReminderDTO request)
        {
            ReminderSetting newReminder = new ReminderSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DaysBefore = request.DaysBefore,
                SendTime = request.SendTime,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            try
            {
                await _context.ReminderSettings.AddAsync(newReminder);
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception) { return false;  }
        }

        public async Task<bool> DeleteReminderAsync(Guid reminderId, Guid userId)
        {
            try
            {
                var reminder = await _context.ReminderSettings.FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId);
                if (reminder == null) return false;
                reminder.IsActive = false;
                reminder.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<List<NotificationResponseDTO>> GetAllNotificationsAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            if (notifications == null || notifications.Count == 0) return new List<NotificationResponseDTO>();
            var response = notifications.Select(n => MapToDTO(n)).ToList();
            return response;
        }

        public async Task<NotificationResponseDTO?> GetNotificationAsync(Guid notificationId, Guid userId)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null) return null;
            return MapToDTO(notification);
        }

        public async Task<NotificationPreferenceDTO?> GetNotificationPreferenceAsync(Guid userId)
        {
            var preference = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            return new NotificationPreferenceDTO
            {
                EmailEnabled = preference?.EmailEnabled ?? true
            };
        }

        public async Task<List<ReminderSettingDTO>> GetRemindersAsync(Guid userId)
        {
            var settings = await _context.ReminderSettings.Where(r => r.UserId == userId && r.IsActive).ToListAsync();
            if (settings == null || !settings.Any()) return new List<ReminderSettingDTO>();
            List<ReminderSettingDTO> response = settings.Select(s => new ReminderSettingDTO
            {
                Id = s.Id,
                DaysBefore = s.DaysBefore,
                SendTime = s.SendTime,
                IsActive = s.IsActive
            }).ToList();
            return response;
        }

        public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                if (notification == null) return false;
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<bool> SendEmailAsync(Guid userId, string email, string subject, string body)
        {
            try
            {
                var connectionString = await _secretsProvider.GetSecretAsync(SecretKeys.AzureCommunicationServices);
                var senderAddress = await _secretsProvider.GetSecretAsync(SecretKeys.AzureEmailSenderAddress);
                var emailClient = new EmailClient(connectionString);
                var preference = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (preference == null || !preference.EmailEnabled) return true;
                await emailClient.SendAsync(
                    Azure.WaitUntil.Started,
                    senderAddress: senderAddress,
                    recipientAddress: email,
                    subject: subject,
                    htmlContent: body
                );
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<bool> UpdateNotificationPreferenceAsync(Guid userId, NotificationPreferenceDTO request)
        {
            try
            {
                var preference = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
                if (preference == null) return false;
                preference.UpdatedAt = DateTime.UtcNow;
                if (request.EmailEnabled != preference.EmailEnabled) preference.EmailEnabled = request.EmailEnabled;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<bool> UpdateReminderAsync(Guid userId, UpdateReminderDTO request)
        {
            try
            {
                var reminder = await _context.ReminderSettings.FirstOrDefaultAsync(r => r.Id == request.ReminderId && r.UserId == userId);
                if (reminder == null) return false;
                reminder.UpdatedAt = DateTime.UtcNow;
                if (request.DaysBefore.HasValue) reminder.DaysBefore = request.DaysBefore.Value;
                if (request.SendTime.HasValue) reminder.SendTime = request.SendTime.Value;
                if (request.IsActive.HasValue) reminder.IsActive = request.IsActive.Value;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task SendTesterActionEmailAsync(TesterActionDTO request)
        {
            try
            {
                var connectionString = await _secretsProvider.GetSecretAsync(SecretKeys.AzureCommunicationServices);
                var senderAddress = await _secretsProvider.GetSecretAsync(SecretKeys.AzureEmailSenderAddress);
                var emailClient = new EmailClient(connectionString);

                var encodedBody = System.Net.WebUtility.HtmlEncode(request.Body ?? "(none)");
                var htmlContent = $@"
<h3>Tester Action Intercepted</h3>
<p><strong>Tester:</strong> {request.TesterEmail}</p>
<p><strong>Action:</strong> {request.Action}</p>
<p><strong>Request body:</strong></p>
<pre style=""background:#f4f4f4;padding:10px;border-radius:4px"">{encodedBody}</pre>
<hr>
<p><em>This action was intercepted at the Gateway and was not persisted to the database.</em></p>";

                await emailClient.SendAsync(
                    Azure.WaitUntil.Started,
                    senderAddress: senderAddress,
                    recipientAddress: "shubh610@gmail.com",
                    subject: $"[Singh Resident Hub] Tester action: {request.Action}",
                    htmlContent: htmlContent
                );
            }
            catch (Exception) { }
        }

        private NotificationResponseDTO MapToDTO(Notification notification)
        {
            return new NotificationResponseDTO
            {
                Id = notification.Id,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}
