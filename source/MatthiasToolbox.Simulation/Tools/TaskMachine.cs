using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Simulation.Engine;

namespace MatthiasToolbox.Simulation.Tools
{
    /// <summary>
    /// The task machine can be filled with a task sequence. On start
    /// the machine tries to start one task after the other. If starting
    /// a task fails, the machine will be paused until Continue is 
    /// called manually.
    /// 
    /// TODO: implement StateMachine, force unique name, allow user to 
    /// stop, clear and change sequence (use priority dictionary for tasks?)
    /// </summary>
    /// 
    [Serializable]
    public class TaskMachine : IResettable
    {
        #region cvar

        private List<Task> tasks;
        private List<Task> finishedTasks;
        private List<IResettable> resettables;

        #endregion
        #region prop

        #region main

        /// <summary>
        /// a unique name for this task machine
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// the owner entity of this task machine.
        /// </summary>
        public IEntity Owner { get; private set; }

        /// <summary>
        /// if a task is currently in progress, this 
        /// is it. Otherwise this is null
        /// </summary>
        public Task CurrentTask { get; private set; }

        /// <summary>
        /// the current task sequence. If the task machine is currently
        /// working on the tasks, the first returned task is equal to
        /// the CurrentTask. It will be removed after completion only.
        /// </summary>
        public IEnumerable<Task> Tasks { get { foreach (Task t in tasks) yield return t; } }

        #endregion
        #region flag

        /// <summary>
        /// if false the TaskMachine is busy working on a task sequence
        /// </summary>
        public bool Idle { get { return CurrentTask == null; } }

        /// <summary>
        /// if true the TaskMachine is busy working on a task sequence
        /// </summary>
        public bool Busy { get { return CurrentTask != null; } }

        /// <summary>
        /// returns true if a task sequence was interrupted somehow
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// returns false if no open tasks remain in this task machine
        /// </summary>
        public bool HasTasks { get { return tasks.Count != 0; } }

        /// <summary>
        /// returns true if no open tasks remain in this task machine
        /// </summary>
        public bool Empty { get { return tasks.Count == 0; } }

        #endregion
        #region hand

        public Action<List<Task>> NotifySequenceFinished { get; set; }
        public Action<Task> NotifyTaskFinished { get; set; }
        public Action<Task> NotifyTaskStarted { get; set; }
        public Action<Task> NotifyTaskStartFailed { get; set; }

        private bool UseNotifySequenceFinished { get { return NotifySequenceFinished != null; } }
        private bool UseNotifyTaskFinished { get { return NotifyTaskFinished != null; } }
        private bool UseNotifyTaskStarted { get { return NotifyTaskStarted != null; } }
        private bool UseNotifyTaskStartFailed { get { return NotifyTaskStartFailed != null; } }

        #endregion

        #endregion
        #region ctor

        private TaskMachine(string name)
        {
            Name = name;
            tasks = new List<Task>();
            finishedTasks = new List<Task>();
            resettables = new List<IResettable>();
        }

        /// <summary>
        /// main ctor
        /// </summary>
        /// <param name="owner"></param>
        public TaskMachine(string name, IEntity owner, 
                           Action<List<Task>> notifySequenceFinished = null, 
                           Action<Task> notifyTaskFinished = null,
                           Action<Task> notifyTaskStarted = null, 
                           Action<Task> notifyTaskStartFailed = null)
            : this(name)
        {
            Owner = owner;
            NotifySequenceFinished = notifySequenceFinished;
            NotifyTaskFinished = notifyTaskFinished;
            NotifyTaskStarted = notifyTaskStarted;
            NotifyTaskStartFailed = notifyTaskStartFailed;
        }

        #endregion
        #region impl

        /// <summary>
        /// add a task to the task sequence
        /// </summary>
        /// <param name="task"></param>
        /// <returns>success flag</returns>
        public bool AddTask(Task task) 
        {
            if (Busy)
            {
                this.Log<SIM_WARNING>("Unable to add task " + task.Name + ". The task sequence is already running.", Owner.Model);
                return false;
            }
            task.machine = this;
            tasks.Add(task);
            if (task is IResettable) resettables.Add(task as IResettable);
            return true;
        }

        /// <summary>
        /// Continue a task sequence.
        /// </summary>
        /// <returns></returns>
        public bool ContinueTaskSequence() { return StartTaskSequence(); }

        /// <summary>
        /// Start a task sequence or restart
        /// a task sequence which failed to start earlier
        /// </summary>
        public bool StartTaskSequence()
        {
            if (Busy) // cannot start
            {
                this.Log<SIM_WARNING>("This TaskMachine is already busy working on a task sequence.", Owner.Model);
                return false;
            }
            else if (Empty) // cannot start
            {
                this.Log<SIM_WARNING>("This TaskMachine currently contains no tasks.", Owner.Model);
                return false;
            }

            // get first task and start
            CurrentTask = tasks.First();
            if (!CurrentTask.Start())
            {
                Paused = true;
                this.Log<SIM_WARNING>("Unable to start task " + CurrentTask.Name + " for owner " + Owner.EntityName + ".", Owner.Model);
                CurrentTask = null;
                if (UseNotifyTaskStartFailed) NotifyTaskStartFailed(tasks.First());
                return false;
            }
            Paused = false;
            
            if (UseNotifyTaskStarted) NotifyTaskStarted.Invoke(CurrentTask);

            return true;
        }

        internal void OnTaskFinished(Task finishedTask)
        {
            if (finishedTask != CurrentTask)
            {
                throw new InvalidOperationException("The finish method of task " + 
                    finishedTask.Name + " was called but " + CurrentTask.Name + 
                    " was the current task. You can not finish tasks before they were started!");
            }

            // update lists
            tasks.Remove(CurrentTask);
            finishedTasks.Add(CurrentTask);

            if (UseNotifyTaskFinished) NotifyTaskFinished.Invoke(CurrentTask);

            CurrentTask = null;

            if (Empty) // the last task in the sequence was just finished
            {
                if (UseNotifySequenceFinished) NotifySequenceFinished.Invoke(finishedTasks);
            }
            else
            {
                ContinueTaskSequence();
            }
        }

        #endregion
        #region rset

        /// <summary>
        /// reset the task machine
        /// CAUTION: notification delegates will not be reset
        /// </summary>
        public void Reset()
        {
            foreach (IResettable r in resettables) r.Reset();
            resettables.Clear();
            tasks.Clear();
            finishedTasks.Clear();
            CurrentTask = null;
            Paused = false;
        }

        #endregion
    }
}