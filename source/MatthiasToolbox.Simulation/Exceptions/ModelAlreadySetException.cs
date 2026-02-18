using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Exceptions;

namespace MatthiasToolbox.Simulation.Exceptions
{
    public class ModelAlreadySetException : ValueAlreadySetException
    {
        public ModelAlreadySetException() 
            : base("Model") 
        { }
    }
}
