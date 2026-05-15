namespace TenantPortal.Auth.DTOs
{
    public class RegisterRequestDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required string InviteToken { get; set; }
    }
}
