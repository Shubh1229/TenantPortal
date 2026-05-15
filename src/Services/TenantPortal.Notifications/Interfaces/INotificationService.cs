using TenantPortal.Notifications.DTOs;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.Interfaces
{
    /// <summary>
    /// Manages in-app notifications, email dispatch, user notification preferences,
    /// and configurable rent reminders.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Returns all in-app notifications for a user, sorted newest first.</summary>
        Task<List<NotificationResponseDTO>> GetAllNotificationsAsync(Guid userId);

        /// <summary>Returns a single notification owned by the specified user.</summary>
        Task<NotificationResponseDTO?> GetNotificationAsync(Guid notificationId, Guid userId);

        /// <summary>Marks a notification as read. Only the owning user may mark their notifications.</summary>
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>Returns the email notification preference for the specified user.</summary>
        Task<NotificationPreferenceDTO?> GetNotificationPreferenceAsync(Guid userId);

        /// <summary>Updates the email enabled/disabled preference for the specified user.</summary>
        Task<bool> UpdateNotificationPreferenceAsync(Guid userId, NotificationPreferenceDTO request);

        /// <summary>Returns all active reminder settings for the specified user.</summary>
        Task<List<ReminderSettingDTO>> GetRemindersAsync(Guid userId);

        /// <summary>Creates a new reminder for the specified user.</summary>
        Task<bool> CreateReminderAsync(Guid userId, CreateReminderDTO request);

        /// <summary>Updates an existing reminder owned by the specified user.</summary>
        Task<bool> UpdateReminderAsync(Guid userId, UpdateReminderDTO request);

        /// <summary>Deactivates (soft-deletes) a reminder owned by the specified user.</summary>
        Task<bool> DeleteReminderAsync(Guid reminderId, Guid userId);

        /// <summary>
        /// Sends a transactional email via Azure Communication Services,
        /// but only if the recipient has email notifications enabled.
        /// Returns <c>true</c> when email is disabled (not an error — no action needed).
        /// </summary>
        Task<bool> SendEmailAsync(Guid userId, string email, string subject, string body);

        /// <summary>
        /// Creates a new in-app notification record for the specified user.
        /// In-app notifications are always delivered regardless of email preference.
        /// </summary>
        Task<bool> CreateInAppNotificationAsync(Guid userId, NotificationType type, string message);
    }
}
