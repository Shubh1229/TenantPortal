namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for step 1 of login — email and password validation.</summary>
    public class LoginRequestDTO
    {
        /// <summary>The user's registered email address.</summary>
        public required string Email { get; set; }

        /// <summary>The user's plain-text password (transmitted only over TLS; never logged).</summary>
        public required string Password { get; set; }
    }
}
