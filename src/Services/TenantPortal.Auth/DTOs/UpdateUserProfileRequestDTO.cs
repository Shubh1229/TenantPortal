namespace TenantPortal.Auth.DTOs
{
    public class UpdateUserProfileRequestDTO
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
