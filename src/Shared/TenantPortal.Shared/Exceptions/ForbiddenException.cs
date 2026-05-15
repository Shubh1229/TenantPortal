using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantPortal.Shared.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("User is forbidden to access this") { }
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
