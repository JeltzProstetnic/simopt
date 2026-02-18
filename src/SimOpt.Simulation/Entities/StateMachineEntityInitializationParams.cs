using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Entities
{
    [Serializable]
    public class StateMachineEntityInitializationParams : ResourceEntityInitializationParams, IStateMachineEntityInitializationParams
    {
        public StateMachineEntityInitializationParams(string id = "", 
                                                      string name = "", 
                                                      int? seedID = null, 
                                                      Point initialPosition = null, 
                                                      IResourceManager manager = null, 
                                                      IEntity currentHolder = null,
                                                      State initialState = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder)
        {
            this.InitialState = initialState;
        }

        public StateMachineEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,
                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      string initialState = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder)
        {
            this.InitialStateName = initialState;
        }

        public StateMachineEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,
                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      int? initialState = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder)
        {
            this.InitialStateID = initialState;
        }

        #region IStateMachineEntityInitializationParams

        public State InitialState { get; set; }

        public string InitialStateName { get; set; }

        public int? InitialStateID { get; set; }

        #endregion
    }
}
