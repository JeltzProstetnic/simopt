using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Entities;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Enum;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;

namespace MatthiasToolbox.Simulation.Templates
{
    [Serializable]
    public class SimpleBuffer : Buffer<SimpleEntity>
    {
        #region ctor

        public SimpleBuffer() : base() { }

        public SimpleBuffer(IModel model,
                     QueueRule queueRule = QueueRule.FIFO,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, queueRule, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public SimpleBuffer(IModel model,
                     int seedID,
                     QueueRule queueRule = QueueRule.FIFO,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, seedID, queueRule, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public SimpleBuffer(IModel model,
                     Func<SimpleEntity> itemSelector,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, itemSelector, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        public SimpleBuffer(IModel model,
                     Func<SimpleEntity> itemSelector,
                     int seedID,
                     string id = "",
                     string name = "",
                     int maxCapacity = int.MaxValue,
                     List<string> states = null,
                     List<Tuple<string, string>> transitions = null,
                     string initialState = null,
                     Point initialPosition = null,
                     IResourceManager manager = null,
                     IEntity currentHolder = null)
            : base(model, itemSelector, seedID, id, name, maxCapacity, states, transitions, initialState, initialPosition, manager, currentHolder)
        { }

        #endregion
    }
}
