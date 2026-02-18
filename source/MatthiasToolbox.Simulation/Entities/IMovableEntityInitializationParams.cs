using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.StateMachine;
using MatthiasToolbox.Simulation.Tools;

namespace MatthiasToolbox.Simulation.Entities
{
    public interface IMovableEntityInitializationParams : ITaskMachineEntityInitializationParams
    {
        Double VMax { get; }
        Double Acceleration { get; }
        Double Deceleration { get; }
    }
}