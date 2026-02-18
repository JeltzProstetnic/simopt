using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Tools
{
    /// <summary>
    /// Finish must be called manually.
    /// TODO: force unique names using static dictionary?
    /// </summary>
    [Serializable]
    public class Task : IResettable
    {
        #region cvar

        internal TaskMachine machine;
        
        private Func<bool> startTask;
        private Action simpleStartTask;
        private Action endTask;
        private bool mustEnd;
        private bool mustStart;

        #endregion
        #region prop

        /// <summary>
        /// a unique name for this task
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// indicates if the task was already started
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// indicates if the task is currently active
        /// </summary>
        public bool Active { get { return Started && !Finished; } }

        /// <summary>
        /// indicates if the task was finished
        /// </summary>
        public bool Finished { get; private set; }

        #endregion
        #region ctor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startTask">let the start task function return true to signal that the task was not started</param>
        /// <param name="endTask">
        /// will be invoked BEFORE the task machine sets the
        /// task to be finished and starts the next one.
        /// </param>
        public Task(String name, Func<bool> startTask, Action endTask = null)
        {
            this.startTask = startTask;
            this.mustStart = startTask != null;
            this.endTask = endTask;
            this.mustEnd = endTask != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startTask"></param>
        /// <param name="endTask">
        /// will be invoked BEFORE the task machine sets the
        /// task to be finished and starts the next one.
        /// </param>
        public Task(String name, Action startTask = null, Action endTask = null)
        {
            this.simpleStartTask = startTask;
            this.startTask = SimpleStartTaskWrapper;
            this.mustStart = startTask != null;
            this.endTask = endTask;
            this.mustEnd = endTask != null;
        }

        #endregion
        #region impl

        /// <summary>
        /// the task machine will call this to start the task.
        /// If the task was already started, the method will 
        /// just return true and do nothing except for logging a warning. 
        /// </summary>
        /// <returns></returns>
        internal bool Start()
        {
            if (Started)
            {
                this.Log<SIM_WARNING>("The task was already started.", machine.Owner.Model);
                return true;
            }
            if (mustStart) Started = startTask.Invoke();
            else Started = true;
            OnStart();
            return Started;
        }

        /// <summary>
        /// call this to tell the task machine that
        /// the task is finished. If the task was already
        /// finished before the method will do nothing.
        /// </summary>
        public void Finish()
        {
            if (Finished)
            {
                this.Log<SIM_WARNING>("The task was already finished.", machine.Owner.Model);
                return;
            }
            Finished = true;
            OnFinish();
            if (mustEnd) endTask.Invoke();
            machine.OnTaskFinished(this);
        }

        #endregion
        #region virt

        /// <summary>
        /// will be called immediately after the task is set to started
        /// </summary>
        public virtual void OnStart() { }

        /// <summary>
        /// will be called directly before the task is set to finished
        /// </summary>
        public virtual void OnFinish() { }

        /// <summary>
        /// must be implemented for reproducibility issues
        /// </summary>
        public virtual void Reset() { }

        #endregion
        #region util

        private bool SimpleStartTaskWrapper()
        {
            simpleStartTask.Invoke();
            return true;
        }

        #endregion
    }
}