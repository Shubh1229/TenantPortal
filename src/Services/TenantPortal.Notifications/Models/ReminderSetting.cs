namespace TenantPortal.Notifications.Models
{
    public class ReminderSetting
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required int DaysBefore { get; set; }
        public required TimeOnly SendTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
