using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Mathematics.Exceptions
{
    public class ClassInitializationException : ApplicationException
    {
        public ClassInitializationException() : base() { }
        public ClassInitializationException(string message) : base(message) { }
        public ClassInitializationException(string message, Exception innerException) : base(message, innerException) { }
        public ClassInitializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
