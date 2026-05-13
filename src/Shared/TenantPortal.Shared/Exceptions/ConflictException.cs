using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantPortal.Shared.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException() : base("Conflict occurred, item already exists") { }
        public ConflictException(string message) : base(message)
        {
        }
    }
}
