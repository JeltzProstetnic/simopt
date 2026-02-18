using System;

namespace SimOpt.Basics.Exceptions
{
    public class InitializationException : ApplicationException
    {
        public InitializationException() : base() { }
        public InitializationException(string message) : base(message) { }
        public InitializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
