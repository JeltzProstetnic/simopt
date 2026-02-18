using System;

namespace SimOpt.Simulation.Exceptions
{
    public class CausalityException : Exception
    {
        public CausalityException() : base() { }

        public CausalityException(String text) : base(text) { }

        public CausalityException(String text, Exception inner) : base(text, inner) { }
    }
}
