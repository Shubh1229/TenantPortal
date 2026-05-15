namespace TenantPortal.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a create or update operation would violate a uniqueness constraint.
    /// Typically maps to an HTTP 409 response.
    /// </summary>
    public class ConflictException : Exception
    {
        /// <summary>Initialises the exception with a default message.</summary>
        public ConflictException() : base("A conflict occurred — the item already exists.") { }

        /// <summary>Initialises the exception with a specific message.</summary>
        public ConflictException(string message) : base(message) { }
    }
}
