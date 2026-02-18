using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Entities
{
    public interface IResourceEntityInitializationParams : IStochasticEntityInitializationParams
    {
        IResourceManager Manager { get; }
        IEntity CurrentHolder { get; }
    }
}