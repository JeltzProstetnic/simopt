using System;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// An observation-based ("tally") statistic: waiting time, cycle time, time spent in a delay.
    /// Each observation carries equal weight, in contrast to
    /// <see cref="TimeWeightedCollector"/> which weights by how long a value was in force.
    /// </summary>
    /// <remarks>
    /// SIM-63. Mean and variance accumulate by Welford's method. That is a correctness
    /// requirement rather than a stylistic preference: simulated waiting times sit on a large mean
    /// with comparatively small spread, and the textbook shortcut
    /// <c>(Σx² − n·x̄²)/(n−1)</c> loses every significant digit to cancellation at that scale —
    /// frequently returning a negative variance. Welford is single-pass, keeps O(1) state, and
    /// stays accurate.
    /// </remarks>
    public sealed class TallyCollector : Entity, IStatisticCollector
    {
        private long count;
        private double mean;        // running mean
        private double m2;          // running sum of squared deviations from the running mean
        private double min;
        private double max;

        public TallyCollector(IModel model, string statisticName, string id = "", string name = "")
            : base(model, id, name)
        {
            StatisticName = statisticName ?? throw new ArgumentNullException(nameof(statisticName));
            ResetState();
        }

        public string StatisticName { get; }

        public double WarmupTime { get; set; }

        public long Count => count;

        public bool HasData => count > 0;

        /// <summary>Mean of the retained observations, or NaN when there are none.</summary>
        public double Mean => count > 0 ? mean : double.NaN;

        /// <summary>Smallest retained observation, or NaN when there are none.</summary>
        public double Min => count > 0 ? min : double.NaN;

        /// <summary>Largest retained observation, or NaN when there are none.</summary>
        public double Max => count > 0 ? max : double.NaN;

        /// <summary>
        /// Sample variance (n−1 denominator), or NaN with fewer than two observations. The sample
        /// form is used because a simulation run is a sample from the process, never the whole of
        /// it; the population form would understate the spread and narrow every interval built on
        /// it.
        /// </summary>
        public double Variance => count > 1 ? m2 / (count - 1) : double.NaN;

        /// <summary>Square root of <see cref="Variance"/>, or NaN when the variance is undefined.</summary>
        public double StdDev
        {
            get
            {
                double v = Variance;
                return double.IsNaN(v) ? double.NaN : Math.Sqrt(v);
            }
        }

        /// <summary>Records an observation made at the model's current simulation time.</summary>
        public void Observe(double value) => Observe(value, Model.CurrentTime);

        /// <summary>
        /// Records an observation completing at <paramref name="time"/>. Observations completing
        /// before <see cref="WarmupTime"/> are discarded; the boundary is inclusive, so an
        /// observation completing exactly at the warm-up time counts.
        /// </summary>
        public void Observe(double value, double time)
        {
            if (time < WarmupTime) return;

            count++;
            double delta = value - mean;
            mean += delta / count;
            m2 += delta * (value - mean);   // second factor uses the UPDATED mean — this is Welford

            if (value < min) min = value;
            if (value > max) max = value;
        }

        /// <summary>
        /// The mean of the retained observations. The <paramref name="now"/> argument is part of
        /// the shared collector contract and is unused here — an observation-based statistic is
        /// complete the moment its last observation was made, with no open interval to close.
        /// </summary>
        public double Estimate(double now) => Mean;

        public override void Reset() => ResetState();

        private void ResetState()
        {
            // WarmupTime is deliberately not cleared: it is a setting of the experiment, not
            // accumulated state, and the replication runner sets it once for all replications.
            count = 0;
            mean = 0d;
            m2 = 0d;
            min = double.PositiveInfinity;
            max = double.NegativeInfinity;
        }
    }
}
