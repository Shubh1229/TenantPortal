namespace TenantPortal.Auth.Models
{
    /// <summary>
    /// Personal profile information for a registered user.
    /// Created the first time a user completes the profile-setup step.
    /// </summary>
    public class UserProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
