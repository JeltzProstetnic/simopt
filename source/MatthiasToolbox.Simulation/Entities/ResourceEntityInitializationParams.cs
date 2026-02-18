using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Simulation.Entities
{
    [Serializable]
    public class ResourceEntityInitializationParams : StochasticEntityInitializationParams, IResourceEntityInitializationParams
    {
        public ResourceEntityInitializationParams(string id = "",
                                                    string name = "",
                                                    int? seedID = null,
                                                    Point initialPosition = null,
                                                    IResourceManager manager = null,
                                                    IEntity currentHolder = null)
            : base(id, name, seedID, initialPosition)
        {
            this.Manager = manager;
            this.CurrentHolder = currentHolder;
        }

        #region IResourceEntityInitializationParams

        public IResourceManager Manager { get; set; }

        public IEntity CurrentHolder { get; set; }

        #endregion
    }
}
