using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Simulation.Templates;

namespace SimOpt.McpServer.Tools
{
    /// <summary>
    /// MCP tools that expose the SimOpt simulation-optimization framework.
    /// All tools are pure (no shared mutable state beyond the registry, which is thread-safe).
    /// </summary>
    [McpServerToolType]
    public sealed class SimulationTools
    {
        private readonly ModelRegistry _registry;

        public SimulationTools(ModelRegistry registry)
        {
            _registry = registry;
        }

        // ── create_model ──────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Creates a new simulation model from a JSON topology description. " +
            "Returns a model_id to be used in subsequent calls. " +
            "Nodes may have types: source, queue, station, delay, router, sink (buffer and server are accepted for queue and station). " +
            "Connections flow from source nodes toward sink nodes. " +
            "Source params: arrival, a distribution object such as {\"type\":\"exponential\",\"mean\":2} " +
            "(types: exponential, triangular, uniform, lognormal, gamma, constant, empirical), " +
            "or mean_interval, an exponential mean (default 1.0). " +
            "Queue params: capacity (default unlimited), discipline \"fifo\" (default) or \"lifo\". " +
            "Station params: service, a distribution object, or service_time, an exponential mean (default 1.0); " +
            "servers, a whole number (default 1); a station with more than one server must be fed by a queue, " +
            "and its servers are reported as <id>#1 .. <id>#c. " +
            "Delay params: duration, a distribution object, or delay_time, an exponential mean; every item gets its own timer, " +
            "so a delay holds any number of items (it is fed by a source, station, delay or router, not by a queue). " +
            "Router params: policy, \"probabilistic\" (each connection leaving it may carry a weight, on all or none; " +
            "none splits equally) or \"shortest_queue\" (routes only to queues, to the one with the fewest items waiting " +
            "or in service at the station it feeds; a tie goes to the connection listed first); " +
            "a router is fed like a delay and feeds a queue, delay, sink or router. " +
            "Sink has no params. " +
            "Example: {\"name\":\"SQSS\",\"seed\":42," +
            "\"nodes\":[{\"id\":\"src\",\"type\":\"source\",\"params\":{\"mean_interval\":2.0}}," +
            "{\"id\":\"buf\",\"type\":\"buffer\",\"params\":{\"capacity\":10}}," +
            "{\"id\":\"srv\",\"type\":\"server\",\"params\":{\"service_time\":1.5}}," +
            "{\"id\":\"snk\",\"type\":\"sink\",\"params\":{}}]," +
            "\"connections\":[{\"from\":\"src\",\"to\":\"buf\"},{\"from\":\"buf\",\"to\":\"srv\"},{\"from\":\"srv\",\"to\":\"snk\"}]}")]
        public string CreateModel(
            [Description("Topology JSON: {name, seed, nodes:[{id,type,params}], connections:[{from,to}]}")]
            TopologyDefinition topology)
        {
            try
            {
                string modelId = _registry.Create(topology);
                return JsonSerializer.Serialize(new
                {
                    model_id = modelId,
                    name = topology.Name,
                    node_count = topology.Nodes.Count,
                    connection_count = topology.Connections.Count,
                    message = $"Model '{topology.Name}' created with id '{modelId}'."
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        // ── run_simulation ────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Runs an existing simulation model for the specified duration (simulation time units). " +
            "The model is reset before each run so results are independent and reproducible. " +
            "Returns final stats: elapsed simulation time, per-sink entity counts, " +
            "per-buffer queue levels, and per-server busy/idle state.")]
        public string RunSimulation(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("Simulation duration in time units (e.g. 1000.0)")]
            double duration)
        {
            try
            {
                if (duration <= 0)
                    return JsonSerializer.Serialize(new { error = "duration must be positive." });

                var active = _registry.Get(model_id);

                // Reset ensures reproducibility across repeated runs.
                active.Model.Reset();

                // Start all sources that have autoStartDelay set.
                // Sources with autoStartDelay=0 schedule their first arrival during Reset/Start.
                // Model.Run drives the event loop to the stop time.
                active.Model.Run(duration);

                // Collect stats
                var sinkStats = new Dictionary<string, int>();
                foreach (var (id, sink) in active.Sinks)
                    sinkStats[id] = sink.Count;

                var bufferStats = new Dictionary<string, int>();
                foreach (var (id, buf) in active.Buffers)
                    bufferStats[id] = buf.Count;

                var serverStats = new Dictionary<string, object>();
                foreach (var (id, srv) in active.Servers)
                    serverStats[id] = new { busy = srv.Busy, idle = srv.Idle, stopped = srv.Stopped };

                var delayStats = new Dictionary<string, object>();
                foreach (var (id, dly) in active.Delays)
                    delayStats[id] = new { in_flight = dly.Count, refused = dly.RefusedCount };

                var routerStats = new Dictionary<string, object>();
                foreach (var (id, rtr) in active.Routers)
                    routerStats[id] = new { routed = RoutedByTarget(rtr), refused = rtr.RefusedCount };

                var sourceStats = new Dictionary<string, object>();
                foreach (var (id, src) in active.Sources)
                    sourceStats[id] = new { running = src.Running };

                return JsonSerializer.Serialize(new
                {
                    model_id,
                    duration_requested = duration,
                    time = active.Model.CurrentTime,
                    events_processed = active.Model.EventCounter,
                    stats = new
                    {
                        sinks = sinkStats,
                        buffers = bufferStats,
                        servers = serverStats,
                        delays = delayStats,
                        routers = routerStats,
                        sources = sourceStats,
                    }
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        /// <summary>SIM-65 step 4: the items a router passed on, by target id.</summary>
        private static Dictionary<string, int> RoutedByTarget(SimpleRouter router)
        {
            var routed = new Dictionary<string, int>();
            for (int i = 0; i < router.Targets.Count; i++)
            {
                string target = (router.Targets[i] as SimOpt.Simulation.Engine.IEntity)?.Identifier ?? i.ToString();
                routed[target] = routed.GetValueOrDefault(target) + router.Routed[i];
            }
            return routed;
        }

        // ── get_status ────────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Returns the current state of all nodes in an existing model " +
            "(queue depths, server states, sink counts, source running flag). " +
            "Call after run_simulation to inspect results, or between runs for incremental analysis.")]
        public string GetStatus(
            [Description("The model_id returned by create_model")]
            string model_id)
        {
            try
            {
                var active = _registry.Get(model_id);

                var nodeStates = new List<object>();

                foreach (var (id, src) in active.Sources)
                    nodeStates.Add(new { id, type = "source", running = src.Running });

                foreach (var (id, buf) in active.Buffers)
                    nodeStates.Add(new { id, type = "buffer", queue_depth = buf.Count, is_full = buf.IsFull });

                foreach (var (id, srv) in active.Servers)
                    nodeStates.Add(new { id, type = "server", busy = srv.Busy, idle = srv.Idle, stopped = srv.Stopped });

                foreach (var (id, dly) in active.Delays)
                    nodeStates.Add(new { id, type = "delay", in_flight = dly.Count, refused = dly.RefusedCount });

                foreach (var (id, rtr) in active.Routers)
                    nodeStates.Add(new { id, type = "router", routed = RoutedByTarget(rtr), refused = rtr.RefusedCount });

                foreach (var (id, snk) in active.Sinks)
                    nodeStates.Add(new { id, type = "sink", total_received = snk.Count });

                return JsonSerializer.Serialize(new
                {
                    model_id,
                    model_name = active.Model.Name,
                    current_time = active.Model.CurrentTime,
                    state = active.Model.CurrentState.ToString(),
                    nodes = nodeStates,
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        // ── list_templates ────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Returns descriptions of all available simulation building blocks " +
            "with their parameters, roles, and usage notes. " +
            "No input required.")]
        public string ListTemplates()
        {
            var templates = new List<TemplateDescription>
            {
                new TemplateDescription(
                    Type: "source",
                    Description:
                        "Generates entities (customers, jobs, parts) at random intervals and pushes " +
                        "them to the first downstream node. Uses a negative-exponential inter-arrival " +
                        "distribution (Poisson arrivals). There must be at least one source per model.",
                    Parameters: new List<ParameterDescription>
                    {
                        new ParameterDescription("mean_interval", "double", "1.0",
                            "Mean time between successive entity arrivals. Smaller = more frequent arrivals.")
                    },
                    ConnectionRole: "upstream — connect its id in the 'from' field of a connection"),

                new TemplateDescription(
                    Type: "buffer",
                    Description:
                        "A FIFO queue that holds entities waiting for a downstream server. " +
                        "When capacity is exceeded, new arrivals are rejected (dropped). " +
                        "Typical use: place between a source and a server to absorb bursts.",
                    Parameters: new List<ParameterDescription>
                    {
                        new ParameterDescription("capacity", "int", "unlimited",
                            "Maximum number of entities the buffer can hold simultaneously.")
                    },
                    ConnectionRole: "intermediate — appears in both 'from' and 'to' fields"),

                new TemplateDescription(
                    Type: "server",
                    Description:
                        "Processes one entity at a time for a random service duration, then forwards " +
                        "the finished entity downstream. Uses a negative-exponential service-time " +
                        "distribution. AutoContinue is enabled: the server automatically pulls the " +
                        "next entity from its upstream buffer when idle.",
                    Parameters: new List<ParameterDescription>
                    {
                        new ParameterDescription("service_time", "double", "1.0",
                            "Mean time to process a single entity.")
                    },
                    ConnectionRole: "intermediate or terminal — connect upstream buffer/source via 'to'"),

                new TemplateDescription(
                    Type: "sink",
                    Description:
                        "Terminal node that absorbs entities and counts them. A sink does not forward " +
                        "entities further. Every model should have at least one sink to measure throughput.",
                    Parameters: new List<ParameterDescription>(),
                    ConnectionRole: "terminal — appears only in 'to' field, never in 'from'"),
            };

            var notes = new[]
            {
                "All time values share the same unit — you choose what a unit means (minutes, hours, etc.).",
                "Connections must form a valid DAG (directed acyclic graph) ending at sinks.",
                "A server must be connected to an upstream buffer or source via ConnectTo semantics.",
                "The model uses a negative-exponential distribution for both inter-arrival and service times, " +
                    "which models M/M/1 queuing systems when mean_interval=1/lambda and service_time=1/mu.",
                "For a basic utilization analysis: rho = service_time / mean_interval. " +
                    "If rho >= 1, the queue grows unboundedly — use this to explore stability thresholds.",
            };

            return JsonSerializer.Serialize(new { templates, usage_notes = notes }, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // ── list_models ───────────────────────────────────────────────────────

        [McpServerTool]
        [Description("Lists all model IDs currently registered in this server session.")]
        public string ListModels()
        {
            var ids = new List<string>(_registry.AllIds());
            return JsonSerializer.Serialize(new { model_count = ids.Count, model_ids = ids });
        }
    }

    // ── supporting DTOs for ListTemplates ────────────────────────────────────

    internal sealed record TemplateDescription(
        string Type,
        string Description,
        List<ParameterDescription> Parameters,
        string ConnectionRole);

    internal sealed record ParameterDescription(
        string Name,
        string Type,
        string DefaultValue,
        string Description);
}
