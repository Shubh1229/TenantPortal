using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.Models
{
    /// <summary>
    /// An in-app notification delivered to a specific user.
    /// In-app notifications are always delivered and cannot be disabled by the user.
    /// </summary>
    public class Notification
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The user this notification is addressed to.</summary>
        public required Guid UserId { get; set; }

        /// <summary>The event that triggered this notification.</summary>
        public required NotificationType Type { get; set; }

        /// <summary>Human-readable notification body shown in the notification bell/inbox.</summary>
        public required string Message { get; set; }

        /// <summary><c>true</c> once the user has viewed or explicitly dismissed the notification.</summary>
        public bool IsRead { get; set; }

        /// <summary>UTC timestamp when this notification was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
