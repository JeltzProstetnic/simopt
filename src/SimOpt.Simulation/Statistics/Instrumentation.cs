using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;
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

        /// <summary>Statistic-name constant for a stock's time-averaged level (SIM-127).</summary>
        public const string StockLevel = "stock_level";

        /// <summary>Statistic-name constant for the fraction of time a stock was empty (SIM-127).</summary>
        public const string StockEmpty = "stock_empty";

        /// <summary>SIM-65 step 5: items that reached a sink after the warm-up.</summary>
        public const string ItemCount = "count";

        /// <summary>SIM-65 step 5: items that reached a sink after the warm-up, per unit of time.</summary>
        public const string Throughput = "throughput";

        /// <summary>SIM-65 step 5: time from an item's creation at a source to its arrival at a sink.</summary>
        public const string CycleTime = "cycle_time";

        /// <summary>SIM-126: the share of the items reaching any sink that reach this one.</summary>
        public const string Share = "share";

        /// <summary>
        /// SIM-126: the share of the items reaching any of <paramref name="sinks"/> after the warm-up that
        /// reach <paramref name="at"/>, which is one of them.
        /// </summary>
        public static ShareCollector ObserveShare<T>(Sink<T> at, IEnumerable<Sink<T>> sinks, double warmup = 0d)
        {
            var collector = new ShareCollector(at.Model, Share,
                id: at.Identifier + ".stat." + Share,
                name: at.EntityName + " share")
            { WarmupTime = warmup };

            foreach (Sink<T> sink in sinks)
            {
                bool here = ReferenceEquals(sink, at);
                sink.ItemReceived.AddHandler((_, _) => collector.Record(here));
            }
            return collector;
        }

        /// <summary>
        /// Records a stock's level as a time-persistent statistic (SIM-127).
        /// </summary>
        public static TimeWeightedCollector ObserveStockLevel(Stock stock, double warmup = 0d)
        {
            var collector = new TimeWeightedCollector(
                stock.Model, StockLevel, () => stock.Level,
                id: stock.Identifier + ".stat." + StockLevel,
                name: stock.EntityName + " level")
            { WarmupTime = warmup };

            stock.LevelChangedEvent.AddHandler((_, level) => collector.Record(level));
            return collector;
        }

        /// <summary>
        /// Records the share of a process resource's units in use, time-weighted (SIM-126).
        /// </summary>
        public static TimeWeightedCollector ObserveResourceUtilization(SimOpt.Simulation.Processes.Resource resource, double warmup = 0d)
        {
            var collector = new TimeWeightedCollector(
                resource.Model, Utilization, () => (double)resource.InUse / resource.Capacity,
                id: resource.Identifier + ".stat." + Utilization,
                name: resource.EntityName + " utilization")
            { WarmupTime = warmup };

            resource.InUseChangedEvent.AddHandler((_, inUse) => collector.Record((double)inUse / resource.Capacity));
            return collector;
        }

        /// <summary>
        /// SIM-126: the time each granted request waited for a unit of a process resource, from the
        /// request to the grant, as an observation-based statistic. Requests withdrawn before their grant
        /// (an item that gave up) are not observed.
        /// </summary>
        public static TallyCollector ObserveResourceWaitingTime(SimOpt.Simulation.Processes.Resource resource, double warmup = 0d)
        {
            var collector = new TallyCollector(
                resource.Model, WaitTime,
                id: resource.Identifier + ".stat." + WaitTime,
                name: resource.EntityName + " waiting time")
            { WarmupTime = warmup };

            resource.GrantedEvent.AddHandler((_, waited) => collector.Observe(waited));
            return collector;
        }

        /// <summary>
        /// SIM-126: the number of requests waiting for a unit of a process resource, time-weighted, as the
        /// queue length of a buffer is.
        /// </summary>
        public static TimeWeightedCollector ObserveResourceQueueLength(SimOpt.Simulation.Processes.Resource resource, double warmup = 0d)
        {
            var collector = new TimeWeightedCollector(
                resource.Model, QueueLength, () => resource.Waiting,
                id: resource.Identifier + ".stat." + QueueLength,
                name: resource.EntityName + " queue length")
            { WarmupTime = warmup };

            resource.WaitingChangedEvent.AddHandler((_, waiting) => collector.Record(waiting));
            return collector;
        }

        /// <summary>
        /// Records whether a stock is empty (1) or not (0), so the time average is the share of time
        /// it stood empty (SIM-127).
        /// </summary>
        public static TimeWeightedCollector ObserveStockEmpty(Stock stock, double warmup = 0d)
        {
            var collector = new TimeWeightedCollector(
                stock.Model, StockEmpty, () => stock.Level <= 0d ? 1d : 0d,
                id: stock.Identifier + ".stat." + StockEmpty,
                name: stock.EntityName + " empty")
            { WarmupTime = warmup };

            stock.LevelChangedEvent.AddHandler((_, level) => collector.Record(level <= 0d ? 1d : 0d));
            return collector;
        }

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

        /// <summary>
        /// SIM-65 step 5: the busy share of a pool of servers (a station of c servers, step 3), as a
        /// time-persistent statistic under <paramref name="id"/>. A pool of one measures what
        /// <see cref="ObserveUtilization"/> measures for its server.
        /// </summary>
        public static TimeWeightedCollector ObservePoolUtilization<TMaterial, TProduct, TData>(
            IReadOnlyList<Server<TMaterial, TProduct, TData>> pool, string id, double warmup = 0d)
            where TProduct : new()
        {
            if (pool == null || pool.Count == 0)
                throw new System.ArgumentException("A pool has at least one server.", nameof(pool));

            // A server sets its flag before it raises WorkingChangedEvent, so counting the flags in
            // the handler sees the change that raised it.
            double BusyShare()
            {
                int busy = 0;
                for (int i = 0; i < pool.Count; i++) if (pool[i].Working) busy++;
                return busy / (double)pool.Count;
            }

            var collector = new TimeWeightedCollector(
                pool[0].Model, Utilization, BusyShare,
                id: id + ".stat." + Utilization,
                name: id + " utilization")
            { WarmupTime = warmup };

            foreach (Server<TMaterial, TProduct, TData> server in pool)
                server.WorkingChangedEvent.AddHandler((_, _) => collector.Record(BusyShare()));
            return collector;
        }

        /// <summary>SIM-65 step 5: the items that reached the sink after the warm-up.</summary>
        public static CountCollector ObserveCount<T>(Sink<T> sink, double warmup = 0d)
            => ObserveArrivals(sink, ItemCount, perUnitTime: false, warmup);

        /// <summary>SIM-65 step 5: the items that reached the sink after the warm-up, per unit of time.</summary>
        public static CountCollector ObserveThroughput<T>(Sink<T> sink, double warmup = 0d)
            => ObserveArrivals(sink, Throughput, perUnitTime: true, warmup);

        private static CountCollector ObserveArrivals<T>(Sink<T> sink, string statistic, bool perUnitTime, double warmup)
        {
            var collector = new CountCollector(
                sink.Model, statistic, perUnitTime,
                id: sink.Identifier + ".stat." + statistic,
                name: sink.EntityName + " " + statistic)
            { WarmupTime = warmup };

            sink.ItemReceived.AddHandler((_, _) => collector.Increment());
            return collector;
        }

        /// <summary>
        /// SIM-65 step 5: the time from an item's creation at one of <paramref name="sources"/> to
        /// its arrival at one of <paramref name="sinks"/>, as an observation-based statistic under
        /// <paramref name="id"/>. It relies on the item that reaches the sink being the item the
        /// source made, which SIM-90 established for stations; an item made elsewhere is not timed.
        /// </summary>
        public static TallyCollector ObserveCycleTime<T>(IModel model, string id,
            IEnumerable<IItemSource<T>> sources, IEnumerable<Sink<T>> sinks, double warmup = 0d)
        {
            var collector = new TallyCollector(model, CycleTime,
                id: id + ".stat." + CycleTime,
                name: id + " cycle time")
            { WarmupTime = warmup };

            var stamps = new CreationStamps<T>(model);

            // Stamped before any other handler, as in ObserveWaitingTime: a source wired straight to
            // a sink delivers the item while its creation event is still being raised.
            foreach (IItemSource<T> source in sources)
                source.EntityCreatedEvent.AddHandler(
                    (_, item) => stamps.Stamp(item),
                    new Priority(type: PriorityType.LowLevelBeforeOthers));

            foreach (Sink<T> sink in sinks)
                sink.ItemReceived.AddHandler((_, item) =>
                {
                    if (stamps.Take(item, out double created)) collector.Observe(model.CurrentTime - created);
                });

            return collector;
        }

        /// <summary>
        /// The creation times of the items in the system. An entity, so Model.Reset empties it with
        /// everything else: items still in flight at the end of a replication would otherwise stay
        /// here for the life of the model.
        /// </summary>
        private sealed class CreationStamps<T> : Entity
        {
            private readonly Dictionary<T, double> created = new Dictionary<T, double>();

            public CreationStamps(IModel model) : base(model) { }

            public void Stamp(T item) => created[item] = Model.CurrentTime;

            public bool Take(T item, out double time) => created.Remove(item, out time);

            public override void Reset() => created.Clear();
        }
    }
}
