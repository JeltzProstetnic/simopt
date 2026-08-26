using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// A named output statistic gathered by the simulation engine itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-63. Collectors are <see cref="IEntity"/> implementations so that they are registered
    /// with the model, reset by <c>Model.Reset</c> along with everything else, and discoverable
    /// through <c>Model.FindEntities</c> — which is what lets a headless run, the MCP head and the
    /// replication runner read results without any of them knowing what was instrumented.
    /// </para>
    /// <para>
    /// The subsystem exists because output statistics previously lived in the rendering layer:
    /// server utilisation was accumulated by sampling a busy flag once per render tick
    /// (<c>SimulationCanvas.cs:297-332</c>). That both misses busy intervals shorter than the poll
    /// gap and puts the product's headline number somewhere a headless caller cannot reach.
    /// </para>
    /// </remarks>
    public interface IStatisticCollector : IEntity
    {
        /// <summary>What is being measured, e.g. "wait_time", "queue_length", "utilization".</summary>
        string StatisticName { get; }

        /// <summary>
        /// Absolute simulation time before which observations are excluded, so that a start-up
        /// transient does not contaminate a steady-state measure. Zero means no truncation.
        /// </summary>
        double WarmupTime { get; set; }

        /// <summary>Number of observations retained after warm-up truncation.</summary>
        long Count { get; }

        /// <summary>
        /// The collector's single summary figure as of <paramref name="now"/> — a mean for an
        /// observation-based statistic, a time-average for a time-persistent one — or
        /// <see cref="double.NaN"/> when <see cref="HasData"/> is false.
        /// </summary>
        /// <remarks>
        /// This exists so the replication runner can aggregate every collector uniformly instead of
        /// switching on its concrete type. A type switch there would silently omit any collector
        /// kind added later, and the omission would look like a metric that simply was not
        /// instrumented rather than like a bug.
        /// </remarks>
        double Estimate(double now);

        /// <summary>
        /// Whether any data survived warm-up truncation. When false, every reported figure is
        /// <see cref="double.NaN"/> rather than zero — "nothing was measured" and "the measured
        /// value was zero" are different claims and must not be confused in a result a decision
        /// rests on.
        /// </summary>
        bool HasData { get; }
    }
}
