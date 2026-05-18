namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// Defines the role hierarchy used for authorization across all services.
    /// Roles are stored as integers in the database and as strings in JWT claims.
    /// </summary>
    public enum UserRole
    {
        /// <summary>Developer/owner account. Hardcoded and seeded on first deployment. Receives no notifications.</summary>
        SuperAdmin,

        /// <summary>Landlord or property manager. Created by Super Admin only.</summary>
        Admin,

        /// <summary>Renter. Created by Admin or Super Admin via the invite flow.</summary>
        Tenant,

        /// <summary>
        /// Read-only test account with Admin-level access. Write operations are intercepted
        /// at the Gateway, logged, and emailed to the Super Admin instead of being persisted.
        /// Cannot send invites.
        /// </summary>
        Tester
    }
}
