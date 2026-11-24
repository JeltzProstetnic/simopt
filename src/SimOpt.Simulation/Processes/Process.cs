using System;
using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;

namespace SimOpt.Simulation.Processes
{
    /// <summary>
    /// SIM-126: a behaviour written as one iterator method that yields <see cref="Wait"/>s, as in
    /// SimPy, driven by the event engine. Design: <c>docs/plans/2026-11-25-sim-126-process-modelling.md</c>.
    /// <para>
    /// A process is a <see cref="StochasticEntity"/>, so with an id it has its own seed ID (SIM-125).
    /// A process started while the model is stopped is part of the model and starts again from the
    /// beginning on every <c>Reset</c>; one started during a run is transient and ends with the run.
    /// Yielding a process waits until it has finished.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class Process : StochasticEntity
    {
        #region cvar

        private readonly Func<Process, IEnumerable<Wait>> body;
        private readonly bool restartOnReset;
        private readonly List<Action> joiners = new List<Action>();
        private IEnumerator<Wait> steps;
        private int generation;
        private bool alive;
        private Wait current;          // what the process is waiting for, or null
        private int token;             // identifies the current wait; a resume carrying an older one is stale
        private bool interruptPending;
        private bool interrupted;

        #endregion
        #region ctor

        internal Process(IModel model, Func<Process, IEnumerable<Wait>> body, string id, string name)
            : base(model, id, name)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            restartOnReset = !model.IsRunning;
            Begin();
        }

        #endregion
        #region prop

        /// <summary>True from the start until the body has run to its end.</summary>
        public bool IsAlive { get { return alive; } }

        /// <summary>The current simulation time.</summary>
        public double Now { get { return Model.CurrentTime; } }

        /// <summary>
        /// True in the step that follows an <see cref="Interrupt"/>, false otherwise. Read it after the
        /// yield to tell an interrupt from a completed wait.
        /// </summary>
        public bool Interrupted { get { return interrupted; } }

        /// <summary>A wait that completes when this process has finished.</summary>
        public Wait Finished { get { return new JoinWait(this); } }

        /// <summary>Lets a body yield a process directly to wait until it has finished.</summary>
        public static implicit operator Wait(Process process) => process.Finished;

        #endregion
        #region impl

        /// <summary>A wait that completes after <paramref name="delay"/> time units (0 is allowed).</summary>
        public Wait Timeout(double delay) => new TimeoutWait(delay);

        /// <summary>
        /// Schedules the first step as a zero-delay event, so a process started inside a handler runs
        /// after that handler. The generation makes resumes from an earlier start no-ops.
        /// </summary>
        private void Begin()
        {
            generation++;
            DisposeSteps();
            joiners.Clear();
            current = null;
            interruptPending = interrupted = false;
            alive = true;
            int g = generation;
            Model.Schedule(0d, () =>
            {
                if (g != generation) return;
                steps = body(this).GetEnumerator();
                Step(g);
            });
        }

        private void Step(int g)
        {
            if (g != generation || !alive) return;
            interrupted = interruptPending;
            interruptPending = false;
            if (!steps.MoveNext())
            {
                Finish();
                return;
            }
            current = steps.Current ?? throw new InvalidOperationException(
                "Process '" + EntityName + "' yielded null; yield a Wait such as Timeout(...).");
            int t = ++token;
            current.Subscribe(this, () => Resume(g, t));
        }

        private void Resume(int g, int t)
        {
            if (g != generation || t != token) return;
            current = null;
            Step(g);
        }

        /// <summary>
        /// Stops the process waiting: its current wait is withdrawn, and it resumes at once (from a
        /// zero-delay event) with <see cref="Interrupted"/> set. Ignored if the process is not waiting.
        /// </summary>
        public void Interrupt()
        {
            if (!alive || current == null) return;
            current.Cancel();
            current = null;
            interruptPending = true;
            int g = generation, t = ++token;
            Model.Schedule(0d, () => Resume(g, t));
        }

        private void Finish()
        {
            alive = false;
            current = null;
            DisposeSteps();
            Action[] waiting = joiners.ToArray();
            joiners.Clear();
            foreach (Action resume in waiting) Model.Schedule(0d, resume);
            // a finished process started during a run is done for good (SIM-124)
            if (!restartOnReset) Model.ReleaseTransient(this);
        }

        /// <summary>Resumes <paramref name="resume"/> when this process has finished, or now if it has.</summary>
        internal void Join(Action resume)
        {
            if (alive) joiners.Add(resume);
            else Model.Schedule(0d, resume);
        }

        private void DisposeSteps()
        {
            steps?.Dispose();
            steps = null;
        }

        public override void Reset()
        {
            base.Reset();
            if (restartOnReset)
            {
                Begin();
            }
            else
            {
                generation++;
                DisposeSteps();
                joiners.Clear();
                current = null;
                alive = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// SIM-126: starting processes on a model.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Starts <paramref name="body"/> as a process. Started while the model is stopped, it is part of
        /// the model and starts again on every <c>Reset</c>; started during a run, it ends with the run.
        /// </summary>
        public static Process StartProcess(this IModel model, Func<Process, IEnumerable<Wait>> body,
                                           string id = "", string name = "")
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            return new Process(model, body, id, name);
        }
    }
}
