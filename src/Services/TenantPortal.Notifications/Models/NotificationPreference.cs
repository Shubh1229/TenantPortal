namespace TenantPortal.Notifications.Models
{
    /// <summary>
    /// Stores a user's email notification opt-in/opt-out preference.
    /// One record per user; in-app notifications are always on regardless of this setting.
    /// </summary>
    public class NotificationPreference
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The user this preference belongs to.</summary>
        public required Guid UserId { get; set; }

        /// <summary>
        /// When <c>true</c>, email notifications are sent for applicable events.
        /// When <c>false</c>, only in-app notifications are delivered.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool EmailEnabled { get; set; } = true;

        /// <summary>UTC timestamp of the last preference update.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
