using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Tools
{
    [Serializable]
    public class EntityInitializationParams : IEntityInitializationParams
    {
        public EntityInitializationParams(string id = "", string name = "")
        {
            this.ID = id;
            this.EntityName = name;
        }

        #region IEntityInitializationParams

        public string ID { get; set; }

        public string EntityName { get; set; }

        #endregion
    }
}
