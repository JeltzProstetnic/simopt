using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;

namespace MatthiasToolbox.SQSSModel
{
    [Serializable]
    public class ProductGenerator
    {
        Func<SimpleEntity> GetFuncOfSimpleEntity()
        {
            return () => new SimpleEntity(); 
        }

    }
}
