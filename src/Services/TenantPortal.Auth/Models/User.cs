using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string TotpSecret { get; set; }
        public required UserRole Role {  get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? InvitedBy { get; set; }
    }
}
