namespace TenantPortal.Notifications.DTOs
{
    /// <summary>Request body for creating a new rent reminder.</summary>
    public class CreateReminderDTO
    {
        /// <summary>How many days before the rent due date this reminder should fire.</summary>
        public required int DaysBefore { get; set; }

        /// <summary>Time of day at which the reminder notification is sent.</summary>
        public required TimeOnly SendTime { get; set; }
    }
}
