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
        private readonly LinkedList<RequestWait> waiting = new LinkedList<RequestWait>();
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

        /// <summary>
        /// A wait that completes when a unit has been granted to the waiting process. Withdrawn by an
        /// interrupt or a lost <see cref="Wait.AnyOf"/>: a queued request leaves the queue, and a unit
        /// granted but not yet delivered goes back.
        /// </summary>
        public Wait Request() => new RequestWait(this);

        /// <summary>Returns one unit; the next waiting request, if any, gets it at once.</summary>
        public void Release()
        {
            if (inUse == 0) throw new InvalidOperationException("Resource '" + EntityName + "' has no unit in use to release.");
            inUse--;
            inUseChangedEvent.Raise(this, inUse);
            if (waiting.Count > 0)
            {
                RequestWait next = waiting.First.Value;
                waiting.RemoveFirst();
                next.Node = null;
                Grant(next);
            }
        }

        private void Acquire(RequestWait request)
        {
            if (inUse < capacity && waiting.Count == 0) Grant(request);
            else request.Node = waiting.AddLast(request);
        }

        private void Grant(RequestWait request)
        {
            inUse++;
            inUseChangedEvent.Raise(this, inUse);
            request.State = RequestState.Granted;
            Model.Schedule(0d, request.Deliver);
        }

        private enum RequestState { New, Queued, Granted, Delivered, Withdrawn }

        private sealed class RequestWait : Wait
        {
            private readonly Resource resource;
            private Action resume;

            public RequestWait(Resource resource) { this.resource = resource; }

            public RequestState State { get; set; }
            public LinkedListNode<RequestWait> Node { get; set; }

            internal override void Subscribe(Process process, Action resume)
            {
                this.resume = resume;
                State = RequestState.Queued;
                resource.Acquire(this);
            }

            public void Deliver()
            {
                if (State != RequestState.Granted) return;
                State = RequestState.Delivered;
                resume();
            }

            internal override void Cancel()
            {
                if (State == RequestState.Queued && Node != null)
                {
                    resource.waiting.Remove(Node);
                    Node = null;
                }
                else if (State == RequestState.Granted)
                {
                    State = RequestState.Withdrawn;
                    resource.Release();
                    return;
                }
                if (State != RequestState.Delivered) State = RequestState.Withdrawn;
            }
        }

        public override void Reset()
        {
            base.Reset();
            inUse = 0;
            foreach (RequestWait request in waiting) { request.Node = null; request.State = RequestState.Withdrawn; }
            waiting.Clear();
        }

        #endregion
    }
}
