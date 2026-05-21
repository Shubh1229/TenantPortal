namespace TenantPortal.Auth.DTOs
{
    public class ForgotPasswordRequestDTO
    {
        public required string Email { get; set; }
        public required string FrontendBaseUrl { get; set; }
    }
}
