using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Basics.Datastructures.StateMachine;
using SimOpt.Simulation.Tools;

namespace SimOpt.Simulation.Entities
{
    public interface ITaskMachineEntityInitializationParams : IStateMachineEntityInitializationParams
    {
        Action<Task> NotifyTaskFinished { get; set; }
        Action<Task> NotifyTaskStarted { get; set; }
        Action<Task> NotifyTaskStartFailed { get; set; }
    }
}