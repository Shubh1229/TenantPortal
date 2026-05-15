namespace TenantPortal.Shared.Constants
{
    public static class AppConstants
    {
        public static class Claims
        {
            public const string UserId = "uid";
            public const string UserRole = "role";
            public const string Email = "email";
        }

        public static class Policies
        {
            public const string RequireSuperAdmin = "RequireSuperAdmin";
            public const string RequireAdmin = "RequireAdmin";
            public const string RequireTenant = "RequireTenant";
        }

        public static class Headers
        {
            public const string CorrelationId = "X-Correlation-ID";
        }
    }
}