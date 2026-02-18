using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Simulation.Tools;

namespace SimOpt.Simulation.Entities
{
    public interface IMovableEntityInitializationParams : ITaskMachineEntityInitializationParams
    {
        Double VMax { get; }
        Double Acceleration { get; }
        Double Deceleration { get; }
    }
}