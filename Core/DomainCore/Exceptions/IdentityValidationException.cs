using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCore.Exceptions
{
    public class IdentityValidationException: Exception
    {
        public IEnumerable<string> Errors { get; }

        public IdentityValidationException(IEnumerable<string> errors)
            : base("Error de validación en Identity")
        {
            Errors = errors;
        }

    }
}
