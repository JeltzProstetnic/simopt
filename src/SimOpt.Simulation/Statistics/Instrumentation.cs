using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;

namespace SimOpt.Simulation.Statistics
{
    /// <summary>
    /// Attaches output-statistics collectors to simulation templates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-63. This is the layer a model builder calls — <c>ModelRegistry</c> for MCP-built models,
    /// a topology builder for hand-written ones. It exists so that no builder has to know which
    /// engine event corresponds to which statistic, and so the wiring is defined once instead of
    /// being reinvented (differently, and wrongly) per builder, which is exactly how the codebase
    /// ended up with three disjoint and mutually inconsistent statistics implementations.
    /// </para>
    /// <para>
    /// Every collector returned is an <see cref="IEntity"/> registered with the model, so it is
    /// reset along with everything else and is discoverable through <c>Model.FindEntities</c>
    /// without the caller having to hold a reference to it.
    /// </para>
    /// </remarks>
    public static class Instrumentation
    {
        /// <summary>Statistic-name constant for a buffer's time-averaged queue length.</summary>
        public const string QueueLength = "queue_length";

        /// <summary>Statistic-name constant for time spent waiting in a buffer.</summary>
        public const string WaitTime = "wait_time";

        /// <summary>Statistic-name constant for the fraction of time a server was working.</summary>
        public const string Utilization = "utilization";

        /// <summary>
        /// Records the buffer's contents as a time-persistent statistic, so the reported figure is
        /// a time-average rather than the average of however many times someone happened to look.
        /// </summary>
        public static TimeWeightedCollector ObserveQueueLength<T>(Buffer<T> buffer, double warmup = 0d)
        {
            var collector = new TimeWeightedCollector(
                buffer.Model, QueueLength, () => buffer.Count,
                id: buffer.Identifier + ".stat." + QueueLength,
                name: buffer.EntityName + " queue length")
            { WarmupTime = warmup };

            buffer.ItemReceivedEvent.AddHandler((_, _) => collector.Record(buffer.Count));
            buffer.ItemRemovedEvent.AddHandler((_, _) => collector.Record(buffer.Count));
            return collector;
        }

        /// <summary>
        /// Records how long each item waited in the buffer, as an observation-based statistic.
        /// </summary>
        /// <remarks>
        /// Arrival times are held here, keyed by item reference, rather than stamped onto the
        /// entity. An entity passes through several stations, so a single timestamp field on the
        /// entity could only ever describe one of them; one map per instrumented buffer describes
        /// all of them without touching the entity type at all. The map is bounded by the buffer's
        /// contents and drains as items leave, and <see cref="TallyCollector.Reset"/> on the
        /// collector is paired with a re-stamp of whatever the buffer holds at reset time so the
        /// instrument cannot be corrupted by the order in which the model resets its entities.
        /// </remarks>
        public static TallyCollector ObserveWaitingTime<T>(Buffer<T> buffer, double warmup = 0d)
        {
            var collector = new TallyCollector(
                buffer.Model, WaitTime,
                id: buffer.Identifier + ".stat." + WaitTime,
                name: buffer.EntityName + " waiting time")
            { WarmupTime = warmup };

            var arrivals = new Dictionary<T, double>();

            // The arrival stamp MUST be recorded before any other handler on this event, and this
            // priority is what guarantees it. A server pulls from its buffer, so the conventional
            // wiring puts a "start the server if it is idle" handler on ItemReceivedEvent — and
            // that handler drains the item synchronously, raising ItemRemovedEvent, before control
            // returns. Registered at ordinary priority the instrument would see the removal of an
            // item it had not yet stamped, find nothing in the map, and record no observation at
            // all: a waiting time of exactly zero observations, reported as "no data", on a station
            // that was busy the whole run. Measured, not theorised — it is what this instrument did
            // before the priority was added.
            buffer.ItemReceivedEvent.AddHandler(
                (_, item) => arrivals[item] = buffer.Model.CurrentTime,
                new Priority(type: PriorityType.LowLevelBeforeOthers));

            buffer.ItemRemovedEvent.AddHandler((_, item) =>
            {
                if (!arrivals.TryGetValue(item, out double arrived)) return;
                arrivals.Remove(item);
                collector.Observe(buffer.Model.CurrentTime - arrived);
            });

            return collector;
        }

        /// <summary>
        /// Records the fraction of time the server spent working, from its busy/idle transitions.
        /// </summary>
        /// <remarks>
        /// This is the measurement the product's headline KPI rests on, and the reason the whole
        /// subsystem exists: the previous implementation sampled the busy flag once per UI render
        /// tick, so it could not see a busy interval shorter than the poll gap and its answer moved
        /// with the frame rate. Driven by transitions, the integral is exact.
        /// </remarks>
        public static TimeWeightedCollector ObserveUtilization<TMaterial, TProduct, TData>(
            Server<TMaterial, TProduct, TData> server, double warmup = 0d)
            where TProduct : new()
        {
            var collector = new TimeWeightedCollector(
                server.Model, Utilization, () => server.Working ? 1d : 0d,
                id: server.Identifier + ".stat." + Utilization,
                name: server.EntityName + " utilization")
            { WarmupTime = warmup };

            server.WorkingChangedEvent.AddHandler((_, working) => collector.Record(working ? 1d : 0d));
            return collector;
        }
    }
}
