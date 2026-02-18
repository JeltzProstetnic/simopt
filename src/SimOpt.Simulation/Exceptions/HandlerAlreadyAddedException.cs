using System;

namespace SimOpt.Simulation.Exceptions
{
    public class HandlerAlreadyAddedException : Exception
    {
        public HandlerAlreadyAddedException() : base() { }

        public HandlerAlreadyAddedException(object handler, string eventName)
            : base("The handler " + handler.GetType().FullName +
                " was already added to the event " + eventName + ".") { }

        public HandlerAlreadyAddedException(String text) : base(text) { }

        public HandlerAlreadyAddedException(String text, Exception inner) : base(text, inner) { }
    }
}
