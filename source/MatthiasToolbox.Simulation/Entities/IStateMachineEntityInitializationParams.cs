using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.StateMachine;

namespace MatthiasToolbox.Simulation.Entities
{
    public interface IStateMachineEntityInitializationParams : IResourceEntityInitializationParams
    {
        State InitialState { get; }
        string InitialStateName { get; }
        int? InitialStateID { get; }
    }
}