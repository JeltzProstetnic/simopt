using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Basics.Exceptions
{
    public class InfiniteRecursionException : Exception
    {
        public InfiniteRecursionException() : base() { }
        public InfiniteRecursionException(string message) : base(message) { }
        public InfiniteRecursionException(string message, Exception innerException) : base(message, innerException) { }
        public InfiniteRecursionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
