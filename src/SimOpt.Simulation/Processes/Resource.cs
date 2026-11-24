using System;
using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Events;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126: a resource with a capacity, the counterpart of SimPy's <c>Resource</c> (machines,
    /// operators, docks). A process yields <see cref="Request"/>, which completes when a unit is
    /// granted, first come, first served, and calls <see cref="Release"/> when it is done.
    /// A granted request resumes its process by a zero-delay event at the instant of the grant.
    /// </summary>
    [Serializable]
    public sealed class Resource : SimpleEntity
    {
        #region cvar

        private readonly int capacity;
        private int inUse;
        private readonly Queue<Action> waiting = new Queue<Action>();
        private readonly BinaryEvent<Resource, int> inUseChangedEvent;

        #endregion
        #region ctor

        public Resource(IModel model, int capacity = 1, string id = "", string name = "")
            : base(model, id, name)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be at least 1.");
            this.capacity = capacity;
            inUseChangedEvent = new BinaryEvent<Resource, int>(EntityName + ".InUseChanged");
        }

        #endregion
        #region prop

        public int Capacity { get { return capacity; } }

        /// <summary>Units granted and not yet released.</summary>
        public int InUse { get { return inUse; } }

        /// <summary>Requests waiting for a unit.</summary>
        public int Waiting { get { return waiting.Count; } }

        /// <summary>Raised with the new number of units in use whenever it changes.</summary>
        public BinaryEvent<Resource, int> InUseChangedEvent { get { return inUseChangedEvent; } }

        #endregion
        #region impl

        /// <summary>A wait that completes when a unit has been granted to the waiting process.</summary>
        public Wait Request() => Wait.From(Acquire);

        private void Acquire(Action granted)
        {
            if (inUse < capacity && waiting.Count == 0) Grant(granted);
            else waiting.Enqueue(granted);
        }

        /// <summary>Returns one unit; the next waiting request, if any, gets it at once.</summary>
        public void Release()
        {
            if (inUse == 0) throw new InvalidOperationException("Resource '" + EntityName + "' has no unit in use to release.");
            inUse--;
            inUseChangedEvent.Raise(this, inUse);
            if (waiting.Count > 0) Grant(waiting.Dequeue());
        }

        private void Grant(Action granted)
        {
            inUse++;
            inUseChangedEvent.Raise(this, inUse);
            Model.Schedule(0d, granted);
        }

        public override void Reset()
        {
            base.Reset();
            inUse = 0;
            waiting.Clear();
        }

        #endregion
    }
}
