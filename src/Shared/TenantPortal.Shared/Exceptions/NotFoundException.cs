namespace TenantPortal.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a requested resource does not exist or is not visible to the caller.
    /// Typically maps to an HTTP 404 response.
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>Initialises the exception with a default message.</summary>
        public NotFoundException() : base("The requested item could not be found.") { }

        /// <summary>Initialises the exception with a specific message.</summary>
        public NotFoundException(string message) : base(message) { }
    }
}
