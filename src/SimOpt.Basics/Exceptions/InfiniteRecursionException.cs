using System;

namespace SimOpt.Basics.Exceptions
{
    public class InfiniteRecursionException : Exception
    {
        public InfiniteRecursionException() : base() { }
        public InfiniteRecursionException(string message) : base(message) { }
        public InfiniteRecursionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
