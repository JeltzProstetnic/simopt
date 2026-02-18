using System;

namespace SimOpt.Mathematics.Exceptions
{
    public class ClassInitializationException : ApplicationException
    {
        public ClassInitializationException() : base() { }
        public ClassInitializationException(string message) : base(message) { }
        public ClassInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
