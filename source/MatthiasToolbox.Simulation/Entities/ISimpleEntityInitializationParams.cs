using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Simulation.Entities
{
    public interface ISimpleEntityInitializationParams : IEntityInitializationParams
    {
        Point InitialPosition { get; }
    }
}
