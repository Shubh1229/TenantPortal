namespace TenantPortal.Auth.DTOs
{
    public class UserProfileDTO
    {
        public required string Email { get; set; }
        public required string Role { get; set; }
        public bool IsProfileComplete { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public List<NotificationEmailDTO> NotificationEmails { get; set; } = [];
    }

    public class NotificationEmailDTO
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
    }
}
