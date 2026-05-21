namespace TenantPortal.Auth.DTOs
{
    public class ResetPasswordRequestDTO
    {
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
    }
}
