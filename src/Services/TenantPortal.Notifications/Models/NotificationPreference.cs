namespace TenantPortal.Notifications.Models
{
    public class NotificationPreference
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public bool EmailEnabled { get; set; } = true;
        public DateTime UpdatedAt { get; set; }
    }
}