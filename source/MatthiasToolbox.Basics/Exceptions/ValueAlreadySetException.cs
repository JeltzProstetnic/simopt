using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Exceptions
{
    public class ValueAlreadySetException : Exception
    {
        public ValueAlreadySetException(string valueName) 
            : base(valueName + 
            " was already set. This value can only be set once and may not be changed afterwards.")
        { }
    }
}
