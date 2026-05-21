namespace TenantPortal.Auth.DTOs
{
    public class ChangePasswordRequestDTO
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}
