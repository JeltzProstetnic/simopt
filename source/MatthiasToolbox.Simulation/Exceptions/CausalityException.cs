using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Simulation.Exceptions
{
    public class CausalityException : Exception
    {
        public CausalityException() : base() { }

        public CausalityException(String text) : base(text) { }

        public CausalityException(String text, Exception inner) : base(text, inner) { }

        public CausalityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
