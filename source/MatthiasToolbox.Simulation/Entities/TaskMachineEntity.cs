using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;
using MatthiasToolbox.Basics.Datastructures.Geometry;
using MatthiasToolbox.Simulation.Interfaces;
using MatthiasToolbox.Simulation.Tools;
using MatthiasToolbox.Logging;

namespace MatthiasToolbox.Simulation.Entities
{
    /// <summary>
    /// The task machine entity is based upon the state machine entity and allows the configuration of a task sequence for the object
    /// to process using a TaskMachine instance from the tools namespace. This can be used to let the simulation object process a 
    /// sequence of relatively simple steps and is helpful if the same sequence is required to run repeatedly. However, the modeller 
    /// should be aware of the fact that this may defy the purpose of discrete simulation to some extent if it is overused. The 
    /// feature is still a subject of discussion among experts.
    /// </summary>
    [Serializable]
    public class TaskMachineEntity : StateMachineEntity
    {
        #region over

        public override void Reset()
        {
            base.Reset();
            taskMachine.Reset();
        }

        #endregion
        #region cvar

        private TaskMachine taskMachine;
        private bool taskMachineInitialized = false;

        #endregion
        #region prop

        /// <summary>
        /// the internal task machine of the entity
        /// </summary>
        public TaskMachine TaskMachine { get { return taskMachine; } }

        /// <summary>
        /// if a task is currently in progress, this 
        /// is it. Otherwise this is null
        /// </summary>
        public Task CurrentTask { get { return taskMachine.CurrentTask; } }

        /// <summary>
        /// the current task sequence. If the task machine is currently
        /// working on the tasks, the first returned task is equal to
        /// the CurrentTask. It will be removed after completion only.
        /// </summary>
        public IEnumerable<Task> OpenTasks { get { return taskMachine.Tasks; } }
        
        /// <summary>
        /// if false the TaskMachine is busy working on a task sequence
        /// </summary>
        public bool Idle { get { return taskMachine.Idle; } }

        /// <summary>
        /// if true the TaskMachine is busy working on a task sequence
        /// </summary>
        public bool Busy { get { return taskMachine.Busy; } }

        /// <summary>
        /// returns false if no open tasks remain in this task machine
        /// </summary>
        public bool HasTasks { get { return taskMachine.HasTasks; } }

        /// <summary>
        /// returns true if no open tasks remain in this task machine
        /// </summary>
        public bool Empty { get { return taskMachine.Empty; } }

        #endregion
        #region ctor

        public TaskMachineEntity() : base() { }

        public TaskMachineEntity(IModel model, 
                                 string id = "", 
                                 string name = "",
                                 Action<Task> notifyTaskFinished = null,
                                 Action<Task> notifyTaskStarted = null,
                                 Action<Task> notifyTaskStartFailed = null,
                                 List<string> states = null, 
                                 List<Tuple<string, string>> transitions = null,
                                 string initialState = "", 
                                 Point initialPosition = null, 
                                 IResourceManager manager = null, 
                                 IEntity currentHolder = null) 
            : base(model, id, name, states, transitions, initialState, initialPosition, manager, currentHolder) 
        {
            InitializeTaskMachine(notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed);
        }

        public TaskMachineEntity(IModel model,
                                 int seedID,
                                 string id = "",
                                 string name = "",
                                 Action<Task> notifyTaskFinished = null,
                                 Action<Task> notifyTaskStarted = null,
                                 Action<Task> notifyTaskStartFailed = null, 
                                 List<string> states = null,
                                 List<Tuple<string, string>> transitions = null,
                                 string initialState = "",
                                 Point initialPosition = null,
                                 IResourceManager manager = null,
                                 IEntity currentHolder = null)
            : base(model, seedID, id, name, states, transitions, initialState, initialPosition, manager, currentHolder)
        {
            InitializeTaskMachine(notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed);
        }

        #endregion
        #region init

        public override void Initialize(IModel model, IEntityInitializationParams parameters)
        {
            ITaskMachineEntityInitializationParams initParams = parameters as ITaskMachineEntityInitializationParams;
            if (initParams == null) throw new ArgumentException("You must use an ITaskMachineEntityInitializationParams instance to initialize a TaskMachineEntity.");
            Initialize(model, initParams);
        }

        public void Initialize(IModel model, ITaskMachineEntityInitializationParams parameters)
        {
            base.Initialize(model, parameters);
#if DEBUG
            if (taskMachineInitialized) this.Log<WARN>("The task machine was initialized more than once.");
#endif
            taskMachine = new TaskMachine(EntityName + ".TaskMachine", this, OnTaskSequenceFinished, parameters.NotifyTaskFinished, parameters.NotifyTaskStarted, parameters.NotifyTaskStartFailed);
            taskMachineInitialized = true;
        }

        public void InitializeTaskMachine(Action<Task> notifyTaskFinished = null,
                                          Action<Task> notifyTaskStarted = null, 
                                          Action<Task> notifyTaskStartFailed = null)
        {
#if DEBUG
            if (taskMachineInitialized) this.Log<WARN>("The task machine was initialized more than once.");
#endif
            taskMachine = new TaskMachine(EntityName + ".TaskMachine", this, OnTaskSequenceFinished, notifyTaskFinished, notifyTaskStarted, notifyTaskStartFailed);
            taskMachineInitialized = true;
        }

        #endregion
        #region virt

        public virtual void OnTaskSequenceFinished(List<Task> finishedTasks) { }

        #endregion
    }
}