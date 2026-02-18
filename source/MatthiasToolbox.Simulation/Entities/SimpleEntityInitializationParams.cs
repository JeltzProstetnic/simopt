using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Simulation.Entities
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