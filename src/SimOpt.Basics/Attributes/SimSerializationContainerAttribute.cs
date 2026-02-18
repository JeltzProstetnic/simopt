using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Basics.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class SimSerializationContainer : System.Attribute
    {
        public SimSerializationContainer()
        {
        }
    }


}
