using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;

namespace SimOpt.Simulation.Entities
{
    public interface IStateMachineEntityInitializationParams : IResourceEntityInitializationParams
    {
        State InitialState { get; }
        string InitialStateName { get; }
        int? InitialStateID { get; }
    }
}