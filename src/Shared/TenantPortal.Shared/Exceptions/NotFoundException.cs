
namespace TenantPortal.Shared.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("Could not find item") { }
        public NotFoundException(string message) : base(message) { }
    }
}
