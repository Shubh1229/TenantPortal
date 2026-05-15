using TenantPortal.Shared.Enums;

namespace TenantPortal.Notifications.DTOs
{
    public class NotificationResponseDTO
    {
        public required Guid Id { get; set; }
        public required NotificationType Type { get; set; }
        public required string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
