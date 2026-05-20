namespace TenantPortal.Auth.DTOs
{
    public class AccountProfileDTO
    {
        public required string Email { get; set; }
        public string? NotificationEmail { get; set; }
        public required string Role { get; set; }
    }
}
