using System;
namespace MatthiasToolbox.Simulation.Enum
{

    [Serializable]
    public enum ExecutionState
    {
        Undefined, Stopped, Paused, Interrupted, TimeElapsed, InBreakPoint, Running
    }
}