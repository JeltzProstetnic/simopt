using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.StateMachine;
using MatthiasToolbox.Simulation.Tools;

namespace MatthiasToolbox.Simulation.Entities
{
    public interface ITaskMachineEntityInitializationParams : IStateMachineEntityInitializationParams
    {
        Action<Task> NotifyTaskFinished { get; set; }
        Action<Task> NotifyTaskStarted { get; set; }
        Action<Task> NotifyTaskStartFailed { get; set; }
    }
}