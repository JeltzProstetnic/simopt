using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Interfaces;

namespace SimOpt.Simulation.Interfaces
{
    public interface ISaveable : IIdentifiable
    {
        void Save(ModelState state);
        bool Load(ModelState state);
    }
}
