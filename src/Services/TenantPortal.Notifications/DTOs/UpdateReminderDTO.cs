namespace TenantPortal.Notifications.DTOs
{
    public class UpdateReminderDTO
    {
        public required Guid ReminderId { get; set; }
        public int? DaysBefore { get; set; }
        public TimeOnly? SendTime { get; set; }
        public bool? IsActive { get; set; }
    }
}
