using System;
using SimOpt.Simulation.Entities;

namespace SimOpt.Examples.SQSS
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
