using System;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// A count of events after the warm-up, reported as the count itself or per unit of time:
    /// items reaching a sink (<c>count</c>) and the rate they reach it at (<c>throughput</c>).
    /// </summary>
    /// <remarks>
    /// SIM-65 step 5. A count is neither a mean of observations (<see cref="TallyCollector"/>) nor a
    /// time-average of a level (<see cref="TimeWeightedCollector"/>), so it gets its own collector
    /// rather than being derived from one of those by the reader. A count of zero is data: a sink
    /// that received nothing reports 0, not "no data".
    /// </remarks>
    public sealed class CountCollector : Entity, IStatisticCollector
    {
        private long count;

        public CountCollector(IModel model, string statisticName, bool perUnitTime, string id = "", string name = "")
            : base(model, id, name)
        {
            StatisticName = statisticName ?? throw new ArgumentNullException(nameof(statisticName));
            PerUnitTime = perUnitTime;
        }

        public string StatisticName { get; }

        /// <summary>True to report the count per unit of time after the warm-up.</summary>
        public bool PerUnitTime { get; }

        public double WarmupTime { get; set; }

        public long Count => count;

        public bool HasData => true;

        /// <summary>Counts one event at the model's current simulation time.</summary>
        public void Increment() => Increment(Model.CurrentTime);

        /// <summary>Counts one event at <paramref name="time"/>, unless it falls before the warm-up.</summary>
        public void Increment(double time)
        {
            if (time >= WarmupTime) count++;
        }

        /// <summary>
        /// The count, or the count per unit of time since the warm-up as of <paramref name="now"/>
        /// (NaN while no time has passed since the warm-up).
        /// </summary>
        public double Estimate(double now)
        {
            if (!PerUnitTime) return count;
            double span = now - WarmupTime;
            return span > 0d ? count / span : double.NaN;
        }

        public override void Reset() => count = 0;
    }
}
