using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Engine;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class SimpleDelay : Delay<SimpleEntity>
    {
        #region ctor

        public SimpleDelay() { }

        public SimpleDelay(IModel model,
                     IDistribution<double> interval,
                     string id = "",
                     string name = "",
                     SimpleEntity initialItem = null,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, interval, id, name, initialItem, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public SimpleDelay(IModel model,
                     IDistribution<double> interval,
                     int seedID,
                     string id = "",
                     string name = "",
                     SimpleEntity initialItem = null,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, interval, seedID, id, name, initialItem, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }
}
