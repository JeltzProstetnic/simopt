using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Optimization;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
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

        /// <summary>
        /// SIM-66: tool output is read by a language model, not embedded in a web page, so quotes, '+' and
        /// arrows are written as themselves rather than as \u0027-style escapes that cost tokens.
        /// </summary>
        private static readonly JsonSerializerOptions Output = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public SimulationTools(ModelRegistry registry)
        {
            _registry = registry;
        }

        // ── create_model ──────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Creates a new simulation model from a JSON topology description. " +
            "Returns a model_id to be used in subsequent calls. " +
            "Nodes may have types: source, queue, station, delay, router, sink, process, resource, stock (buffer and server are accepted for queue and station). " +
            "Connections flow from source nodes toward sink nodes. " +
            "Source params: arrival, a distribution object such as {\"type\":\"exponential\",\"mean\":2} " +
            "(types: exponential, triangular, uniform, lognormal, gamma, constant, empirical, normal: truncated at zero), " +
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
            "Beside the item flow, connected to nothing: resource (capacity, whole units, default 1), units a process holds; " +
            "stock (initial, default 0; capacity, default unlimited), a level processes take from and put into. " +
            "A process node has no params but steps, a list run in order for every item it receives, any number at once: " +
            "{\"type\":\"delay\",\"duration\":<distribution>}, {\"type\":\"seize\",\"resource\":<id>}, " +
            "{\"type\":\"release\",\"resource\":<id>}, {\"type\":\"take\"|\"put\",\"stock\":<id>,\"amount\":<number or distribution>}, " +
            "each with an optional id (else <type>#<n>); an item leaving frees every unit it holds, step values are drawn when it " +
            "enters; a process is fed and feeds like a delay, never from a queue. " +
            "Metrics: an optional list [{id, kind, node?}] of what the model measures: wait_time and queue_length at a queue or " +
            "a resource, utilization at a station or a resource, level and empty_share at a stock, count and throughput at a " +
            "sink, cycle_time at a sink or, without a node, at all sinks. " +
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
                // SIM-66 (D7): the same checks as validate_model; nothing is registered on an error
                ValidationReport report = ModelValidator.Validate(topology, out ActiveModel? built);
                if (built == null)
                    return JsonSerializer.Serialize(new { error = string.Join("\n", report.Errors.Select(e => e.Line)), validation = report }, Output);
                string modelId = _registry.Register(built);
                return JsonSerializer.Serialize(new
                {
                    model_id = modelId,
                    name = topology.Name,
                    node_count = topology.Nodes.Count,
                    connection_count = topology.Connections.Count,
                    message = $"Model '{topology.Name}' created with id '{modelId}'.",
                    validation = report,
                }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── patch_model ───────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Changes an existing model by a list of operations, applied in order to a copy of its latest version, all or none. " +
            "Operations: {op:\"set\", path, value} where path is \"name\", \"seed\", \"<node>.<param>\" (value a number, a string " +
            "or a distribution object; null removes it) or \"<node>.<param>.<field>\" (one field of a distribution, e.g. " +
            "{\"op\":\"set\",\"path\":\"arrivals.arrival.mean\",\"value\":2.5}); a process's step field is " +
            "\"<process>.steps.<step id>.<field>[.<distribution field>]\" (a step without an id is <type>#<n>, e.g. " +
            "\"supplier.steps.put#1.up_to\"); {op:\"add_node\", value:{id,type,params}}; " +
            "{op:\"remove_node\", node} (its connections and metrics go with it); {op:\"set_connection\", from, to, weight?, outlet?} " +
            "(adds it, or sets its weight); {op:\"remove_connection\", from, to, outlet?}; {op:\"set_metric\", value:{id,kind,node?}}; " +
            "{op:\"remove_metric\", id}. The result is validated like create_model. If an operation is faulty or the result has " +
            "errors, nothing changes and every fault is returned; otherwise it becomes the next version, earlier versions stay " +
            "available to run_experiment. Returns {model_id, version, changes, validation}.")]
        public string PatchModel(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("The operations, applied in order")]
            List<PatchOperation> patch)
        {
            try
            {
                int version = _registry.Versions(model_id);
                var report = new ValidationReport();
                var changes = new List<string>();
                TopologyDefinition? patched = TopologyPatcher.Apply(_registry.Topology(model_id, version), patch ?? new(), report, changes);
                ActiveModel? built = null;
                if (patched != null) report = ModelValidator.Validate(patched, out built);
                if (built == null)
                    return JsonSerializer.Serialize(new
                    {
                        error = string.Join("\n", report.Errors.Select(e => e.Line)),
                        model_id,
                        version,
                        validation = report,
                    }, Output);
                int next = _registry.AddVersion(model_id, built, basedOn: version);
                return JsonSerializer.Serialize(new { model_id, version = next, changes, validation = report }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── run_experiment ────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Runs a model with replications and reports each declared metric as a mean with a 95% confidence interval " +
            "(a model without declared metrics is measured by cycle_time, throughput_<sink>, util_<station> and wait_<queue>). " +
            "scenarios: variants run in the same call, each {name, patch} with a patch as for patch_model; each is compared " +
            "with the unpatched \"baseline\" replication by replication (all share the seed, so unchanged parts draw the same " +
            "random numbers), giving vs_baseline {diff, ci95, significant}. flags say what went wrong: saturated stations, " +
            "items refused per replication, nothing finished, metrics without data. Returns provenance (version, " +
            "model_sha256 = sha256sum of the file save_model writes for that version, seed, duration, warmup, replications, " +
            "confidence, tool), assumed (every value the run used without being given it, call arguments by name and node " +
            "parameters by patch path, with the value used), each scenario's patch, and a one-line summary. Reproducible: " +
            "the same call gives the same result. record: a new .json file to write the run record to (the model document, " +
            "the call with every default resolved, the answer and record_sha256 over them); reproduce_run repeats it anywhere.")]
        public string RunExperiment(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("Simulated time per replication, warm-up included, in the model's time unit")]
            double duration,
            [Description("Replications per scenario, 1 to 1000 (default 10)")]
            int? replications = null,
            [Description("Time at the start of each replication excluded from every metric (default a tenth of duration)")]
            double? warmup = null,
            [Description("Metric ids to report (default all)")]
            List<string>? metrics = null,
            [Description("Variants to compare with the baseline: [{name, patch:[operations as for patch_model]}]")]
            List<ScenarioDefinition>? scenarios = null,
            [Description("The model version to run (default the latest)")]
            int? version = null,
            [Description("Base seed of the replications (default the model's seed)")]
            int? seed = null,
            [Description("A new .json file to write the run record to (default none); an existing file is never replaced")]
            string? record = null)
        {
            try
            {
                string? target = record == null ? null : RunRecordFile.Target(record);
                int run = version ?? _registry.Versions(model_id);
                TopologyDefinition topology = _registry.Topology(model_id, run);
                ExperimentResult result = ExperimentRunner.Run(model_id, run, topology, duration, replications, warmup,
                                                               metrics, scenarios, seed);
                if (target != null)
                    result.Record = new RecordWritten(target, RunRecordFile.Write(target, topology, metrics, scenarios, result));
                return JsonSerializer.Serialize(result, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── reproduce_run (SIM-86) ────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Repeats a run record written by run_experiment's record argument, on this server, from the record alone. " +
            "Reports record_intact (the record still hashes to its record_sha256), model_intact (its model still hashes to " +
            "the model_sha256 the answer named), identical (every recorded figure came out the same), the differences if " +
            "not, the tool versions then and now, and a summary.")]
        public string ReproduceRun(
            [Description("The run record (.json) to repeat")]
            string path)
        {
            try
            {
                return JsonSerializer.Serialize(RunRecordFile.Reproduce(path), Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── report_run (SIM-86 slice C) ───────────────────────────────────────

        [McpServerTool]
        [Description(
            "Renders a run record (run_experiment's or optimize's record argument) as a Markdown appendix for an expert " +
            "report, in English (\"en\") or German (\"de\"): provenance with the record and model hashes checked, the run's " +
            "settings, the results with their 95 % intervals, what the run assumed, what the model does not represent, and " +
            "how to repeat it. Runs nothing (reproduce_run repeats a record). Returns language, record_intact, model_intact " +
            "and markdown.")]
        public string ReportRun(
            [Description("The run record (.json) to render")]
            string path,
            [Description("\"en\" (default) or \"de\"")]
            string? language = null)
        {
            try
            {
                return JsonSerializer.Serialize(RunReport.Render(path, language), Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── optimize ──────────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Searches a model's decision variables for the best point that keeps the constraints. variables: " +
            "[{path, type: \"integer\"|\"real\", min, max, step?}], each path as for patch_model (e.g. \"exam.servers\"). " +
            "objective: {metric, direction: \"min\"|\"max\", constraints?: [{metric, op: \"<\"|\"<=\"|\">\"|\">=\", value}]}; " +
            "metric is a metric id or a variable's path (\"exam.servers\" with min = use as few as possible). Every point is " +
            "run with replications_per_eval replications at one seed, so points differ only where their settings do; a " +
            "constraint counts as kept only when the metric's whole 95% interval keeps it. strategy: \"auto\" (every point " +
            "when the space fits max_evaluations, else the evolutionary algorithm), \"grid\", \"random\", \"sa\" (simulated annealing), " +
            "\"ea\" (evolutionary algorithm) or \"pso\" (particle swarm); each stops at the budget. Returns the best point with its " +
            "metrics and intervals, violations if no point is feasible, the five best points, provenance (as run_experiment, " +
            "with model_sha256 and assumed; the decision variables are not listed as assumed) and a summary; record writes " +
            "a run record that reproduce_run repeats, as for run_experiment. " +
            "Example: fewest exam rooms keeping the mean wait under 15: variables [{\"path\":\"exam.servers\",\"type\":" +
            "\"integer\",\"min\":4,\"max\":12}], objective {\"metric\":\"exam.servers\",\"direction\":\"min\",\"constraints\":" +
            "[{\"metric\":\"wait_exam\",\"op\":\"<\",\"value\":15}]}.")]
        public string Optimize(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("Decision variables: [{path, type, min, max, step?}]")]
            List<DecisionVariable> variables,
            [Description("What to optimise: {metric, direction, constraints?}")]
            ObjectiveDefinition objective,
            [Description("Simulated time per replication, warm-up included, in the model's time unit")]
            double duration,
            [Description("Time at the start of each replication excluded from every metric (default a tenth of duration)")]
            double? warmup = null,
            [Description("Replications per evaluated point (default 5)")]
            int? replications_per_eval = null,
            [Description("Most points to evaluate (default 50)")]
            int max_evaluations = Optimizer.DefaultEvaluations,
            [Description("Wall-clock limit in seconds (default none)")]
            double? max_seconds = null,
            [Description("auto | grid | random | sa | ea | pso (default auto)")]
            string strategy = "auto",
            [Description("Seed of the replications and of the search (default the model's seed)")]
            int? seed = null,
            [Description("The model version to optimise (default the latest)")]
            int? version = null,
            [Description("A new .json file to write the run record to (default none), as for run_experiment; not with max_seconds")]
            string? record = null)
        {
            try
            {
                if (record != null && max_seconds != null)
                    return JsonSerializer.Serialize(new { error = "A search stopped by max_seconds depends on the machine's speed and " +
                        "cannot be repeated exactly, so it cannot be recorded; give max_evaluations instead." }, Output);
                string? target = record == null ? null : RunRecordFile.Target(record);
                int run = version ?? _registry.Versions(model_id);
                TopologyDefinition topology = _registry.Topology(model_id, run);
                OptimizationResult result = Optimizer.Run(model_id, run, topology, variables ?? new(), objective ?? new(), duration,
                    warmup, replications_per_eval, max_evaluations, max_seconds, strategy, seed);
                if (target != null)
                    result.Record = new RecordWritten(target, RunRecordFile.Write(target, topology, variables ?? new(),
                                                                                   objective ?? new(), max_evaluations, result));
                return JsonSerializer.Serialize(result, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── validate_model ────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Checks a topology without creating a model, and reports every problem at once. " +
            "Errors (each with code, node, message and suggested fix) stop a model from being built; warnings do not: " +
            "structure (no source or sink, unreachable nodes, dead ends, a node wired to two nodes without a router), " +
            "stations whose offered load is near or above 1, and what a short trial run saw (items refused, none finished). " +
            "Info lists every station's offered load and the trial's counts. create_model runs the same checks. " +
            "Returns {valid, errors, warnings, info}.")]
        public string ValidateModel(
            [Description("Topology JSON, as for create_model")]
            TopologyDefinition topology)
        {
            try
            {
                return JsonSerializer.Serialize(ModelValidator.Validate(topology), Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── save_model, load_model, export_model (SIM-67) ─────────────────────

        [McpServerTool]
        [Description(
            "Saves a version of a model to a file as its topology JSON (schema v1), the same document create_model " +
            "takes, so that it can be reopened with load_model, shared, and run by any later server. path must end " +
            "in .json; an existing file is kept unless overwrite is true. Returns {model_id, version, path, bytes}.")]
        public string SaveModel(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("The file to write, ending in .json")]
            string path,
            [Description("The version to save (default the latest)")]
            int? version = null,
            [Description("Replace an existing file (default false)")]
            bool overwrite = false)
        {
            try
            {
                int saved = version ?? _registry.Versions(model_id);
                string json = Saved(_registry.Topology(model_id, saved));
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return JsonSerializer.Serialize(new { error = $"path must end in .json, not \"{path}\"." }, Output);
                string full = Path.GetFullPath(path);
                if (File.Exists(full) && !overwrite)
                    return JsonSerializer.Serialize(new { error = $"{full} exists; pass overwrite: true to replace it." }, Output);
                File.WriteAllText(full, json);
                return JsonSerializer.Serialize(new { model_id, version = saved, path = full, bytes = new FileInfo(full).Length }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        [McpServerTool]
        [Description(
            "Opens a model saved with save_model, or any topology JSON file, and registers it exactly as create_model " +
            "would: the same checks, every fault reported at once, and the same answer with a new model_id.")]
        public string LoadModel(
            [Description("The .json file to open")]
            string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                if (!File.Exists(full)) return JsonSerializer.Serialize(new { error = $"No file {full}." }, Output);
                TopologyDefinition? topology;
                try
                {
                    topology = JsonSerializer.Deserialize<TopologyDefinition>(File.ReadAllText(full), McpJsonUtilities.DefaultOptions);
                }
                catch (JsonException ex)
                {
                    return JsonSerializer.Serialize(new { error = $"{full} is not topology JSON: {ex.Message}" }, Output);
                }
                return topology == null
                    ? JsonSerializer.Serialize(new { error = $"{full} holds no topology." }, Output)
                    : CreateModel(topology);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        [McpServerTool]
        [Description(
            "Returns a version of a model as its topology JSON (schema v1), for a client that does not share the " +
            "server's disk. Returns {model_id, version, topology}.")]
        public string ExportModel(
            [Description("The model_id returned by create_model")]
            string model_id,
            [Description("The version to export (default the latest)")]
            int? version = null)
        {
            try
            {
                int exported = version ?? _registry.Versions(model_id);
                JsonNode? topology = JsonNode.Parse(Saved(_registry.Topology(model_id, exported)));
                return JsonSerializer.Serialize(new { model_id, version = exported, topology }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        /// <summary>SIM-67 (D1): the topology as a schema v1 document; its SHA-256 names it in results (SIM-86).</summary>
        private static string Saved(TopologyDefinition topology) => ModelDocument.Write(topology);

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
                    return JsonSerializer.Serialize(new { error = "duration must be positive." }, Output);

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

                var resourceStats = new Dictionary<string, object>();
                foreach (var (id, res) in active.Resources)
                    resourceStats[id] = new { capacity = res.Capacity, in_use = res.InUse, waiting = res.Waiting };

                var stockStats = new Dictionary<string, object>();
                foreach (var (id, stk) in active.Stocks)
                    stockStats[id] = StockState(stk);

                var processStats = new Dictionary<string, object>();
                foreach (var (id, proc) in active.Processes)
                    processStats[id] = new { inside = proc.Count };

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
                        resources = resourceStats,
                        stocks = stockStats,
                        processes = processStats,
                    }
                }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        /// <summary>SIM-126: a stock's level and capacity (null when unlimited, which JSON cannot hold) and its waiting requests.</summary>
        private static object StockState(SimOpt.Simulation.Templates.Stock stock) => new
        {
            level = stock.Level,
            capacity = double.IsInfinity(stock.Capacity) ? (double?)null : stock.Capacity,
            waiting_takes = stock.WaitingTakers,
            waiting_puts = stock.WaitingPutters,
        };

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

                foreach (var (id, res) in active.Resources)
                    nodeStates.Add(new { id, type = "resource", capacity = res.Capacity, in_use = res.InUse, waiting = res.Waiting });

                foreach (var (id, proc) in active.Processes)
                    nodeStates.Add(new { id, type = "process", inside = proc.Count });

                foreach (var (id, stk) in active.Stocks)
                    nodeStates.Add(new
                    {
                        id, type = "stock", level = stk.Level,
                        capacity = double.IsInfinity(stk.Capacity) ? (double?)null : stk.Capacity,
                        waiting_takes = stk.WaitingTakers, waiting_puts = stk.WaitingPutters,
                    });

                return JsonSerializer.Serialize(new
                {
                    model_id,
                    model_name = active.Model.Name,
                    current_time = active.Model.CurrentTime,
                    state = active.Model.CurrentState.ToString(),
                    nodes = nodeStates,
                }, Output);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Output);
            }
        }

        // ── get_schema ────────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Returns the topology schema (version 1) that create_model reads: a JSON Schema of the document, " +
            "every node type with its parameters, every metric kind, every distribution family, and the limits: what a " +
            "model of this schema does not represent. " +
            "Check a topology against it before calling create_model. No input required.")]
        public string GetSchema()
        {
            // SIM-65 step 6 (D7): rendered from SchemaCatalog, which the builder reads too
            return JsonSerializer.Serialize(new
            {
                schema_version = SchemaCatalog.SchemaVersion,
                json_schema = SchemaCatalog.JsonSchema(),
                node_types = SchemaCatalog.NodeTypes.Select(t => new
                {
                    name = t.Name,
                    aliases = t.Aliases,
                    description = t.Description,
                    parameters = t.Parameters.Select(Describe),
                    fed_by = t.FedBy,
                    feeds = t.Feeds,
                }),
                step_types = SchemaCatalog.StepTypes.Select(t => new { name = t.Name, description = t.Description, fields = t.Fields.Select(Describe) }),
                metric_kinds = SchemaCatalog.MetricKinds.Select(k => new { name = k.Name, measured_at = k.MeasuredAt, description = k.Description }),
                distributions = SchemaCatalog.DistributionTypes,
                limits = SchemaCatalog.Limits,
            }, Output);
        }

        private static object Describe(ParameterSpec p) => new
        {
            name = p.Name,
            kind = p.Kind,
            required = p.Required,
            @default = p.Default,
            description = p.Description,
            choices = p.Choices,
            alternative = p.Alternative,
        };

        // ── list_templates ────────────────────────────────────────────────────

        [McpServerTool]
        [Description(
            "Returns descriptions of all available simulation building blocks " +
            "with their parameters, roles, and usage notes. " +
            "No input required.")]
        public string ListTemplates()
        {
            // SIM-65 step 6 (D1, D7): the v1 names only, rendered from SchemaCatalog
            var templates = SchemaCatalog.NodeTypes.Select(t => new
            {
                type = t.Name,
                description = t.Description,
                parameters = t.Parameters.Select(Describe),
                fed_by = t.FedBy,
                feeds = t.Feeds,
            });

            var notes = new[]
            {
                "All time values share one unit; you choose what it means (minutes, hours, etc.).",
                "Every station takes its work from a queue: source -> queue -> station -> sink is the smallest working line.",
                "A time is a distribution object such as {\"type\":\"triangular\",\"min\":20,\"mode\":30,\"max\":40}; families: " +
                    string.Join(", ", SchemaCatalog.DistributionTypes) + ".",
                "Declare what to measure in metrics: " +
                    string.Join(", ", SchemaCatalog.MetricKinds.Select(k => $"{k.Name} (at {k.MeasuredAt})")) + ".",
                "get_schema returns the whole document schema as JSON Schema.",
            };

            return JsonSerializer.Serialize(new { templates, usage_notes = notes }, new JsonSerializerOptions(Output)
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
            return JsonSerializer.Serialize(new { model_count = ids.Count, model_ids = ids }, Output);
        }
    }
}
