using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Basics.Exceptions
{
    public class InitializationException : ApplicationException
    {
        public InitializationException() : base() { }
        public InitializationException(string message) : base(message) { }
        public InitializationException(string message, Exception innerException) : base(message, innerException) { }
        public InitializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
