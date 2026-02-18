using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.StateMachine;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Simulation.Tools;

namespace MatthiasToolbox.Simulation.Entities
{
    [Serializable]
    public class TaskMachineEntityInitializationParams : StateMachineEntityInitializationParams, ITaskMachineEntityInitializationParams
    {
        public TaskMachineEntityInitializationParams(string id = "", 
                                                      string name = "", 
                                                      int? seedID = null, 
                                                      Point initialPosition = null, 
                                                      IResourceManager manager = null, 
                                                      IEntity currentHolder = null,
                                                      State initialState = null,
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            this.NotifyTaskFinished = notifyTaskFinished;
            this.NotifyTaskStarted = notifyTaskStarted;
            this.NotifyTaskStartFailed = notifyTaskStartFailed;
        }

        public TaskMachineEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,
                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      string initialState = null, 
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            this.NotifyTaskFinished = notifyTaskFinished;
            this.NotifyTaskStarted = notifyTaskStarted;
            this.NotifyTaskStartFailed = notifyTaskStartFailed;
        }

        public TaskMachineEntityInitializationParams(string id = "",
                                                      string name = "",
                                                      int? seedID = null,
                                                      Point initialPosition = null,
                                                      IResourceManager manager = null,
                                                      IEntity currentHolder = null,
                                                      int? initialState = null, 
                                                      Action<Task> notifyTaskFinished = null,
                                                      Action<Task> notifyTaskStarted = null,
                                                      Action<Task> notifyTaskStartFailed = null)
            : base(id, name, seedID, initialPosition, manager, currentHolder, initialState)
        {
            this.NotifyTaskFinished = notifyTaskFinished;
            this.NotifyTaskStarted = notifyTaskStarted;
            this.NotifyTaskStartFailed = notifyTaskStartFailed;
        }

        #region ITaskMachineEntityInitializationParams

        public Action<Task> NotifyTaskFinished { get; set; }

        public Action<Task> NotifyTaskStarted { get; set; }

        public Action<Task> NotifyTaskStartFailed { get; set; }

        #endregion
    }
}
