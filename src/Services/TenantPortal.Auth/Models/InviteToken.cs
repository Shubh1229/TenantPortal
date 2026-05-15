using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Models
{
    public class InviteToken
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required UserRole Role { get; set; }
        public required string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
