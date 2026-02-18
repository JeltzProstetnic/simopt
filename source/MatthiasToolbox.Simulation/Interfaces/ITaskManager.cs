using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Simulation.Interfaces
{
    interface ITaskManager
    {
        ///// <summary>
        ///// the internal task machine
        ///// </summary>
        //TaskMachine TaskMachine { get; }

        ///// <summary>
        ///// the internal handler for the start of a task
        ///// this is needed because starting specific tasks
        ///// may change the object's state
        ///// </summary>
        //DefaultHandler<ITaskObject, Task> TaskStartedHandler { get; }

        ///// <summary>
        ///// the event which will be raised when a task
        ///// except for the last one in a sequence is
        ///// finished.
        ///// </summary>
        //DefaultEvent<ITaskObject, Task> TaskFinishedEvent { get; }

        ///// <summary>
        ///// the event which will be raised each time a
        ///// task sequence has finished
        ///// </summary>
        //DefaultEvent<ITaskObject, Task> TaskSequenceFinishedEvent { get; }

        ///// <summary>
        ///// add a task to the task sequence
        ///// </summary>
        ///// <param name="task"></param>
        //void AddTask(Task task);

        ///// <summary>
        ///// start a task sequence
        ///// </summary>
        //void StartTaskSequence();
    }
}
