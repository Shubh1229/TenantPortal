namespace TenantPortal.Shared.Exceptions
{
    /// <summary>
    /// Thrown when an authenticated caller does not have permission to perform the requested action.
    /// Typically maps to an HTTP 403 response.
    /// </summary>
    public class ForbiddenException : Exception
    {
        /// <summary>Initialises the exception with a default message.</summary>
        public ForbiddenException() : base("You do not have permission to perform this action.") { }

        /// <summary>Initialises the exception with a specific message.</summary>
        public ForbiddenException(string message) : base(message) { }
    }
}
