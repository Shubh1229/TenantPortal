namespace TenantPortal.Shared.Exceptions
{
    /// <summary>
    /// Thrown when input data fails validation rules before reaching the database.
    /// Typically maps to an HTTP 400 response.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>Initialises the exception with a default message.</summary>
        public ValidationException() : base("One or more validation errors occurred.") { }

        /// <summary>Initialises the exception with a specific message.</summary>
        public ValidationException(string message) : base(message) { }
    }
}
