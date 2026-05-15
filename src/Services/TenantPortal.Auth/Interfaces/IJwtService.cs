using TenantPortal.Auth.Models;

namespace TenantPortal.Auth.Interfaces
{
    /// <summary>
    /// Handles JWT access token creation and opaque refresh token generation.
    /// Refresh token validation is intentionally handled in <see cref="IAuthService"/>
    /// because it requires a database lookup against the stored hash — it cannot
    /// be performed by parsing the token alone.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Creates a short-lived (15-minute) signed JWT access token containing
        /// the user's ID, email, and role as claims.
        /// </summary>
        /// <param name="user">The authenticated user whose claims will be embedded.</param>
        /// <returns>A signed JWT string ready to be returned to the client.</returns>
        string CreateAccessToken(User user);

        /// <summary>
        /// Generates a cryptographically random opaque refresh token.
        /// The caller is responsible for hashing and persisting it before returning
        /// it to the client — never store the plain-text value.
        /// </summary>
        /// <returns>A Base64 string derived from 32 random bytes (256 bits of entropy).</returns>
        string GenerateRefreshToken();
    }
}
