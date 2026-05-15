using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.DTOs
{
    public class InviteRequestDTO
    {
        public required string Email { get; set; }
        public required UserRole Role { get; set; }
    }
}
