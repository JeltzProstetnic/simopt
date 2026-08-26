using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.SpecialFunctions;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// Runs a model repeatedly with independent random streams and reports each instrumented
    /// statistic as a mean with a confidence interval.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-63, serving UN-012. A single simulation run produces one draw from a random process, and
    /// reporting it as though it were the answer is how a confident wrong recommendation gets made.
    /// Replication is what turns "the queue was 4.2 long" into "the queue is 4.2 long, give or take
    /// 0.3" — and the second statement is the only one a decision can honestly rest on.
    /// </para>
    /// <para>
    /// The interval is computed <b>across replication means</b>, never from the observations within
    /// a single run. Observations inside one run are heavily autocorrelated — a long queue at one
    /// moment implies a long queue the next — so a within-run interval is far too narrow. That is
    /// the classic silent error in this area, and it errs in the direction of overstating the
    /// tool's own precision.
    /// </para>
    /// </remarks>
    public sealed class ReplicationRunner
    {
        private readonly Model model;
        private readonly double runLength;
        private readonly double warmupTime;

        /// <param name="runLength">Simulated time per replication, including the warm-up period.</param>
        /// <param name="warmupTime">
        /// Simulated time excluded from every statistic, to keep the start-up transient out of a
        /// steady-state measure. A single value declared in advance and applied identically to
        /// every collector and every replication — which is what makes it a stated assumption a
        /// third party can check, rather than an algorithmic choice needing its own defence.
        /// </param>
        public ReplicationRunner(Model model, double runLength, double warmupTime = 0d)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            if (runLength <= 0d)
                throw new ArgumentOutOfRangeException(nameof(runLength), "Run length must be positive.");
            if (warmupTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(warmupTime), "Warm-up time cannot be negative.");
            if (warmupTime >= runLength)
                throw new ArgumentOutOfRangeException(nameof(warmupTime),
                    "Warm-up time must be shorter than the run, otherwise every statistic is empty by construction.");

            this.runLength = runLength;
            this.warmupTime = warmupTime;
        }

        /// <summary>
        /// Runs <paramref name="replications"/> independent replications and summarises every
        /// collector registered with the model.
        /// </summary>
        /// <param name="baseSeed">
        /// Root of the seed derivation. The same base seed always produces the same set of
        /// replications, on any machine and after any restart — which is what makes a reported
        /// figure reproducible by someone who was not present when it was produced (UN-033).
        /// </param>
        public ReplicationResult Run(int replications, int baseSeed, double confidenceLevel = 0.95)
        {
            if (replications < 1)
                throw new ArgumentOutOfRangeException(nameof(replications), "At least one replication is required.");
            if (confidenceLevel <= 0d || confidenceLevel >= 1d)
                throw new ArgumentOutOfRangeException(nameof(confidenceLevel),
                    "Confidence level must be strictly between 0 and 1.");

            var collectors = model.FindEntities<IStatisticCollector>().ToList();
            var seeds = new List<int>(replications);
            var samples = new Dictionary<string, List<double>>();
            var names = new Dictionary<string, string>();

            foreach (IStatisticCollector c in collectors)
            {
                samples[c.Identifier] = new List<double>(replications);
                names[c.Identifier] = c.StatisticName;
            }

            for (int r = 0; r < replications; r++)
            {
                int seed = SeedFor(baseSeed, r);
                seeds.Add(seed);

                model.Reset(seed);

                // Set after Reset: Reset deliberately preserves WarmupTime, but a collector created
                // outside this runner may never have had one set at all.
                foreach (IStatisticCollector c in collectors) c.WarmupTime = warmupTime;

                model.Run(runLength);

                // Read before the next Reset — a reset wipes every collector.
                foreach (IStatisticCollector c in collectors)
                    samples[c.Identifier].Add(c.Estimate(model.CurrentTime));
            }

            var metrics = collectors
                .Select(c => Summarise(c.Identifier, names[c.Identifier], samples[c.Identifier], confidenceLevel))
                .ToList();

            return new ReplicationResult(replications, baseSeed, runLength, warmupTime,
                                         confidenceLevel, seeds, metrics);
        }

        /// <summary>
        /// Derives replication <paramref name="index"/>'s seed from the base seed.
        /// </summary>
        /// <remarks>
        /// Uses the same process-stable FNV-1a hash that keys per-node streams (SIM-62), so the
        /// derivation survives a process restart — .NET's own string hashing is randomised per
        /// process and would make every reported figure irreproducible tomorrow.
        /// <para>
        /// Distinct seeds are the standard practice for independent replications, but they are not
        /// a proof of stream independence; a counter-based generator would be. That is a known and
        /// deliberate limitation, recorded here rather than left for someone to assume away.
        /// </para>
        /// </remarks>
        public static int SeedFor(int baseSeed, int index)
            => baseSeed ^ StableHash.Of("replication:" + index);

        /// <summary>
        /// Half-width of a two-sided confidence interval on the mean of independent
        /// <paramref name="values"/>, or null when fewer than two are available.
        /// </summary>
        /// <remarks>
        /// Exposed separately from the run loop so the interval arithmetic can be pinned against
        /// hand-computed values without simulating anything. An interval that is only ever
        /// exercised through a stochastic model is an interval nobody has actually checked.
        /// </remarks>
        public static double? ConfidenceHalfWidth(IReadOnlyList<double> values, double confidenceLevel = 0.95)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (confidenceLevel <= 0d || confidenceLevel >= 1d)
                throw new ArgumentOutOfRangeException(nameof(confidenceLevel),
                    "Confidence level must be strictly between 0 and 1.");

            int n = values.Count;
            if (n < 2) return null;

            double mean = values.Average();
            double sumSq = values.Sum(v => (v - mean) * (v - mean));

            // n−1: the mean was estimated from this same data, so the sum of squared deviations has
            // expectation (n−1)σ². Dividing by n would bias the interval systematically narrow —
            // the wrong direction of error for a claim about precision.
            double stdDev = Math.Sqrt(sumSq / (n - 1));
            double t = StudentT.TwoSidedCriticalValue(1d - confidenceLevel, n - 1);
            return t * stdDev / Math.Sqrt(n);
        }

        private static MetricSummary Summarise(string id, string name, List<double> values, double confidenceLevel)
        {
            double[] usable = values.Where(v => !double.IsNaN(v)).ToArray();

            if (usable.Length == 0)
                return new MetricSummary(id, name, 0, double.NaN, double.NaN, null, values.ToArray(), false);

            double mean = usable.Average();

            if (usable.Length == 1)
            {
                // One replication carries no information about its own variability. Reporting a
                // half-width of zero here would read as infinite precision, which is the single
                // most dangerous number this subsystem could emit — so there is no interval at all.
                return new MetricSummary(id, name, 1, mean, double.NaN, null, values.ToArray(), true);
            }

            double meanOfUsable = mean;
            double stdDev = Math.Sqrt(usable.Sum(v => (v - meanOfUsable) * (v - meanOfUsable)) / (usable.Length - 1));
            double? halfWidth = ConfidenceHalfWidth(usable, confidenceLevel);

            return new MetricSummary(id, name, usable.Length, mean, stdDev, halfWidth, values.ToArray(), true);
        }
    }

    /// <summary>Summary of one statistic across replications.</summary>
    /// <param name="ReplicateEstimates">
    /// Every replication's own figure, retained so a reader can check the summary rather than
    /// having to trust it.
    /// </param>
    /// <param name="HalfWidth">
    /// Null when no interval is computable — with a single replication, or when no replication
    /// produced data. Never zero: a zero would be indistinguishable from perfect precision.
    /// </param>
    public sealed record MetricSummary(
        string StatisticId,
        string StatisticName,
        int Replications,
        double Mean,
        double StdDev,
        double? HalfWidth,
        double[] ReplicateEstimates,
        bool HasData)
    {
        /// <summary>Lower end of the confidence interval, or NaN when there is no interval.</summary>
        public double Lower => HalfWidth.HasValue ? Mean - HalfWidth.Value : double.NaN;

        /// <summary>Upper end of the confidence interval, or NaN when there is no interval.</summary>
        public double Upper => HalfWidth.HasValue ? Mean + HalfWidth.Value : double.NaN;
    }

    /// <summary>
    /// The outcome of a replicated experiment, carrying enough provenance for a third party to
    /// reproduce it: the base seed, every derived replication seed, the run length, the warm-up
    /// applied and the confidence level used (UN-034).
    /// </summary>
    public sealed record ReplicationResult(
        int Replications,
        int BaseSeed,
        double RunLength,
        double WarmupTime,
        double ConfidenceLevel,
        IReadOnlyList<int> ReplicationSeeds,
        IReadOnlyList<MetricSummary> Metrics)
    {
        /// <summary>Finds a metric by the identifier of the collector that produced it.</summary>
        public MetricSummary this[string statisticId] =>
            Metrics.FirstOrDefault(m => m.StatisticId == statisticId)
            ?? throw new KeyNotFoundException($"No statistic with id '{statisticId}' was collected.");
    }
}
