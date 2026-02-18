using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Tools
{
    public class SimulationEventArgs : EventArgs
    {
        public IModel Model { get; private set; }

        public SimulationEventArgs(IModel model)
            : base()
        {
            this.Model = model;
        }
    }
}
