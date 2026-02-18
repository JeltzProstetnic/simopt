using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Tools
{
    [Serializable]
    public struct HandlerInfo
    {
        public Priority Priority { get; set; }
        public String MethodName { get; set; }
        public String ItemID { get; set; }
        public bool IsToDetatch { get; set; }

        public HandlerInfo(Priority priority, string methodName, string entityID) : this()
        {
            Priority = priority;
            MethodName = methodName;
            ItemID = entityID;
            IsToDetatch = false;
        }
    }
}