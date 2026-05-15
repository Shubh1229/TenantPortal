namespace TenantPortal.Notifications.DTOs
{
    public class CreateReminderDTO
    {
        public required int DaysBefore { get; set; }
        public required TimeOnly SendTime { get; set; }
    }
}
