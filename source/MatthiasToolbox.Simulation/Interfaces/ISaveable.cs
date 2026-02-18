using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Simulation.Interfaces
{
    public interface ISaveable : IIdentifiable
    {
        void Save(ModelState state);
        bool Load(ModelState state);
    }
}
