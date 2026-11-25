using System;
using SimOpt.Simulation.Events;

namespace SimOpt.Simulation.Processes
{
    // SIM-126, slice 4: a process waits for the next raise of an event, so process-style logic can sit on
    // top of a Source/Buffer/Server model. The handler records what the raise carried, removes itself and
    // resumes the process through a zero-delay event, so the process continues in the same instant after
    // the event and its other handlers (SIM-128). Cancelling (a lost any-of, an interrupt) or abandoning
    // (a reset) removes the handler, so no stale handler adds an event to a later replication.

    /// <summary>
    /// Waits for the next raise of a unary event that passes the filter; made by
    /// <see cref="Wait.Next{T}(UnaryEvent{T}, Func{T, bool})"/>.
    /// </summary>
    public sealed class EventWait<T> : Wait
    {
        private readonly UnaryEvent<T> evnt;
        private readonly Func<T, bool> when;
        private Action<T> handler;
        private bool cancelled;

        internal EventWait(UnaryEvent<T> evnt, Func<T, bool> when)
        {
            this.evnt = evnt ?? throw new ArgumentNullException(nameof(evnt));
            this.when = when;
        }

        /// <summary>What the raise that ended the wait carried.</summary>
        public T Value { get; private set; }

        internal override void Subscribe(Process process, Action resume)
        {
            cancelled = false;
            handler = value =>
            {
                if (cancelled || (when != null && !when(value))) return;
                Value = value;
                Detach();
                process.Model.Schedule(0d, () => { if (!cancelled) resume(); });
            };
            evnt.AddHandler(handler);
        }

        internal override void Cancel()
        {
            cancelled = true;
            Detach();
        }

        internal override void Abandon() => Cancel();

        private void Detach()
        {
            if (handler == null) return;
            evnt.RemoveHandler(handler);
            handler = null;
        }
    }

    /// <summary>
    /// Waits for the next raise of a binary event (sender and arguments) that passes the filter; made by
    /// <see cref="Wait.Next{TSender, TArgs}(BinaryEvent{TSender, TArgs}, Func{TSender, TArgs, bool})"/>.
    /// </summary>
    public sealed class EventWait<TSender, TArgs> : Wait
    {
        private readonly BinaryEvent<TSender, TArgs> evnt;
        private readonly Func<TSender, TArgs, bool> when;
        private Action<TSender, TArgs> handler;
        private bool cancelled;

        internal EventWait(BinaryEvent<TSender, TArgs> evnt, Func<TSender, TArgs, bool> when)
        {
            this.evnt = evnt ?? throw new ArgumentNullException(nameof(evnt));
            this.when = when;
        }

        /// <summary>The sender of the raise that ended the wait.</summary>
        public TSender Sender { get; private set; }

        /// <summary>The arguments of the raise that ended the wait, such as the item a buffer received.</summary>
        public TArgs Args { get; private set; }

        internal override void Subscribe(Process process, Action resume)
        {
            cancelled = false;
            handler = (sender, args) =>
            {
                if (cancelled || (when != null && !when(sender, args))) return;
                Sender = sender;
                Args = args;
                Detach();
                process.Model.Schedule(0d, () => { if (!cancelled) resume(); });
            };
            evnt.AddHandler(handler);
        }

        internal override void Cancel()
        {
            cancelled = true;
            Detach();
        }

        internal override void Abandon() => Cancel();

        private void Detach()
        {
            if (handler == null) return;
            evnt.RemoveHandler(handler);
            handler = null;
        }
    }

    /// <summary>
    /// Waits for the next raise of a ternary event (sender, arguments and data) that passes the filter;
    /// made by <see cref="Wait.Next{TSender, TArgs, TData}(TernaryEvent{TSender, TArgs, TData}, Func{TSender, TArgs, TData, bool})"/>.
    /// </summary>
    public sealed class EventWait<TSender, TArgs, TData> : Wait
    {
        private readonly TernaryEvent<TSender, TArgs, TData> evnt;
        private readonly Func<TSender, TArgs, TData, bool> when;
        private Action<TSender, TArgs, TData> handler;
        private bool cancelled;

        internal EventWait(TernaryEvent<TSender, TArgs, TData> evnt, Func<TSender, TArgs, TData, bool> when)
        {
            this.evnt = evnt ?? throw new ArgumentNullException(nameof(evnt));
            this.when = when;
        }

        /// <summary>The sender of the raise that ended the wait.</summary>
        public TSender Sender { get; private set; }

        /// <summary>The arguments of the raise that ended the wait.</summary>
        public TArgs Args { get; private set; }

        /// <summary>The data of the raise that ended the wait.</summary>
        public TData Data { get; private set; }

        internal override void Subscribe(Process process, Action resume)
        {
            cancelled = false;
            handler = (sender, args, data) =>
            {
                if (cancelled || (when != null && !when(sender, args, data))) return;
                Sender = sender;
                Args = args;
                Data = data;
                Detach();
                process.Model.Schedule(0d, () => { if (!cancelled) resume(); });
            };
            evnt.AddHandler(handler);
        }

        internal override void Cancel()
        {
            cancelled = true;
            Detach();
        }

        internal override void Abandon() => Cancel();

        private void Detach()
        {
            if (handler == null) return;
            evnt.RemoveHandler(handler);
            handler = null;
        }
    }
}
