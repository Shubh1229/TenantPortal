using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.DTOs
{
    public class CreateInAppNotificationRequestDTO
    {
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public required string Message { get; set; }
    }
}
