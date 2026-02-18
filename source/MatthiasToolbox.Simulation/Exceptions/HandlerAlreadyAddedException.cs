using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using System.Runtime.Serialization;

namespace MatthiasToolbox.Simulation.Exceptions
{
    public class HandlerAlreadyAddedException : Exception
    {
        public HandlerAlreadyAddedException() : base() { }

        public HandlerAlreadyAddedException(object handler, string eventName)
            : base("The handler " + handler.GetType().FullName +
                " was already added to the event " + eventName + ".") { }

        public HandlerAlreadyAddedException(String text) : base(text) { }

        public HandlerAlreadyAddedException(String text, Exception inner) : base(text, inner) { }

        public HandlerAlreadyAddedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
