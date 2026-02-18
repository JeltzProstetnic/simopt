using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Entities
{
    public interface IResourceEntityInitializationParams : IStochasticEntityInitializationParams
    {
        IResourceManager Manager { get; }
        IEntity CurrentHolder { get; }
    }
}