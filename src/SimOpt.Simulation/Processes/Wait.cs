using System;
using System.Linq;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126: something a process yields and waits for: a timeout, a stock or resource request,
    /// another process, or a combination (<see cref="AnyOf"/>, <see cref="AllOf"/>). The process is
    /// resumed at the instant the wait completes. A wait is yielded once.
    /// </summary>
    public abstract class Wait
    {
        /// <summary>
        /// Starts waiting on behalf of <paramref name="process"/> and calls <paramref name="resume"/>
        /// exactly once, from an event at the instant the wait completes.
        /// </summary>
        internal abstract void Subscribe(Process process, Action resume);

        /// <summary>
        /// Stops waiting: the wait will not resume anyone, and a request it still holds is withdrawn
        /// (or, if already granted but not yet delivered, given back). Called by an interrupt and by
        /// <see cref="AnyOf"/> for the waits that lost.
        /// </summary>
        internal virtual void Cancel() { }

        /// <summary>
        /// A wait for anything that takes a completion callback, e.g.
        /// <c>Wait.From(done =&gt; stock.Take(5, done))</c>. The callback must be invoked from an event.
        /// Such a wait cannot be withdrawn.
        /// </summary>
        public static Wait From(Action<Action> start)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            return new CallbackWait(start);
        }

        /// <summary>Completes when the first of <paramref name="waits"/> completes; the others are withdrawn.</summary>
        public static AnyOfWait AnyOf(params Wait[] waits) => new AnyOfWait(waits);

        /// <summary>Completes when all of <paramref name="waits"/> have completed.</summary>
        public static Wait AllOf(params Wait[] waits) => new AllOfWait(waits);

        internal static Wait[] Check(Wait[] waits)
        {
            if (waits == null || waits.Length == 0) throw new ArgumentException("At least one wait is required.", nameof(waits));
            if (waits.Any(w => w == null)) throw new ArgumentException("A wait must not be null.", nameof(waits));
            return waits;
        }

        private sealed class CallbackWait : Wait
        {
            private readonly Action<Action> start;
            public CallbackWait(Action<Action> start) { this.start = start; }
            internal override void Subscribe(Process process, Action resume) => start(resume);
        }
    }

    /// <summary>
    /// SIM-126: completes when the first of its waits completes and withdraws the others.
    /// <see cref="Winner"/> names the one that completed.
    /// </summary>
    public sealed class AnyOfWait : Wait
    {
        private readonly Wait[] waits;
        private bool done;

        internal AnyOfWait(Wait[] waits) { this.waits = Check(waits); }

        /// <summary>The wait that completed first, or null while waiting.</summary>
        public Wait Winner { get; private set; }

        internal override void Subscribe(Process process, Action resume)
        {
            foreach (Wait w in waits)
            {
                Wait child = w;
                child.Subscribe(process, () =>
                {
                    if (done) return;
                    done = true;
                    Winner = child;
                    foreach (Wait other in waits) if (!ReferenceEquals(other, child)) other.Cancel();
                    resume();
                });
                if (done) break;
            }
        }

        internal override void Cancel()
        {
            if (done) return;
            done = true;
            foreach (Wait w in waits) w.Cancel();
        }
    }

    internal sealed class AllOfWait : Wait
    {
        private readonly Wait[] waits;
        private int pending;
        private bool cancelled;

        public AllOfWait(Wait[] waits) { this.waits = Check(waits); }

        internal override void Subscribe(Process process, Action resume)
        {
            pending = waits.Length;
            foreach (Wait w in waits)
                w.Subscribe(process, () =>
                {
                    if (cancelled) return;
                    if (--pending == 0) resume();
                });
        }

        internal override void Cancel()
        {
            cancelled = true;
            foreach (Wait w in waits) w.Cancel();
        }
    }

    internal sealed class TimeoutWait : Wait
    {
        private readonly double delay;
        private bool cancelled;

        public TimeoutWait(double delay)
        {
            if (double.IsNaN(delay) || delay < 0d)
                throw new ArgumentOutOfRangeException(nameof(delay), "A timeout must not be negative.");
            this.delay = delay;
        }

        internal override void Subscribe(Process process, Action resume)
            => process.Model.Schedule(delay, () => { if (!cancelled) resume(); });

        internal override void Cancel() => cancelled = true;
    }

    internal sealed class JoinWait : Wait
    {
        private readonly Process target;
        private bool cancelled;

        public JoinWait(Process target) { this.target = target; }

        internal override void Subscribe(Process process, Action resume)
            => target.Join(() => { if (!cancelled) resume(); });

        internal override void Cancel() => cancelled = true;
    }
}
