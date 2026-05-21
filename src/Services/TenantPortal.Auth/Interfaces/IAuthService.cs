using TenantPortal.Auth.DTOs;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Orchestrates the two-step login flow (password → TOTP), account registration,
    /// invite sending, token refresh, and logout.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Validates email and password. On success returns a short-lived opaque temporary token
        /// to be exchanged at <see cref="ValidateTotpAsync"/> — this is not a JWT.
        /// </summary>
        /// <returns>A temporary token string, or <c>null</c> if credentials are invalid.</returns>
        Task<string?> LoginAsync(string email, string password);

        /// <summary>
        /// Validates the TOTP code against the temp token from <see cref="LoginAsync"/>.
        /// On success consumes the temp token (single-use), persists a hashed refresh token,
        /// and returns a full access + refresh token pair.
        /// </summary>
        /// <returns>JWT access and opaque refresh token, or <c>null</c> if validation fails.</returns>
        Task<LoginResponseDTO?> ValidateTotpAsync(string tempToken, string totpCode);

        /// <summary>
        /// Validates an opaque refresh token against the hashed value stored in the database.
        /// On success rotates the refresh token (issues a new one, invalidates the old) and
        /// returns a new access + refresh token pair.
        /// </summary>
        /// <returns>New token pair, or <c>null</c> if the refresh token is invalid or expired.</returns>
        Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Invalidates a refresh token by clearing the stored hash, logging the user out
        /// from all sessions that share that token.
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Completes registration using a valid invite token. Creates the user account,
        /// generates a TOTP secret, and returns a QR code for authenticator app enrollment.
        /// </summary>
        /// <returns>TOTP setup data (manual key + QR code PNG as Base64), or <c>null</c> if the invite is invalid.</returns>
        Task<TotpSetupResponseDTO?> RegisterAsync(RegisterRequestDTO request);

        /// <summary>
        /// Creates a pending invite record for <paramref name="request"/>.Email.
        /// The plain-text invite token must be delivered to the invitee out-of-band
        /// (via the Notification service once inter-service communication is wired up).
        /// </summary>
        /// <param name="createdBy">ID of the Admin or Super Admin sending the invite.</param>
        /// <returns>Success flag and a user-facing error message (null on success).</returns>
        Task<(bool Success, string? Error)> SendInviteAsync(InviteRequestDTO request, Guid createdBy);

        /// <summary>
        /// Dev-only login that skips TOTP. Only works for the hardcoded dev test accounts.
        /// Returns <c>null</c> if credentials are wrong or the account is not a dev account.
        /// </summary>
        Task<LoginResponseDTO?> DevLoginAsync(string email, string password);

        /// <summary>
        /// Issues a new access token with <paramref name="targetRole"/> for a SuperAdmin.
        /// When <paramref name="targetRole"/> is <see cref="UserRole.SuperAdmin"/>, returns a normal
        /// (un-switched) token. All other roles produce a switched token with <c>is_switched=true</c>.
        /// </summary>
        Task<string?> SwitchRoleAsync(Guid superAdminId, UserRole targetRole);

        /// <summary>
        /// Returns active, non-deleted users. Admins only see users they invited;
        /// SuperAdmins see everyone.
        /// </summary>
        /// <param name="role">Optional role filter. <c>null</c> returns all roles.</param>
        /// <param name="callerId">ID of the requesting user (used for Admin scoping).</param>
        /// <param name="callerRole">Role of the requesting user.</param>
        Task<IEnumerable<UserListItemDTO>> GetUsersAsync(UserRole? role, Guid callerId, UserRole callerRole);

        /// <summary>Returns the full profile (email, role, profile fields, notification emails) for the given user.</summary>
        Task<UserProfileDTO?> GetUserProfileAsync(Guid userId);

        /// <summary>Returns public profile info (name, phone, emergency contact) for any user. Admin-scoped.</summary>
        Task<PublicUserProfileDTO?> GetPublicUserProfileAsync(Guid targetUserId);

        /// <summary>Creates or updates the user's personal profile. Marks IsProfileComplete = true on first save.</summary>
        Task<string?> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequestDTO request);

        /// <summary>Adds a notification email address. Returns error string or null on success.</summary>
        Task<string?> AddNotificationEmailAsync(Guid userId, string email);

        /// <summary>Removes a notification email by ID. Returns false if not found or belongs to another user.</summary>
        Task<bool> DeleteNotificationEmailAsync(Guid userId, Guid emailId);

        /// <summary>
        /// Changes the user's primary login email after verifying the current password.
        /// Returns an error string on failure, or <c>null</c> on success.
        /// </summary>
        Task<string?> UpdatePrimaryEmailAsync(Guid userId, string newEmail, string currentPassword);

        /// <summary>
        /// Changes the user's password after verifying the current password.
        /// Returns an error string on failure, or <c>null</c> on success.
        /// </summary>
        Task<string?> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        /// <summary>
        /// Soft-deletes the user's account. The caller must supply their own email address as
        /// confirmation. Returns an error string on failure, or <c>null</c> on success.
        /// </summary>
        Task<string?> DeleteAccountAsync(Guid userId, string confirmEmail);
    }
}
