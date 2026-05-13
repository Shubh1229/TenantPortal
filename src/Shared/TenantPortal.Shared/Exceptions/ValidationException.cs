using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TenantPortal.Shared.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException() : base("Validation failed") { }
        public ValidationException(string message) : base(message)
        {
        }
    }
}
