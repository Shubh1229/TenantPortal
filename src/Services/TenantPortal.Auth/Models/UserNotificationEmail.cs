namespace TenantPortal.Auth.Models
{
    /// <summary>
    /// An additional email address for a user that receives notification emails.
    /// A user may have zero or more of these; their primary <see cref="User.Email"/> always receives notifications.
    /// </summary>
    public class UserNotificationEmail
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
