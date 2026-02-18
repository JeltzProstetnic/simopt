using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Simulation.Engine
{
    public interface IEntityInitializationParams
    {
        string ID { get; }
        string EntityName { get; }
    }
}