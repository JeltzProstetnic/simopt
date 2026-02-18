using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Exceptions;

namespace SimOpt.Simulation.Exceptions
{
    public class ModelAlreadySetException : ValueAlreadySetException
    {
        public ModelAlreadySetException() 
            : base("Model") 
        { }
    }
}
