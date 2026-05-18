namespace TenantPortal.Auth.DTOs
{
    /// <summary>Request body for completing registration using an invite token.</summary>
    public class RegisterRequestDTO
    {
        /// <summary>The desired password. Should meet minimum complexity requirements enforced by the frontend.</summary>
        public required string Password { get; set; }

        /// <summary>Must match <see cref="Password"/>. Validated client-side before submission.</summary>
        public required string ConfirmPassword { get; set; }

        /// <summary>Plain-text invite token from the registration link sent by email.</summary>
        public required string InviteToken { get; set; }
    }
}
