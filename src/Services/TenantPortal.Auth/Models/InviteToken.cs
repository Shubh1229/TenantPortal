using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Models
{
    /// <summary>
    /// Tracks a pending account invitation. The plain-text token is emailed to the invitee;
    /// only its SHA-256 hash is stored here for safe comparison at registration time.
    /// </summary>
    public class InviteToken
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>Email address the invite was sent to. Becomes the new user's login identifier.</summary>
        public required string Email { get; set; }

        /// <summary>Role the new user will receive on completing registration.</summary>
        public required UserRole Role { get; set; }

        /// <summary>SHA-256 hash of the plain-text invite token. Used for safe comparison at registration.</summary>
        public required string TokenHash { get; set; }

        /// <summary>UTC expiry of the invite link. Invites expire after 48 hours.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary><c>true</c> once the invitee has completed registration — prevents re-use.</summary>
        public bool Used { get; set; }

        /// <summary>ID of the Admin or Super Admin who sent this invite.</summary>
        public Guid CreatedBy { get; set; }

        /// <summary>UTC timestamp when the invite was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
