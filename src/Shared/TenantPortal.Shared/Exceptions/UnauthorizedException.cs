namespace TenantPortal.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a caller is not authenticated or presents invalid credentials.
    /// Typically maps to an HTTP 401 response.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        /// <summary>Initialises the exception with a default message.</summary>
        public UnauthorizedException() : base("User is not authenticated or credentials are invalid.") { }

        /// <summary>Initialises the exception with a specific message.</summary>
        public UnauthorizedException(string message) : base(message) { }
    }
}
