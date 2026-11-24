using System;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126: something a process yields and waits for: a timeout, a stock request, another
    /// process. The process is resumed at the instant the wait completes.
    /// </summary>
    public abstract class Wait
    {
        /// <summary>
        /// Starts waiting on behalf of <paramref name="process"/> and calls <paramref name="resume"/>
        /// exactly once, from an event at the instant the wait completes.
        /// </summary>
        internal abstract void Subscribe(Process process, Action resume);

        /// <summary>
        /// A wait for anything that takes a completion callback, e.g.
        /// <c>Wait.From(done =&gt; stock.Take(5, done))</c>. The callback must be invoked from an event.
        /// </summary>
        public static Wait From(Action<Action> start)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            return new CallbackWait(start);
        }

        private sealed class CallbackWait : Wait
        {
            private readonly Action<Action> start;
            public CallbackWait(Action<Action> start) { this.start = start; }
            internal override void Subscribe(Process process, Action resume) => start(resume);
        }
    }

    internal sealed class TimeoutWait : Wait
    {
        private readonly double delay;

        public TimeoutWait(double delay)
        {
            if (double.IsNaN(delay) || delay < 0d)
                throw new ArgumentOutOfRangeException(nameof(delay), "A timeout must not be negative.");
            this.delay = delay;
        }

        internal override void Subscribe(Process process, Action resume) => process.Model.Schedule(delay, resume);
    }

    internal sealed class JoinWait : Wait
    {
        private readonly Process target;
        public JoinWait(Process target) { this.target = target; }
        internal override void Subscribe(Process process, Action resume) => target.Join(resume);
    }
}
