using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Basics.Datastructures.Geometry;

namespace SimOpt.Simulation.Entities
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
