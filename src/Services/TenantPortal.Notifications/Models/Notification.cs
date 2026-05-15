using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.Models
{
    public class Notification
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required NotificationType Type { get; set; }
        public required string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
