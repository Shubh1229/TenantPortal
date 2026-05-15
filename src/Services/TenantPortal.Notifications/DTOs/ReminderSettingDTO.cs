namespace TenantPortal.Notifications.DTOs
{
    /// <summary>A reminder configuration returned by the reminders list endpoint.</summary>
    public class ReminderSettingDTO
    {
        /// <summary>Reminder record ID.</summary>
        public required Guid Id { get; set; }

        /// <summary>How many days before the rent due date this reminder fires.</summary>
        public required int DaysBefore { get; set; }

        /// <summary>Time of day the reminder is sent.</summary>
        public required TimeOnly SendTime { get; set; }

        /// <summary><c>true</c> while the reminder is active; <c>false</c> if deactivated.</summary>
        public bool IsActive { get; set; }
    }
}
