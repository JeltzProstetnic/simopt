using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace Vr.WarehouseSimulator.Model
{
    public class Simulation
    {
        private MatthiasToolbox.Simulation.Engine.Model model;

        public IModel Model { get { return model; } }

        public Simulation()
        {
            this.model = new MatthiasToolbox.Simulation.Engine.Model("V-Research Warehouse Simulation", 123, 0);

        }
    }
}
