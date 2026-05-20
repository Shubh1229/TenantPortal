namespace TenantPortal.Auth.DTOs
{
    public class UpdateNotificationEmailRequestDTO
    {
        /// <summary>Pass null or empty string to clear the notification email.</summary>
        public string? NotificationEmail { get; set; }
    }
}
