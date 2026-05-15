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
        Tenant
    }
}
