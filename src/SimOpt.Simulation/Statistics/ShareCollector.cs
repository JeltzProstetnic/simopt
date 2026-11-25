using System;
using SimOpt.Simulation.Engine;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// SIM-126 schema: the share of the items reaching any of a model's sinks after the warm-up that reach
    /// one of them (the share who gave up, a scrap rate, "left without being seen"), per replication.
    /// </summary>
    /// <remarks>
    /// A ratio of two counts, so neither a mean of observations nor a time-average; with no item
    /// finished it has no data rather than a share of 0.
    /// </remarks>
    public sealed class ShareCollector : Entity, IStatisticCollector
    {
        private long here, everywhere;

        public ShareCollector(IModel model, string statisticName, string id = "", string name = "")
            : base(model, id, name)
        {
            StatisticName = statisticName ?? throw new ArgumentNullException(nameof(statisticName));
        }

        public string StatisticName { get; }
        public double WarmupTime { get; set; }

        /// <summary>The items counted at this sink.</summary>
        public long Here => here;

        /// <summary>The items counted at every sink, this one included.</summary>
        public long Everywhere => everywhere;

        /// <summary>The items counted, at any sink.</summary>
        public long Count => everywhere;

        public bool HasData => everywhere > 0;

        /// <summary>Counts an item reaching a sink at the current time, <paramref name="atThisSink"/> or elsewhere.</summary>
        public void Record(bool atThisSink)
        {
            if (Model.CurrentTime < WarmupTime) return;
            everywhere++;
            if (atThisSink) here++;
        }

        public double Estimate(double now) => everywhere > 0 ? here / (double)everywhere : double.NaN;

        public override void Reset()
        {
            here = 0;
            everywhere = 0;
        }
    }
}
