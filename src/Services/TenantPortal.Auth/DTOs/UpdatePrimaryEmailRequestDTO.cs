namespace TenantPortal.Auth.DTOs
{
    public class UpdatePrimaryEmailRequestDTO
    {
        public required string NewEmail { get; set; }
        public required string CurrentPassword { get; set; }
    }
}
