using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for sending an account invitation.</summary>
    public class InviteRequestDTO
    {
        /// <summary>Email address of the person being invited. Must not belong to an existing user.</summary>
        public required string Email { get; set; }

        /// <summary>Role the invitee will receive on completing registration.</summary>
        public required UserRole Role { get; set; }
    }
}
