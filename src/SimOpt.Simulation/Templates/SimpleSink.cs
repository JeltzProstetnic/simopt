using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;

namespace SimOpt.Simulation.Templates
{
    [Serializable]
    public class SimpleSink : Sink<SimpleEntity>
    {
        #region ctor

        public SimpleSink() : base() { }

        public SimpleSink(IModel model, string id = "", string name = "", bool log = false, Point initialPosition = null)
            : base(model, id, name, log, initialPosition)
        { }

        #endregion
    }
}
