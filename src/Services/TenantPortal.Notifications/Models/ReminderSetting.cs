namespace TenantPortal.Notifications.Models
{
    /// <summary>
    /// A configurable rent reminder for a user. Each user can have multiple independent reminders,
    /// each firing a set number of days before the rent due date at a specific time of day.
    /// Deactivated reminders have <see cref="IsActive"/> set to <c>false</c> (soft-delete pattern).
    /// </summary>
    public class ReminderSetting
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The user who owns this reminder configuration.</summary>
        public required Guid UserId { get; set; }

        /// <summary>How many days before the due date the reminder should fire.</summary>
        public required int DaysBefore { get; set; }

        /// <summary>The time of day (in the user's configured time zone) at which the reminder fires.</summary>
        public required TimeOnly SendTime { get; set; }

        /// <summary><c>false</c> when the user has deleted or deactivated this reminder.</summary>
        public bool IsActive { get; set; }

        /// <summary>UTC timestamp when this reminder was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
