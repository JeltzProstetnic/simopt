using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;

namespace SimOpt.Visualization.Models;

/// <summary>
/// SIM-110 step 4 (Q-VIEW-PATH, <c>docs/plans/2026-11-25-sim-110-one-model-path.md</c>): the model the viewer
/// runs and draws. It is built from a schema v1 document by the schema builder, through the check the MCP
/// head's <c>validate_model</c> runs (<see cref="ModelValidator"/>), so the screen shows the model the MCP head
/// runs (UN-004, UN-025) and every random decision follows the model's seed (UN-009). It replaces
/// <c>SimulationModel</c>, the viewer's own builder, whose reject server drew from a seed that changed on every
/// process start (SIM-99).
/// </summary>
/// <remarks>
/// SIM-92 (G1): the engine's collectors measure and this class only reads them. A collector the document
/// declares is reused; the others are attached here: queue length and wait at every queue, the busy share at
/// every station, the count at every sink. Every collector of the model starts counting at the warm-up, as a
/// replication of <c>run_experiment</c> does, so before it a measure is null, which the viewer shows as "–".
/// </remarks>
public sealed class ViewerModel
{
    private readonly Dictionary<string, List<SimpleServer>> _stations = new();
    private readonly Dictionary<string, TimeWeightedCollector> _utilization = new();
    private readonly Dictionary<string, TimeWeightedCollector> _queueLength = new();
    private readonly Dictionary<string, TallyCollector> _wait = new();
    private readonly Dictionary<string, CountCollector> _arrivals = new();

    /// <summary>Builds the document's model at t = 0.</summary>
    /// <param name="endTime">Simulated time the run lasts, warm-up included.</param>
    /// <param name="warmup">Simulated time before which nothing is measured; 0 for none.</param>
    /// <exception cref="ModelValidationException">The document has errors; all of them are in its report.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The warm-up is negative, or not shorter than the run.</exception>
    public ViewerModel(VizDocument document, double endTime = 200.0, double warmup = 0.0)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (!(warmup >= 0d) || (warmup > 0d && warmup >= endTime))
            throw new ArgumentOutOfRangeException(nameof(warmup), warmup,
                $"The warm-up must be at least 0 and shorter than the run ({endTime}).");

        Document = document;
        EndTime = endTime;
        Warmup = warmup;
        Report = ModelValidator.Validate(document.Topology, out ActiveModel? built);
        Active = built ?? throw new ModelValidationException(Report);
        Drawn = VizSchema.Drawing(document);
        Observe();
    }

    /// <summary>The viewer's own topology description, converted by <see cref="VizSchema.ToSchema"/>.</summary>
    public static ViewerModel Of(VizTopology topology, double endTime = 200.0) => new(VizSchema.ToSchema(topology), endTime);

    public VizDocument Document { get; }

    /// <summary>The warnings the check found; a document with errors is not built.</summary>
    public ValidationReport Report { get; }

    /// <summary>The model as the schema builder built it.</summary>
    public ActiveModel Active { get; }

    /// <summary>What the viewer draws: every node of the document, queues and routers inserted by the conversion too.</summary>
    public VizTopology Drawn { get; }

    public double EndTime { get; }
    public double Warmup { get; }
    public double CurrentTime => Active.Model.CurrentTime;
    public bool IsFinished => CurrentTime >= EndTime;

    /// <summary>The run has a warm-up and has not reached its end, so nothing is measured yet.</summary>
    public bool WarmingUp => Warmup > 0d && CurrentTime < Warmup;

    public bool Step(double stepSize = 0.1)
    {
        if (IsFinished) return false;
        Active.Model.Step(stepSize);
        return !IsFinished;
    }

    /// <summary>The current state of every drawn node, for rendering.</summary>
    public List<NodeState> GetNodeStates()
    {
        var states = new List<NodeState>();
        foreach (VizNode node in Drawn.Nodes)
        {
            var state = new NodeState { Id = node.Id, Type = node.Type };
            if (Active.Buffers.TryGetValue(node.Id, out SimpleBuffer? buffer))
            {
                state.Count = buffer.Count;
                state.Capacity = buffer.MaxCapacity;
            }
            else if (_stations.TryGetValue(node.Id, out List<SimpleServer>? pool))
            {
                state.Working = pool.Any(s => s.Working);
                state.Damaged = pool.Any(s => s.Damaged);
            }
            else if (Active.Sinks.TryGetValue(node.Id, out SimpleSink? sink))
            {
                state.Count = sink.Count;
            }
            else if (Active.Routers.TryGetValue(node.Id, out SimpleRouter? router))
            {
                state.Count = router.Routed.Sum();   // the items it passed on, so the canvas sees them go through
            }
            states.Add(state);
        }
        return states;
    }

    /// <summary>
    /// What the engine has measured at a node so far; null where nothing is measured (no time has passed
    /// since the warm-up, no item has left the queue, the node is not of that kind), which the viewer shows as "–".
    /// </summary>
    public NodeMeasures Measure(string nodeId)
    {
        double now = CurrentTime;
        int? capacity = Active.Buffers.GetValueOrDefault(nodeId) is SimpleBuffer buffer && buffer.MaxCapacity < int.MaxValue
            ? buffer.MaxCapacity
            : null;
        return new NodeMeasures(
            Utilization: Known(_utilization.GetValueOrDefault(nodeId)?.TimeAverage(now)),
            MeanQueueLength: Known(_queueLength.GetValueOrDefault(nodeId)?.TimeAverage(now)),
            MeanWait: Known(_wait.GetValueOrDefault(nodeId)?.Mean),
            Count: _arrivals.TryGetValue(nodeId, out CountCollector? arrivals) ? (int)arrivals.Count : null,
            Capacity: capacity);
    }

    /// <summary>The stations, whose busy share the engine measures, by node id (a pool of servers is one).</summary>
    public IReadOnlyCollection<string> ServerIds => _utilization.Keys;

    /// <summary>The queues, inserted ones too.</summary>
    public IReadOnlyCollection<string> BufferIds => _queueLength.Keys;

    /// <summary>Items that reached any sink per unit of time since the warm-up; null until time passes after it.</summary>
    public double? Throughput
    {
        get
        {
            double span = CurrentTime - Warmup;
            if (_arrivals.Count == 0 || span <= 0d) return null;
            return _arrivals.Values.Sum(c => (double)c.Count) / span;
        }
    }

    /// <summary>The id of the station with the highest measured utilization (the first on a tie); null when none is measured.</summary>
    public string? Bottleneck
    {
        get
        {
            string? best = null;
            double bestUtilization = double.NegativeInfinity;
            foreach (string id in _utilization.Keys)
                if (Measure(id).Utilization is double u && u > bestUtilization) { best = id; bestUtilization = u; }
            return best;
        }
    }

    private void Observe()
    {
        Dictionary<string, IStatisticCollector> declared = Active.Model.FindEntities<IStatisticCollector>()
            .GroupBy(c => c.Identifier).ToDictionary(g => g.Key, g => g.First());
        T Collector<T>(string node, string kind, Func<T> attach) where T : class, IStatisticCollector =>
            declared.GetValueOrDefault($"{node}.stat.{kind}") as T ?? attach();

        foreach (NodeDefinition node in Active.Topology.Nodes)
        {
            string id = node.Id;
            switch (SchemaCatalog.Canonical(node.Type))
            {
                case "queue":
                {
                    SimpleBuffer queue = Active.Buffers[id];
                    _queueLength[id] = Collector(id, Instrumentation.QueueLength, () => Instrumentation.ObserveQueueLength(queue));
                    _wait[id] = Collector(id, Instrumentation.WaitTime, () => Instrumentation.ObserveWaitingTime(queue));
                    break;
                }
                case "station":
                {
                    // one server keeps the node id; a pool of c is "<id>#1" .. "<id>#c" (SIM-65 D2)
                    List<SimpleServer> pool = Active.Servers.Where(s => s.Key == id || s.Key.StartsWith(id + "#", StringComparison.Ordinal))
                                                            .Select(s => s.Value).ToList();
                    _stations[id] = pool;
                    _utilization[id] = Collector(id, Instrumentation.Utilization, () => Instrumentation.ObservePoolUtilization(pool, id));
                    break;
                }
                case "sink":
                {
                    SimpleSink sink = Active.Sinks[id];
                    _arrivals[id] = Collector(id, Instrumentation.ItemCount, () => Instrumentation.ObserveCount(sink));
                    break;
                }
            }
        }

        // as a replication of run_experiment does (ReplicationRunner): every collector starts at the warm-up
        foreach (IStatisticCollector collector in Active.Model.FindEntities<IStatisticCollector>())
            collector.WarmupTime = Warmup;
    }

    private static double? Known(double? value) =>
        value is double v && !double.IsNaN(v) && !double.IsInfinity(v) ? v : null;
}

/// <summary>
/// SIM-92: a node's measures as the engine's collectors hold them; null means not measured.
/// </summary>
public sealed record NodeMeasures(double? Utilization, double? MeanQueueLength, double? MeanWait, int? Count, int? Capacity);

/// <summary>
/// Snapshot of a single node's state for rendering.
/// </summary>
public class NodeState
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public int Count { get; set; }
    public int Capacity { get; set; }
    public bool Working { get; set; }
    public bool Damaged { get; set; }
}
