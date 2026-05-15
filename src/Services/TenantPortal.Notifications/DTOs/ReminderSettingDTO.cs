namespace TenantPortal.Notifications.DTOs
{
    public class ReminderSettingDTO
    {
        public required Guid Id { get; set; }
        public required int DaysBefore { get; set; }
        public required TimeOnly SendTime { get; set; }
        public bool IsActive { get; set; }
    }
}
