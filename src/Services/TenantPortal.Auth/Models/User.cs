using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.Models
{
    /// <summary>
    /// Represents a registered user. Covers all roles: <see cref="UserRole.SuperAdmin"/>,
    /// <see cref="UserRole.Admin"/>, and <see cref="UserRole.Tenant"/>.
    /// Records are never hard-deleted; set <see cref="IsDeleted"/> instead.
    /// </summary>
    /// <remarks>
    /// After adding <see cref="RefreshTokenHash"/> and <see cref="RefreshTokenExpiresAt"/>
    /// run: <c>dotnet ef migrations add AddRefreshTokenToUser --project TenantPortal.Auth</c>
    /// </remarks>
    public class User
    {
        /// <summary>Primary key — stable UUID assigned at creation.</summary>
        public Guid Id { get; set; }

        /// <summary>Login identifier. Unique across all users.</summary>
        public required string Email { get; set; }

        /// <summary>
        /// True once the user has submitted the post-registration profile form (name, phone, etc.).
        /// The frontend redirects to /profile-setup until this is set.
        /// </summary>
        public bool IsProfileComplete { get; set; }

        /// <summary>bcrypt hash of the user's password. Never stored in plain text.</summary>
        public required string PasswordHash { get; set; }

        /// <summary>Base32-encoded TOTP secret used to generate and verify 6-digit authenticator codes.</summary>
        public required string TotpSecret { get; set; }

        /// <summary>Role assigned at invite time. Governs what the user may access.</summary>
        public required UserRole Role { get; set; }

        /// <summary>
        /// True once the user has completed registration (set password + enrolled TOTP).
        /// Inactive users are rejected at login.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Soft-delete flag. When <c>true</c> the record is logically deleted
        /// and excluded from all queries by default.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// SHA-256 hash of the user's current refresh token.
        /// <c>null</c> when the user is logged out or has never completed login.
        /// Rotated on every successful token refresh.
        /// </summary>
        public string? RefreshTokenHash { get; set; }

        /// <summary>UTC expiry of the stored refresh token. <c>null</c> when logged out.</summary>
        public DateTime? RefreshTokenExpiresAt { get; set; }

        /// <summary>UTC timestamp when the record was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification to this record.</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>UTC timestamp when the user was soft-deleted. <c>null</c> if not deleted.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>ID of the user who sent the invitation that created this account. <c>null</c> for the Super Admin.</summary>
        public Guid? InvitedBy { get; set; }

        // ── SaaS Subscription Fields (Admin role only) ────────────────────────────

        /// <summary>
        /// Stripe Customer ID assigned at self-serve Admin registration.
        /// <c>null</c> for SuperAdmin and Tenants (they do not have subscriptions).
        /// </summary>
        public string? StripeCustomerId { get; set; }

        /// <summary>
        /// Stripe Subscription ID assigned once a checkout session is completed.
        /// <c>null</c> until the first successful subscription creation webhook fires.
        /// </summary>
        public string? StripeSubscriptionId { get; set; }

        /// <summary>
        /// Current state of the Admin's SaaS subscription.
        /// <see cref="SubscriptionStatus.None"/> for SuperAdmin, Tenants, and invited Admins (personal use).
        /// </summary>
        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.None;

        /// <summary>
        /// Maximum number of active tenants this Admin is allowed to invite.
        /// <c>null</c> means unlimited (SuperAdmin and legacy Admins from the personal-use setup).
        /// Set to 10 for the base $20/month plan; increase for higher tiers.
        /// </summary>
        public int? MaxTenants { get; set; }

        // ── Stripe Connect Fields (Admin role only) ───────────────────────────────────

        /// <summary>
        /// Stripe Express connected account ID.
        /// Set when the admin initiates bank-account onboarding via Stripe Connect.
        /// <c>null</c> until onboarding begins.
        /// </summary>
        public string? StripeConnectedAccountId { get; set; }

        /// <summary>
        /// Cached value of <c>charges_enabled</c> from Stripe — updated by the
        /// <c>account.updated</c> Connect webhook. True once the admin has completed
        /// onboarding and can receive tenant payments.
        /// </summary>
        public bool StripeConnectChargesEnabled { get; set; } = false;

        // ── Hierarchy is tracked relationally, not as denormalised lists ─────────────
        //
        // "Who did this user invite?"   → query Users WHERE InvitedBy = this.Id
        // "Pending invites not yet accepted?" → query InviteTokens WHERE CreatedBy = this.Id AND !Used
        //
        // IsActive = false  → placeholder created when an invite is sent; still unregistered
        // IsActive = true   → fully registered account
    }
}
