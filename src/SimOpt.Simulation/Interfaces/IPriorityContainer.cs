using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Interfaces
{
    public interface IPriorityContainer
    {
        Priority Priority { get; }
    }
}