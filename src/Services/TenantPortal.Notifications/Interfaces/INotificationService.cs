using TenantPortal.Notifications.DTOs;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponseDTO>> GetAllNotificationsAsync(Guid userId);
        Task<NotificationResponseDTO?> GetNotificationAsync(Guid notificationId, Guid userId);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
        Task<NotificationPreferenceDTO?> GetNotificationPreferenceAsync(Guid userId);
        Task<bool> UpdateNotificationPreferenceAsync(Guid userId, NotificationPreferenceDTO request);
        Task<List<ReminderSettingDTO>> GetRemindersAsync(Guid userId);
        Task<bool> CreateReminderAsync(Guid userId, CreateReminderDTO request);
        Task<bool> UpdateReminderAsync(Guid userId, UpdateReminderDTO request);
        Task<bool> DeleteReminderAsync(Guid reminderId, Guid userId);
        Task<bool> SendEmailAsync(Guid userId, string email, string subject, string body);
        Task<bool> CreateInAppNotificationAsync(Guid userId, NotificationType type, string message);
    }
}
