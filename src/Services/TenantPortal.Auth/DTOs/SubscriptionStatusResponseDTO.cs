using TenantPortal.Shared.Enums;

namespace TenantPortal.Auth.DTOs
{
    /// <summary>
    /// Describes the current state of an Admin's SaaS subscription and tenant usage.
    /// </summary>
    public class SubscriptionStatusResponseDTO
    {
        /// <summary>Current lifecycle state of the subscription (Active, PastDue, Canceled, etc.).</summary>
        public SubscriptionStatus Status { get; set; }

        /// <summary>Whether the account is currently permitted to log in and access the platform.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Maximum number of active tenants allowed under this subscription.
        /// <c>null</c> means the account has no limit (SuperAdmin or legacy personal-use admin).
        /// </summary>
        public int? MaxTenants { get; set; }

        /// <summary>Number of active tenants currently under this Admin's account.</summary>
        public int CurrentTenantCount { get; set; }
    }
}
