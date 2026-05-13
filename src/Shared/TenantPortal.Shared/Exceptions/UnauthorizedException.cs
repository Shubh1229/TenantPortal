
namespace TenantPortal.Shared.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base("User is not authorized to access this") { }
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}