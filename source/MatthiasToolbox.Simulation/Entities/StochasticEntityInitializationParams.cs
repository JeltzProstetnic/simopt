using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Simulation.Entities
{
    [Serializable]
    public class StochasticEntityInitializationParams : EntityInitializationParams, IStochasticEntityInitializationParams
    {
        public StochasticEntityInitializationParams(string id = "", string name = "", int? seedID = null, Point initialPosition = null)
            : base(id, name)
        {
            this.SeedIdentifier = seedID;
            this.InitialPosition = initialPosition;
        }

        #region IStochasticEntityInitializationParams

        public int? SeedIdentifier { get; set; }

        public Point InitialPosition { get; set; }

        #endregion
    }
}
