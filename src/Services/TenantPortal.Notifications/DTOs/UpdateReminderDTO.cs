namespace TenantPortal.Notifications.DTOs
{
    /// <summary>Request body for updating an existing reminder. All fields are optional — only provided fields are applied.</summary>
    public class UpdateReminderDTO
    {
        /// <summary>The reminder to update. Populated from the route parameter by the controller.</summary>
        public required Guid ReminderId { get; set; }

        /// <summary>New days-before value, if being updated.</summary>
        public int? DaysBefore { get; set; }

        /// <summary>New send time, if being updated.</summary>
        public TimeOnly? SendTime { get; set; }

        /// <summary>New active state, if being toggled.</summary>
        public bool? IsActive { get; set; }
    }
}
