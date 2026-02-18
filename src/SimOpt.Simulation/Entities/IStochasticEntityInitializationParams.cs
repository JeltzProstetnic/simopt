using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Simulation.Entities
{
    public interface IStochasticEntityInitializationParams : IEntityInitializationParams
    {
        int? SeedIdentifier { get; }
        Point InitialPosition { get; }
    }
}