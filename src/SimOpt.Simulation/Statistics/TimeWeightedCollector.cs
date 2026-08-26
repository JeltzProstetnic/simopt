using System;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// A time-persistent statistic: queue length, server utilisation, work in progress. The value
    /// is a step function of simulated time and the collector integrates it, so a value that held
    /// for a long time counts for more than one that held briefly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-63. This replaces the polled accumulation in the rendering layer
    /// (<c>SimulationCanvas.cs:297-332</c>), which sampled a server's busy flag once per render
    /// tick. Sampling cannot see a busy interval shorter than the gap between samples, so reported
    /// utilisation was biased by an amount that depended on the frame rate — and the number was
    /// unreachable from a headless, MCP or optimiser run.
    /// </para>
    /// <para>
    /// Two details carry the correctness of this class. First, reads close the final open interval
    /// <b>virtually</b>, without mutating state, so reading twice gives the same answer and a model
    /// that is continued after a read stays correct. Second, warm-up truncation is applied by
    /// clamping interval arithmetic rather than by clearing state at the boundary: a step in force
    /// across the warm-up boundary contributes only its post-boundary part, but still counts toward
    /// the extremes, because it genuinely was in force at that time.
    /// </para>
    /// </remarks>
    public sealed class TimeWeightedCollector : Entity, IStatisticCollector
    {
        private readonly Func<double> currentValueProbe;

        private double currentValue;
        private double lastChangeTime;
        private double integral;
        private double min;
        private double max;
        private bool seenPostWarmup;

        /// <param name="currentValueProbe">
        /// Reads the live value of the observed quantity, e.g. <c>() =&gt; buffer.Count</c>. It is
        /// consulted on <see cref="Reset"/> so that the collector starts each run holding the truth
        /// regardless of the order in which the model happens to reset its entities — a source that
        /// auto-starts creates its first entity <i>during</i> the reset, so a collector cannot
        /// assume the thing it observes is empty when it is reset.
        /// </param>
        public TimeWeightedCollector(IModel model, string statisticName, Func<double> currentValueProbe,
                                     string id = "", string name = "")
            : base(model, id, name)
        {
            StatisticName = statisticName ?? throw new ArgumentNullException(nameof(statisticName));
            this.currentValueProbe = currentValueProbe ?? throw new ArgumentNullException(nameof(currentValueProbe));
            ResetState();
        }

        public string StatisticName { get; }

        public double WarmupTime { get; set; }

        /// <summary>Number of value changes retained after warm-up truncation.</summary>
        public long Count { get; private set; }

        /// <summary>
        /// Whether the post-warm-up observation window has opened at all, judged against the
        /// model's current simulation time.
        /// </summary>
        public bool HasData => seenPostWarmup || WindowIsOpenAt(Model?.CurrentTime ?? 0d);

        /// <summary>The value currently in force.</summary>
        public double CurrentValue => currentValue;

        /// <summary>
        /// Largest value in force at or after the warm-up boundary, as of <paramref name="now"/>,
        /// or NaN if the observation window is empty.
        /// </summary>
        /// <remarks>
        /// This takes the reading time for the same reason <see cref="TimeIntegral"/> does: the
        /// final interval is still open, and the value holding it may be the extreme one. A
        /// parameterless property would have to either mutate state on read or silently ignore the
        /// open interval — the second of which quietly under-reports the peak queue length, the one
        /// number a capacity decision most often turns on.
        /// </remarks>
        public double Max(double now) => Extremes(now).Max;

        /// <summary>
        /// Smallest value in force at or after the warm-up boundary, as of <paramref name="now"/>,
        /// or NaN if the observation window is empty.
        /// </summary>
        public double Min(double now) => Extremes(now).Min;

        private (double Min, double Max) Extremes(double now)
        {
            // Deliberately a time comparison rather than "is the pending area non-zero": a value of
            // zero held after the warm-up boundary is data, and an empty queue is exactly the case
            // where Min must report 0 instead of NaN.
            bool openIntervalCounts = WindowIsOpenAt(now);

            if (!seenPostWarmup)
            {
                return openIntervalCounts
                    ? (currentValue, currentValue)
                    : (double.NaN, double.NaN);
            }

            return openIntervalCounts
                ? (Math.Min(min, currentValue), Math.Max(max, currentValue))
                : (min, max);
        }

        /// <summary>Records a new value taking effect at the model's current simulation time.</summary>
        public void Record(double newValue) => Record(newValue, Model.CurrentTime);

        /// <summary>Records a new value taking effect at <paramref name="time"/>.</summary>
        public void Record(double newValue, double time)
        {
            Accumulate(time);
            currentValue = newValue;
            lastChangeTime = time;
            if (time >= WarmupTime) NoteExtreme(newValue);
            Count++;
        }

        /// <summary>Adds <paramref name="delta"/> to the current value, effective now.</summary>
        public void Increment(double delta = 1d) => Record(currentValue + delta);

        /// <summary>Adds <paramref name="delta"/> to the current value, effective at <paramref name="time"/>.</summary>
        public void Increment(double delta, double time) => Record(currentValue + delta, time);

        /// <summary>Subtracts <paramref name="delta"/> from the current value, effective now.</summary>
        public void Decrement(double delta = 1d) => Record(currentValue - delta);

        /// <summary>Subtracts <paramref name="delta"/> from the current value, effective at <paramref name="time"/>.</summary>
        public void Decrement(double delta, double time) => Record(currentValue - delta, time);

        /// <summary>
        /// Area under the step function up to <paramref name="now"/>, warm-up excluded. Does not
        /// mutate the collector, so it may be called repeatedly and mid-run.
        /// </summary>
        public double TimeIntegral(double now)
        {
            return integral + PendingArea(now);
        }

        /// <summary>
        /// Time-average of the step function over the post-warm-up observation window, or NaN when
        /// that window is empty — which is what a run shorter than its own warm-up period produces.
        /// </summary>
        public double TimeAverage(double now)
        {
            double windowStart = Math.Max(WarmupTime, startTimeOfRun);
            double span = now - windowStart;
            if (span <= 0d) return double.NaN;
            return TimeIntegral(now) / span;
        }

        /// <summary>The time-average as of <paramref name="now"/>.</summary>
        public double Estimate(double now) => TimeAverage(now);

        public override void Reset() => ResetState();

        // ── internals ────────────────────────────────────────────────────────

        private double startTimeOfRun;

        /// <summary>
        /// Folds the interval [lastChangeTime, upTo) into the accumulated integral, clamped so that
        /// only the part at or after the warm-up boundary counts.
        /// </summary>
        private void Accumulate(double upTo)
        {
            double from = Math.Max(lastChangeTime, WarmupTime);
            if (upTo <= from) return;

            integral += currentValue * (upTo - from);
            // The value held ACROSS the boundary was genuinely in force at the warm-up time, so it
            // belongs in the extremes even though the step that set it began earlier. Clearing
            // state at the boundary instead of clamping is what loses this, and the loss is silent.
            NoteExtreme(currentValue);
        }

        /// <summary>Whether the still-open final interval extends past the warm-up boundary.</summary>
        private bool WindowIsOpenAt(double now) => now > Math.Max(lastChangeTime, WarmupTime);

        /// <summary>Area of the still-open final interval, computed without mutating state.</summary>
        private double PendingArea(double now)
        {
            double from = Math.Max(lastChangeTime, WarmupTime);
            return now > from ? currentValue * (now - from) : 0d;
        }

        private void NoteExtreme(double value)
        {
            if (!seenPostWarmup)
            {
                seenPostWarmup = true;
                min = value;
                max = value;
                return;
            }
            if (value < min) min = value;
            if (value > max) max = value;
        }

        private void ResetState()
        {
            startTimeOfRun = Model?.CurrentTime ?? 0d;
            currentValue = currentValueProbe();
            lastChangeTime = startTimeOfRun;
            integral = 0d;
            min = 0d;
            max = 0d;
            seenPostWarmup = false;
            Count = 0;
        }
    }
}
