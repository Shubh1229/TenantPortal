namespace TenantPortal.Auth.DTOs
{
    public class LogoutRequestDTO
    {
        public required string RefreshToken { get; set; }
    }
}
