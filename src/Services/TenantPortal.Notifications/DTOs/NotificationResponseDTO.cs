using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.DTOs
{
    /// <summary>In-app notification returned by the notifications list and detail endpoints.</summary>
    public class NotificationResponseDTO
    {
        /// <summary>Notification record ID.</summary>
        public required Guid Id { get; set; }

        /// <summary>The event that triggered this notification.</summary>
        public required NotificationType Type { get; set; }

        /// <summary>Human-readable message body displayed in the notification inbox.</summary>
        public required string Message { get; set; }

        /// <summary><c>true</c> once the user has read or dismissed this notification.</summary>
        public bool IsRead { get; set; }

        /// <summary>UTC timestamp when the notification was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
