using TenantPortal.Auth.DTOs;

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
    }
}
