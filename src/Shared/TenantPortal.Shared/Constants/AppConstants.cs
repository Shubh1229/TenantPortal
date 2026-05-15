namespace TenantPortal.Shared.Constants
{
    /// <summary>
    /// Application-wide constants shared across all microservices.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>JWT claim type names embedded in every access token.</summary>
        public static class Claims
        {
            /// <summary>Claim containing the user's UUID primary key.</summary>
            public const string UserId = "uid";

            /// <summary>Claim containing the user's <see cref="TenantPortal.Shared.Enums.UserRole"/> as a string.</summary>
            public const string UserRole = "role";

            /// <summary>Claim containing the user's email address.</summary>
            public const string Email = "email";
        }

        /// <summary>Authorization policy names registered in each service's DI container.</summary>
        public static class Policies
        {
            /// <summary>Restricts access to Super Admin users only.</summary>
            public const string RequireSuperAdmin = "RequireSuperAdmin";

            /// <summary>Allows Admin and Super Admin users.</summary>
            public const string RequireAdmin = "RequireAdmin";

            /// <summary>Allows Tenant, Admin, and Super Admin users — effectively any authenticated user.</summary>
            public const string RequireTenant = "RequireTenant";
        }

        /// <summary>HTTP header names used for request tracing across services.</summary>
        public static class Headers
        {
            /// <summary>
            /// UUID assigned by the Gateway to every inbound request and propagated through
            /// all downstream gRPC calls. Used to correlate log entries for a single request.
            /// </summary>
            public const string CorrelationId = "X-Correlation-ID";
        }
    }
}
