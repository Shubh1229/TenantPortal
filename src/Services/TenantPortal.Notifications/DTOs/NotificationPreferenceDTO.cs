namespace TenantPortal.Notifications.DTOs
{
    /// <summary>Used both to read and update a user's email notification preference.</summary>
    public class NotificationPreferenceDTO
    {
        /// <summary>Whether the user wants to receive email notifications. <c>true</c> by default.</summary>
        public bool EmailEnabled { get; set; }
    }
}
