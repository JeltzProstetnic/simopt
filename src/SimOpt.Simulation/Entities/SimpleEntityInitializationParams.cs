using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Tools;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Simulation.Entities
{
    [Serializable]
    public class SimpleEntityInitializationParams : EntityInitializationParams, ISimpleEntityInitializationParams
    {
        public SimpleEntityInitializationParams(string id = "", string name = "", Point initialPosition = null) 
            :base(id, name)
        {
            this.InitialPosition = initialPosition;
        }

        #region ISimpleEntityInitializationParams

        public Point InitialPosition { get; set; }

        #endregion
    }
}