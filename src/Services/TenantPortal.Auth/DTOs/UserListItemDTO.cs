using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.DTOs
{
    public class UserListItemDTO
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }

        /// <summary>"active" once registered; "pending" while the invite hasn't been accepted yet.</summary>
        public string Status => IsActive ? "active" : "pending";
    }
}
