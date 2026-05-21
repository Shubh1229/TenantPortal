namespace TenantPortal.Auth.DTOs
{
    public class DevLoginRequestDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
