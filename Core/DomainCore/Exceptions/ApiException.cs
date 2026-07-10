using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCore.Exceptions
{
    public class ApiException: Exception
    {

        public int StatusCode { get; }
        public object? DataObject { get; }
        public ApiException(
            string message,
            int statusCode = 400,
            object? data = null)
            : base(message)
        {
            StatusCode = statusCode;
            DataObject = data;
        }

    }
}
