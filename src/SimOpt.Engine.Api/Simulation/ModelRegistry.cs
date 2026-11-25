using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Engine.Api.Models;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using SimOpt.Basics.Utilities;

namespace SimOpt.Engine.Api.Simulation
{
    /// <summary>
    /// Thread-safe registry of active simulation models.
    /// Each model is keyed by a GUID string issued at creation time.
    /// </summary>
    public sealed class ModelRegistry
    {
        /// <summary>
        /// SIM-66 (D8): a model's versions, oldest first, and the model built from the latest. Version n
        /// is <c>Versions[n - 1]</c>.
        /// </summary>
        private sealed class Entry
        {
            public Entry(ActiveModel active)
            {
                Versions.Add(active.Topology);
                Latest = active;
            }

            public List<TopologyDefinition> Versions { get; } = new();
            public ActiveModel Latest { get; set; }
        }

        private readonly ConcurrentDictionary<string, Entry> _models = new();

        /// <summary>
        /// Builds and registers a new model from the given topology definition.
        /// Returns the model ID that callers use for subsequent operations.
        /// </summary>
        /// <exception cref="ModelValidationException">The topology has errors; all of them are in its report.</exception>
        public string Create(TopologyDefinition topology) => Create(topology, new ValidationReport());

        /// <summary>As <see cref="Create(TopologyDefinition)"/>, filling <paramref name="report"/> with what was found.</summary>
        /// <exception cref="ModelValidationException">The topology has errors; all of them are in the report.</exception>
        public string Create(TopologyDefinition topology, ValidationReport report) =>
            Register(TryBuild(topology, report) ?? throw new ModelValidationException(report));

        /// <summary>SIM-66: registers a model already built (by <see cref="ModelValidator"/>) as version 1 and returns its id.</summary>
        public string Register(ActiveModel active)
        {
            string id = Guid.NewGuid().ToString("N");
            _models[id] = new Entry(active);
            return id;
        }

        /// <summary>
        /// Returns the active model for the given id (built from its latest version), or throws if not found.
        /// </summary>
        public ActiveModel Get(string modelId) => Find(modelId).Latest;

        /// <summary>SIM-66 (D8): how many versions the model has; the latest is this number.</summary>
        public int Versions(string modelId)
        {
            Entry entry = Find(modelId);
            lock (entry) return entry.Versions.Count;
        }

        /// <summary>SIM-66 (D8): the topology of a version of the model, the latest when none is named.</summary>
        public TopologyDefinition Topology(string modelId, int? version = null)
        {
            Entry entry = Find(modelId);
            lock (entry)
            {
                int count = entry.Versions.Count;
                int wanted = version ?? count;
                if (wanted < 1 || wanted > count)
                    throw new InvalidOperationException(
                        $"Model '{modelId}' has no version {wanted}; it has {count} (1 to {count}).");
                return entry.Versions[wanted - 1];
            }
        }

        /// <summary>
        /// SIM-66 (D8): makes <paramref name="built"/> the model's next version and returns its number.
        /// Refused when the model has changed since <paramref name="basedOn"/>, the version the change was
        /// made from, so two patches at once cannot silently drop one of them.
        /// </summary>
        public int AddVersion(string modelId, ActiveModel built, int basedOn)
        {
            Entry entry = Find(modelId);
            lock (entry)
            {
                if (entry.Versions.Count != basedOn)
                    throw new InvalidOperationException(
                        $"Model '{modelId}' changed while this patch was made (version {entry.Versions.Count}, not {basedOn}); patch it again.");
                entry.Versions.Add(built.Topology);
                entry.Latest = built;
                return entry.Versions.Count;
            }
        }

        /// <summary>
        /// Returns all registered model IDs.
        /// </summary>
        public IEnumerable<string> AllIds() => _models.Keys;

        private Entry Find(string modelId) =>
            _models.TryGetValue(modelId, out Entry? entry)
                ? entry
                : throw new InvalidOperationException($"Model '{modelId}' not found. Use create_model first.");

        // ── construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Builds the model a topology describes, or returns null when it cannot be built. Every error
        /// found goes into <paramref name="report"/>, not only the first (SIM-66, D1): each check
        /// throws a <see cref="TopologyException"/>, caught here per node, router, connection and
        /// metric. A node that failed is remembered, so a connection or metric at it adds no second,
        /// misleading "not found".
        /// </summary>
        public static ActiveModel? TryBuild(TopologyDefinition topology, ValidationReport report)
        {
            // SIM-65 step 6: a document written for another schema version is refused, not guessed at,
            // and nothing else in it is read.
            if (topology.SchemaVersion != null && topology.SchemaVersion.Trim() != SchemaCatalog.SchemaVersion)
            {
                report.Errors.Add(new ValidationIssue("SCHEMA_VERSION",
                    $"schema_version \"{topology.SchemaVersion}\" is not supported; this server reads version {SchemaCatalog.SchemaVersion}.",
                    Suggestion: $"Write \"schema_version\": \"{SchemaCatalog.SchemaVersion}\" or leave it out."));
                return null;
            }

            var failed = new HashSet<string>();
            void Fail(ValidationIssue issue, string? node)
            {
                report.Errors.Add(issue);
                if (node != null) failed.Add(node);
            }

            // Run a check; a fault it finds is recorded against the node and the build carries on.
            // Only the exceptions a refusal is made of are caught: anything else is a defect in the
            // builder and must surface as one. A faulty connection does not fail its node: the node's
            // other connections are still checked.
            void Check(string? node, string code, Action check, bool failsNode = true)
            {
                string? failing = failsNode ? node : null;
                try { check(); }
                catch (TopologyException e) { Fail(e.Issue, failing); }
                catch (Exception e) when (e is InvalidOperationException or ArgumentException)
                {
                    Fail(new ValidationIssue(code, e.Message, node), failing);
                }
            }

            CheckNodeIds(topology, Fail);
            TopologyLint.Check(topology, report);   // SIM-66 step 2 (D4): structure, from the graph alone

            // SIM-89: built at a deliberately different seed, then reset to the real one at the end
            // of this method. Model.Reset sets seedChange only when the seed actually differs
            // (Model.cs:1191), and StochasticEntity re-derives its per-node seed from SeedID only
            // while seedChange is set (StochasticEntity.cs:41-49). A model constructed at its final
            // seed would therefore never apply any SeedID, and every node would silently keep the
            // construction-order seed it happened to be handed.
            var model = new Model(topology.Name, unchecked(topology.Seed + 1), 0d);
            model.LoggingEnabled = false;

            var sources = new Dictionary<string, SimpleSource>();
            var buffers = new Dictionary<string, SimpleBuffer>();
            var servers = new Dictionary<string, SimpleServer>();               // by server id
            var stations = new Dictionary<string, List<SimpleServer>>();       // by node id
            var delays = new Dictionary<string, SimpleDelay>();
            var routers = new Dictionary<string, SimpleRouter>();
            var sinks = new Dictionary<string, SimpleSink>();
            var resources = new Dictionary<string, Resource>();   // SIM-126: beside the item flow
            var stocks = new Dictionary<string, Stock>();
            var processes = new Dictionary<string, ProcessNode>();

            int entityCounter = 0;

            // ── instantiate nodes ────────────────────────────────────────────

            foreach (NodeDefinition node in topology.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id) || failed.Contains(node.Id)) continue;   // CheckNodeIds reported it
                string label = Label(node);
                Check(node.Id, "INVALID_NODE", () =>
                {
                    if (node.Steps != null && CanonicalType(node) != "process")
                        throw new TopologyException("STEPS_NOT_READ", node.Id,
                            $"{label} '{node.Id}' has steps, and only a process node reads steps.",
                            $"Remove its steps, or make '{node.Id}' a process.");
                    switch (CanonicalType(node))
                    {
                        case "source":
                        {
                            var dist = TimeDistribution(node, label, "arrival", "mean_interval");
                            // SIM-142: one counter for every source of the model. Each source used to copy
                            // it, so two sources both made E1, E2, ... and the second one's first item
                            // threw on the duplicate id; a single source names its items as before.
                            Func<SimpleEntity> generator = () =>
                            {
                                entityCounter++;
                                return new SimpleEntity(model, $"E{entityCounter}", $"E{entityCounter}");
                            };

                            // SIM-62/SIM-89: the node's random stream is keyed to its stable ID, not to
                            // its position in the node list, so reordering the topology JSON cannot
                            // change any node's stream. The seedID must be supplied at construction —
                            // StochasticEntity seeds itself in its constructor and the SeedID setter
                            // refuses to run afterwards (StochasticEntity.cs:157-160).
                            var source = new SimpleSource(
                                model,
                                StableHash.Of(node.Id),
                                dist,
                                generator,
                                autoStartDelay: 0d,
                                id: node.Id,
                                name: node.Id);
                            sources[node.Id] = source;
                            break;
                        }

                        case "queue":
                        {
                            int capacity = Capacity(node, label);
                            var buffer = new SimpleBuffer(
                                model,
                                Discipline(node, label),
                                id: node.Id,
                                name: node.Id,
                                maxCapacity: capacity);
                            buffers[node.Id] = buffer;
                            break;
                        }

                        case "station":
                        {
                            int count = ServerCount(node, label);
                            // SIM-65 (D2): c servers are a pool of single servers behind the upstream
                            // queue, the construction AnalyticBatteryTests.RunMMc validates against
                            // Erlang-C. One server keeps the node id, so its seed, and every result built
                            // on it, stays where it was; pool members are "<id>#1" .. "<id>#c".
                            var pool = new List<SimpleServer>(count);
                            for (int k = 1; k <= count; k++)
                            {
                                string memberId = count == 1 ? node.Id : $"{node.Id}#{k}";
                                var server = new SimpleServer(
                                    model,
                                    StableHash.Of(memberId),   // see the source case above
                                    // a distribution object belongs to one random stream, so each
                                    // member gets its own
                                    TimeDistribution(node, label, "service", "service_time"),
                                    id: memberId,
                                    name: memberId,
                                    // SIM-90: pass the input entity through instead of manufacturing a
                                    // fresh SimpleEntity with a null Identifier. `Buffer.Put` keys on
                                    // Identifier, so with the default generator any server feeding a
                                    // downstream buffer threw — every multi-stage topology was unbuildable.
                                    // It also destroys entity identity, which makes an end-to-end cycle
                                    // time unmeasurable: the thing reaching the sink was not the thing that
                                    // left the source. For a flow model the part that leaves a station is
                                    // the part that entered it.
                                    createProduct: m => m[0]);
                                server.AutoContinue = true;
                                servers[memberId] = server;
                                pool.Add(server);
                            }
                            stations[node.Id] = pool;
                            break;
                        }

                        case "delay":
                        {
                            if (!node.Params.ContainsKey("duration") && !node.Params.ContainsKey("delay_time"))
                                throw new TopologyException("MISSING_PARAMETER", node.Id,
                                    $"{label} '{node.Id}' has no duration.",
                                    "Give duration, a distribution object such as {\"type\": \"constant\", \"value\": 5}, " +
                                    "or delay_time, an exponential mean.");
                            var dist = TimeDistribution(node, label, "duration", "delay_time");
                            // SIM-65/SIM-132: the schema's delay gives every item its own timer, so it has
                            // no capacity limit; with one place it would refuse every item that arrives
                            // while another is inside.
                            var delay = new SimpleDelay(
                                model,
                                dist,
                                StableHash.Of(node.Id),   // see the source case above
                                id: node.Id,
                                name: node.Id)
                            {
                                Capacity = int.MaxValue,
                            };
                            delays[node.Id] = delay;
                            break;
                        }

                        case "router":
                        {
                            // SIM-65 step 4 (D4): a router of its own (SimpleRouter), keyed to the node
                            // id like every other random node; SimpleRejectServer's routing is outside
                            // the seed system (SIM-99).
                            routers[node.Id] = new SimpleRouter(
                                model,
                                Policy(node, label),
                                StableHash.Of(node.Id),
                                id: node.Id,
                                name: node.Id);
                            break;
                        }

                        case "sink":
                        {
                            var sink = new SimpleSink(model, id: node.Id, name: node.Id);
                            sinks[node.Id] = sink;
                            break;
                        }

                        case "resource":
                        {
                            // SIM-126: no randomness of its own, so no seed ID; a process's seize step uses it
                            resources[node.Id] = new Resource(model, ResourceCapacity(node, label), id: node.Id, name: node.Id);
                            break;
                        }

                        case "stock":
                        {
                            var (initial, capacity) = StockLevels(node, label);
                            stocks[node.Id] = new Stock(model, initial, capacity, id: node.Id, name: node.Id);
                            break;
                        }

                        case "process":
                            break;   // SIM-126: built below, once every resource and stock its steps name exists

                        default:
                            throw new TopologyException("UNKNOWN_NODE_TYPE", node.Id,
                                $"Unknown node type '{node.Type}' for node '{node.Id}'.",
                                $"Valid types: {SchemaCatalog.Vocabulary()}.");
                    }
                });
            }

            // SIM-126: every fault in a process's steps is reported, not only the first
            var fed = topology.Connections.Select(c => c.To).ToHashSet();
            foreach (NodeDefinition node in topology.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id) || failed.Contains(node.Id) || CanonicalType(node) != "process") continue;
                var faults = new List<ValidationIssue>();
                ProcessNode? process = BuildProcess(model, node, resources, stocks, failed, faults);
                foreach (ValidationIssue fault in faults) Fail(fault, node.Id);
                if (process != null && faults.Count == 0)
                {
                    processes[node.Id] = process;
                    if (!fed.Contains(node.Id)) process.Repeat();   // rule 4: beside the flow, one instance
                }
                else failed.Add(node.Id);
            }

            // ── wire connections ─────────────────────────────────────────────
            // ConnectTo semantics: downstream.ConnectTo(upstream)
            //
            // Valid connection patterns:
            //   Source  → Buffer  : buffer.ConnectTo(source)    source is IItemSource<SimpleEntity>
            //   Source  → Sink    : sink.ConnectTo(source)      source is IItemSource<SimpleEntity>
            //   Buffer  → Server  : server.ConnectTo(buffer)    buffer is IItemBuffer<SimpleEntity>
            //   Server  → Sink    : sink.ConnectTo(server)      server is IItemSource<SimpleEntity>
            //   Source, Server or Delay → Delay : delay.ConnectTo(upstream)   pushed, one timer per item
            //   Server  → Buffer  : buffer.ConnectTo(server)    server is IItemSource<SimpleEntity>
            //   Delay   → Buffer, Delay or Sink : downstream.ConnectTo(delay)
            //
            // A station is a pool of servers (SIM-65 D2): every server of the pool is wired, and a
            // station of any size is fed only by a queue (D3).
            //
            // Buffer → Sink directly is not supported (Buffer is IItemBuffer, Sink needs IItemSource).
            // Use Buffer → Server → Sink instead. Buffer → Delay is refused: a delay takes every item at
            // once, so the queue would stay empty.

            // SIM-65 step 4: the station pools each queue feeds, filled while wiring and read while
            // running, so a shortest-queue router counts the items in service behind a queue too.
            var feeds = new Dictionary<string, List<List<SimpleServer>>>();
            int WaitingOrInService(SimpleBuffer queue)
            {
                int items = queue.Count;
                if (feeds.TryGetValue(queue.Identifier, out List<List<SimpleServer>>? pools))
                    foreach (List<SimpleServer> pool in pools)
                        foreach (SimpleServer server in pool)
                            if (server.Busy) items++;
                return items;
            }

            bool Exists(string id) =>
                sources.ContainsKey(id) || buffers.ContainsKey(id) || stations.ContainsKey(id) ||
                delays.ContainsKey(id) || routers.ContainsKey(id) || sinks.ContainsKey(id) || processes.ContainsKey(id);

            // A node that pushes what it releases (a source, a station's server, a delay, a router) into the
            // node downstream. False when the destination is not a node that takes a push.
            bool Push(IItemSource<SimpleEntity> upstream, string from, string to)
            {
                if (buffers.TryGetValue(to, out SimpleBuffer? buffer)) return buffer.ConnectTo(upstream);
                if (stations.TryGetValue(to, out List<SimpleServer>? pool))
                {
                    throw StationNeedsAQueue(from, to, pool.Count);   // D3, see there
                }
                if (sinks.TryGetValue(to, out SimpleSink? sink)) return sink.ConnectTo(upstream);
                if (delays.TryGetValue(to, out SimpleDelay? delay)) return delay.ConnectTo(upstream);
                if (routers.TryGetValue(to, out SimpleRouter? router)) return router.ConnectTo(upstream);
                if (processes.TryGetValue(to, out ProcessNode? process)) return process.ConnectTo(upstream);
                return false;
            }

            // SIM-65 step 4: a router routes along the connections leaving it. A probabilistic router
            // reads a weight on all of them or on none (an equal split); giving some is ambiguous.
            foreach (var (routerId, router) in routers)
            {
                Check(routerId, "INVALID_ROUTER", () =>
                {
                    List<ConnectionDefinition> leaving = topology.Connections.Where(c => c.From == routerId).ToList();
                    if (leaving.Count == 0)
                        throw new TopologyException("ROUTER_WITHOUT_TARGETS", routerId,
                            $"Router '{routerId}' has no connection leaving it.",
                            "Connect it to a queue, delay, sink or router.");
                    int weighted = leaving.Count(c => c.Weight.HasValue);
                    if (router.Policy == RoutingPolicy.Probabilistic && weighted > 0 && weighted < leaving.Count)
                        throw new TopologyException("PARTIAL_WEIGHTS", routerId,
                            $"Router '{routerId}': {weighted} of the {leaving.Count} connections leaving it have a weight.",
                            "Give a weight on every connection leaving it or on none, which splits equally.");
                });
            }

            var wired = new HashSet<(string, string, string?)>();
            foreach (ConnectionDefinition conn in topology.Connections)
            {
                // a connection at a node that failed would only repeat that node's error as "not found"
                if (failed.Contains(conn.From ?? string.Empty) || failed.Contains(conn.To ?? string.Empty)) continue;
                // a repeated connection is DUPLICATE_CONNECTION (TopologyLint); wiring it again would
                // subscribe the same handler twice, which the engine refuses with an exception
                if (!wired.Add((conn.From ?? string.Empty, conn.To ?? string.Empty, conn.Outlet))) continue;
                Check(conn.From, "INVALID_CONNECTION", () => Wire(conn), failsNode: false);
            }

            // SIM-126 (rule 3): every outlet a process names leads somewhere
            foreach (var (processId, process) in processes.Where(p => !p.Value.Repeats))   // a repeating one carries no item
                foreach (string outlet in process.Outlets)
                    if (!topology.Connections.Any(c => c.From == processId && c.Outlet == outlet))
                        Fail(new ValidationIssue("OUTLET_NOT_CONNECTED",
                            $"Process '{processId}': no connection leaves by its outlet '{outlet}', so the items that give up would vanish.",
                            processId, $"Add {{\"from\": \"{processId}\", \"to\": <node>, \"outlet\": \"{outlet}\"}}, to a sink for instance."),
                            null);

            void Wire(ConnectionDefinition conn)
            {
                string from = conn.From;
                string to = conn.To;

                bool connected = false;

                // SIM-126 (rule 3): an outlet is an exit of a process, leading where the connection says
                if (conn.Outlet != null)
                {
                    if (!processes.TryGetValue(from, out ProcessNode? withOutlets))
                        throw new TopologyException("OUTLET_NOT_READ", from,
                            $"Connection '{from}' → '{to}' names outlet '{conn.Outlet}', and only a process has outlets.",
                            "Remove the outlet, or connect from the process whose step names it.");
                    if (conn.Weight is double outletWeight)
                        throw new TopologyException("WEIGHT_NOT_READ", from,
                            $"Connection '{from}' → '{to}': weight {outletWeight} is read only on a connection leaving a router, not on an outlet.",
                            "Remove the weight.");
                    if (!withOutlets.Outlets.Contains(conn.Outlet))
                    {
                        List<string> named = withOutlets.Outlets.ToList();
                        throw new TopologyException("UNKNOWN_OUTLET", from,
                            $"Connection '{from}' → '{to}': no step of process '{from}' names the outlet '{conn.Outlet}'.",
                            named.Count > 0 ? $"Its outlets are {string.Join(", ", named.Select(o => $"'{o}'"))}."
                                            : "Give a seize or take step patience and this outlet, or remove the outlet here.");
                    }
                    if (!Push(withOutlets.Outlet(conn.Outlet), from, to))
                        throw new TopologyException("UNKNOWN_NODE", from,
                            $"Connection '{from}' → '{to}': destination '{to}' not found or connection type is unsupported.",
                            "See get_schema for what a process feeds.");
                    return;
                }

                // SIM-126: a resource or a stock is used by process steps; an item is never passed to one
                foreach (string end in new[] { from, to })
                {
                    if (resources.ContainsKey(end))
                        throw new TopologyException("NOT_ON_ITEM_FLOW", end,
                            $"Connection '{from}' → '{to}': resource '{end}' is not on the item flow; items are not passed to " +
                            "or from it, they hold its units.",
                            $"Remove the connection, and name '{end}' in the seize step of the process that uses it.");
                    if (stocks.ContainsKey(end))
                        throw new TopologyException("NOT_ON_ITEM_FLOW", end,
                            $"Connection '{from}' → '{to}': stock '{end}' is not on the item flow; items are not passed to " +
                            "or from it, processes take material from it and put material into it.",
                            $"Remove the connection, and name '{end}' in the take or put step of the process that uses it.");
                }

                // SIM-65: a weight is a probabilistic router's share for this edge. Any other node
                // reads none, and ignoring one would build a different model from the one described.
                if (conn.Weight is double weight)
                {
                    if (!routers.TryGetValue(from, out SimpleRouter? weightedRouter))
                        throw new TopologyException("WEIGHT_NOT_READ", from,
                            $"Connection '{from}' → '{to}': weight {weight} is read only on a connection leaving a router, " +
                            $"and '{from}' is not a router.", "Remove the weight.");
                    if (weightedRouter.Policy == RoutingPolicy.ShortestQueue)
                        throw new TopologyException("WEIGHT_NOT_READ", from,
                            $"Connection '{from}' → '{to}': router '{from}' uses shortest_queue, which reads no weight.",
                            "Remove the weight, or use the probabilistic policy.");
                    if (!(weight > 0d) || double.IsInfinity(weight))
                        throw new TopologyException("INVALID_WEIGHT", from,
                            $"Connection '{from}' → '{to}': weight must be a positive number, not {weight}.");
                }

                // ── from=Source ──────────────────────────────────────────────
                if (sources.TryGetValue(from, out SimpleSource? upSrc))
                {
                    connected = Push(upSrc, from, to);
                }
                // ── from=Buffer ──────────────────────────────────────────────
                else if (buffers.TryGetValue(from, out SimpleBuffer? upBuf))
                {
                    if (stations.TryGetValue(to, out List<SimpleServer>? pool))
                    {
                        foreach (SimpleServer member in pool) member.ConnectTo(upBuf);
                        if (!feeds.TryGetValue(from, out List<List<SimpleServer>>? fed)) feeds[from] = fed = new List<List<SimpleServer>>();
                        fed.Add(pool);
                        // SIM-89: a server *pulls* from its buffer, so connecting the two is only
                        // half the wiring — an idle server has nothing telling it that work has
                        // arrived. Without this handler the buffer fills and never drains, the run
                        // completes "successfully", and every sink reports zero. Every other
                        // builder in the repository does this (SimulationModel.cs:197,
                        // IvotionTopologyBuilder.cs:106, and both example programs); ModelRegistry
                        // was the only one that did not, which is why no MCP-built model has ever
                        // produced throughput.
                        // SIM-65 (D2): one arrival wakes exactly one idle server, the first, as in
                        // AnalyticBatteryTests.RunMMc; a busy server takes the next item itself.
                        upBuf.ItemReceivedEvent.AddHandler((_, _) => pool.FirstOrDefault(s => s.Idle)?.Start());
                        connected = true;
                    }
                    else if (sinks.TryGetValue(to, out _))
                    {
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': Buffer cannot connect directly to Sink.",
                            $"Insert a Server between them: Buffer → Server → Sink.");
                    }
                    else if (buffers.TryGetValue(to, out _))
                    {
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': Buffer cannot connect to another Buffer.",
                            $"Put a station between '{from}' and '{to}', or merge the two queues.");
                    }
                    else if (delays.ContainsKey(to))
                    {
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': a queue cannot feed a delay. A delay takes every item " +
                            $"at once, so the queue would stay empty.",
                            $"Connect the node that feeds '{from}' to '{to}' directly, or put a station between '{from}' and '{to}'.");
                    }
                    else if (processes.ContainsKey(to))
                    {
                        // SIM-126 (rule 6): a process takes every item at once and waits at its seize step
                        string feeders = string.Join(", ", topology.Connections.Where(c => c.To == from).Select(c => $"'{c.From}'"));
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': a queue cannot feed a process. A process takes every item at once, " +
                            $"so the queue would stay empty.",
                            $"Connect {(feeders.Length > 0 ? feeders : "its upstream node")} to '{to}' directly: items wait at " +
                            "its seize step.");
                    }
                    else if (routers.ContainsKey(to))
                    {
                        // SIM-65 step 4: a queue is taken from by its station; it pushes nothing on.
                        string feeders = string.Join(", ", topology.Connections.Where(c => c.To == from).Select(c => $"'{c.From}'"));
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': a queue cannot feed a router, because only a station takes " +
                            $"items from a queue.",
                            $"Route before the queue: connect {(feeders.Length > 0 ? feeders : "its upstream node")} " +
                            $"to '{to}' and '{to}' to '{from}'.");
                    }
                }
                // ── from=Station ─────────────────────────────────────────────
                else if (stations.TryGetValue(from, out List<SimpleServer>? upPool))
                {
                    foreach (SimpleServer member in upPool) connected = Push(member, from, to);
                }
                // ── from=Delay ───────────────────────────────────────────────
                else if (delays.TryGetValue(from, out SimpleDelay? upDly))
                {
                    connected = Push(upDly, from, to);
                }
                // ── from=Process (SIM-126): it feeds what a delay feeds ────────
                else if (processes.TryGetValue(from, out ProcessNode? upProc))
                {
                    connected = Push(upProc, from, to);
                }
                // ── from=Router ──────────────────────────────────────────────
                else if (routers.TryGetValue(from, out SimpleRouter? upRtr))
                {
                    double share = conn.Weight ?? 1d;
                    if (upRtr.Policy == RoutingPolicy.ShortestQueue && !buffers.ContainsKey(to) && Exists(to))
                        throw new TopologyException("INVALID_CONNECTION", from,
                            $"Connection '{from}' → '{to}': router '{from}' routes to the shortest queue, and '{to}' is " +
                            "not a queue.", "Route only to queues, or use the probabilistic policy.");
                    if (buffers.TryGetValue(to, out SimpleBuffer? downBuf4))
                    {
                        // join the shortest queue: the items waiting plus those in service behind it;
                        // counting only the waiting sends work to a busy server while another idles
                        upRtr.AddTarget(downBuf4, share, () => WaitingOrInService(downBuf4));
                        connected = true;
                    }
                    else if (stations.TryGetValue(to, out List<SimpleServer>? downPool))
                    {
                        throw StationNeedsAQueue(from, to, downPool.Count);
                    }
                    else if (delays.TryGetValue(to, out SimpleDelay? downDly4))
                    {
                        upRtr.AddTarget(downDly4, share);
                        connected = true;
                    }
                    else if (sinks.TryGetValue(to, out SimpleSink? downSnk4))
                    {
                        upRtr.AddTarget(downSnk4, share);
                        connected = true;
                    }
                    else if (routers.TryGetValue(to, out SimpleRouter? downRtr))
                    {
                        upRtr.AddTarget(downRtr, share);
                        connected = true;
                    }
                    else if (processes.TryGetValue(to, out ProcessNode? downProc))
                    {
                        upRtr.AddTarget(downProc, share);
                        connected = true;
                    }
                }
                // ── from=Sink (invalid) ──────────────────────────────────────
                else if (sinks.ContainsKey(from))
                {
                    throw new TopologyException("INVALID_CONNECTION", from,
                        $"Connection source '{from}' is a sink. Sinks are terminal nodes and cannot be connection sources.",
                        $"Remove the connection '{from}' → '{to}', or reverse it.");
                }
                else
                {
                    throw new TopologyException("UNKNOWN_NODE", null,
                        $"Connection source '{from}' not found in the topology.",
                        $"Declare a node '{from}', or connect from one of the declared nodes.");
                }

                if (!connected && !sinks.ContainsKey(from) && !sources.ContainsKey(from))
                {
                    throw new TopologyException("UNKNOWN_NODE", from,
                        $"Connection '{from}' → '{to}': destination '{to}' not found or connection type is unsupported.",
                        "See get_schema for what each node type feeds.");
                }
                else if (!connected)
                {
                    throw new TopologyException("UNKNOWN_NODE", from,
                        $"Connection '{from}' → '{to}': destination '{to}' not found in the topology.",
                        $"Declare a node '{to}', or connect to one of the declared nodes.");
                }
            }

            IReadOnlyDictionary<string, string> metrics = AttachMetrics(model, topology, sources, buffers, stations, sinks,
                resources, stocks, report, failed);
            if (report.Errors.Count > 0) return null;
            OfferedLoad.Check(topology, report);   // SIM-66 step 3 (D5)

            // Apply the real seed. This is the reset that makes every SeedID assignment above take
            // effect; from here on the per-node streams are a pure function of (topology.Seed,
            // node.Id) and survive both a process restart and a reordering of the node list.
            model.Reset(topology.Seed);

            return new ActiveModel(model, sources, buffers, servers, delays, routers, sinks, topology, metrics, resources, stocks,
                processes);
        }

        /// <summary>
        /// Creates a <b>configured but deliberately uninitialised</b> negative exponential
        /// distribution with the given mean.
        /// </summary>
        /// <remarks>
        /// SIM-89: this method used to call <c>dist.Initialize(seed)</c> as well. That made every
        /// call to <c>create_model</c> throw, because <see cref="SimOpt.Simulation.Engine.Random{T}"/>
        /// rejects an already-initialised distribution (Random.cs:75) — the wrapper owns
        /// initialisation, since it is what registers the generator with its seed source. The
        /// exception was swallowed by the tool layer's catch-all and returned as a JSON
        /// <c>error</c> field, so the MCP head looked like a working tool rejecting a bad model.
        /// Seeding is now done the engine's own way: see the <c>SeedID</c> assignments in
        /// <see cref="BuildModel"/>.
        /// </remarks>
        private static NegExponentialDistribution CreateNegExp(double mean)
        {
            var dist = new NegExponentialDistribution();
            dist.ConfigureMean(mean);
            return dist;
        }

        /// <summary>
        /// SIM-65 (D1): the v1 name of a node type; <c>buffer</c> and <c>server</c> are aliases of
        /// <c>queue</c> and <c>station</c>, so every older topology still builds.
        /// </summary>
        private static string CanonicalType(NodeDefinition node)
            => SchemaCatalog.Canonical(node.Type) ?? (node.Type ?? string.Empty).Trim().ToLowerInvariant();   // D7

        /// <summary>The node's type as the topology wrote it, capitalised, for error messages.</summary>
        private static string Label(NodeDefinition node)
        {
            string type = (node.Type ?? string.Empty).Trim().ToLowerInvariant();
            return type.Length == 0 ? "Node" : char.ToUpperInvariant(type[0]) + type.Substring(1);
        }

        /// <summary>
        /// SIM-65: a time given as a distribution object (<paramref name="distName"/>, schema v1) or as
        /// the older exponential mean (<paramref name="meanName"/>, default 1); giving both is refused.
        /// </summary>
        private static IDistribution<double> TimeDistribution(NodeDefinition node, string label, string distName, string meanName)
        {
            bool hasMean = TryGetNumber(node, label, meanName, out double mean);
            if (node.Params.TryGetValue(distName, out ParamValue? given))
            {
                if (hasMean)
                    throw new TopologyException("CONFLICTING_PARAMETERS", node.Id,
                        $"{label} '{node.Id}': give {distName} or {meanName}, not both.", $"Remove {meanName}.");
                if (given?.Distribution == null)
                    throw new TopologyException("INVALID_PARAMETER", node.Id,
                        $"{label} '{node.Id}': {distName} must be a distribution object such as " +
                        $"{{\"type\": \"exponential\", \"mean\": 2}}, not {given?.ToString() ?? "null"}.");
                try
                {
                    return DistributionFactory.Create(given.Distribution, $"node '{node.Id}', parameter '{distName}'");
                }
                catch (InvalidOperationException e)
                {
                    throw new TopologyException("INVALID_DISTRIBUTION", node.Id, e.Message);
                }
            }
            if (!hasMean) mean = 1.0;
            if (mean <= 0)
                throw new TopologyException("INVALID_PARAMETER", node.Id, $"{label} '{node.Id}': {meanName} must be positive.");
            return CreateNegExp(mean);
        }

        /// <summary>SIM-65: a queue's <c>discipline</c>, "fifo" (the default) or "lifo".</summary>
        private static QueueRule Discipline(NodeDefinition node, string label)
        {
            if (!node.Params.TryGetValue("discipline", out ParamValue? given)) return QueueRule.FIFO;
            switch (given?.Text?.Trim().ToLowerInvariant())
            {
                case "fifo": return QueueRule.FIFO;
                case "lifo": return QueueRule.LIFO;
                default:
                    throw new TopologyException("INVALID_PARAMETER", node.Id,
                        $"{label} '{node.Id}': discipline must be \"fifo\" or \"lifo\", not {given?.ToString() ?? "null"}.");
            }
        }

        /// <summary>
        /// SIM-65 step 5 (D6): attaches a collector for every declared metric and returns each
        /// metric's id mapped to its collector's identifier. Metrics of one kind at one node share a
        /// collector, because a collector's identifier is its node's and its kind's.
        /// SIM-66 (D1): a faulty metric is reported and the others are still attached; a metric at a node
        /// that failed is skipped, because that node's error already says what is wrong.
        /// </summary>
        private static IReadOnlyDictionary<string, string> AttachMetrics(Model model, TopologyDefinition topology,
            Dictionary<string, SimpleSource> sources, Dictionary<string, SimpleBuffer> buffers,
            Dictionary<string, List<SimpleServer>> stations, Dictionary<string, SimpleSink> sinks,
            Dictionary<string, Resource> resources, Dictionary<string, Stock> stocks,
            ValidationReport report, ISet<string> failed)
        {
            var keys = new Dictionary<string, string>();
            var attached = new HashSet<string>();
            var seen = new HashSet<string>();
            foreach (MetricDefinition metric in topology.Metrics)
            {
                try
                {
                    string? key = AttachMetric(metric);
                    if (key != null) keys[metric.Id.Trim()] = key;
                }
                catch (TopologyException e)
                {
                    report.Errors.Add(e.Issue);
                }
            }
            return keys;

            string? AttachMetric(MetricDefinition metric)
            {
                string id = (metric.Id ?? string.Empty).Trim();
                if (id.Length == 0)
                    throw new TopologyException("INVALID_METRIC", null, "A metric has no id.", "Give every metric a unique id.");
                if (!seen.Add(id))
                    throw new TopologyException("DUPLICATE_METRIC", null,
                        $"Metric '{id}' is declared twice; metric ids are unique.", "Rename or remove one of them.");
                string kind = (metric.Kind ?? string.Empty).Trim().ToLowerInvariant();
                if (!SchemaCatalog.MetricKinds.Any(k => k.Name == kind)) throw UnknownKind(id, metric.Kind);
                string? node = string.IsNullOrWhiteSpace(metric.Node) ? null : metric.Node.Trim();
                if (node != null && failed.Contains(node)) return null;

                // SIM-126: `others` are the nodes of a second type the kind is also measured at (a resource
                // beside a queue or a station), which the caller has already looked for
                T At<T>(Dictionary<string, T> nodes, string needs, IEnumerable<string>? others = null) where T : class
                {
                    if (node == null)
                        throw new TopologyException("INVALID_METRIC", null,
                            $"Metric '{id}': {kind} is measured at a {needs}; give its node.");
                    if (!nodes.TryGetValue(node, out T? found))
                        throw new TopologyException("INVALID_METRIC", null,
                            $"Metric '{id}': {kind} is measured at a {needs}, and '{node}' is not one in this topology.",
                            $"Measure it at one of: {string.Join(", ", nodes.Keys.Concat(others ?? Enumerable.Empty<string>()).Select(k => $"'{k}'"))}.");
                    return found;
                }

                Resource? AtResource() => node != null && resources.TryGetValue(node, out Resource? r) ? r : null;

                string key;
                switch (kind)
                {
                    case Instrumentation.WaitTime:
                    case Instrumentation.QueueLength:
                    {
                        if (AtResource() is Resource resource)
                        {
                            key = $"{resource.Identifier}.stat.{kind}";
                            if (attached.Add(key))
                            {
                                if (kind == Instrumentation.WaitTime) Instrumentation.ObserveResourceWaitingTime(resource);
                                else Instrumentation.ObserveResourceQueueLength(resource);
                            }
                            break;
                        }
                        SimpleBuffer queue = At(buffers, "queue or a resource", resources.Keys);
                        key = $"{queue.Identifier}.stat.{kind}";
                        if (attached.Add(key))
                        {
                            if (kind == Instrumentation.WaitTime) Instrumentation.ObserveWaitingTime(queue);
                            else Instrumentation.ObserveQueueLength(queue);
                        }
                        break;
                    }
                    case Instrumentation.Utilization:
                    {
                        if (AtResource() is Resource resource)
                        {
                            key = $"{resource.Identifier}.stat.{kind}";
                            if (attached.Add(key)) Instrumentation.ObserveResourceUtilization(resource);
                            break;
                        }
                        List<SimpleServer> pool = At(stations, "station or a resource", resources.Keys);
                        key = $"{node}.stat.{kind}";
                        if (attached.Add(key)) Instrumentation.ObservePoolUtilization(pool, node!);
                        break;
                    }
                    case Instrumentation.ItemCount:
                    case Instrumentation.Throughput:
                    {
                        SimpleSink sink = At(sinks, "sink");
                        key = $"{sink.Identifier}.stat.{kind}";
                        if (attached.Add(key))
                        {
                            if (kind == Instrumentation.ItemCount) Instrumentation.ObserveCount(sink);
                            else Instrumentation.ObserveThroughput(sink);
                        }
                        break;
                    }
                    case Instrumentation.Share:
                    {
                        SimpleSink sink = At(sinks, "sink");
                        key = $"{sink.Identifier}.stat.{kind}";
                        if (attached.Add(key)) Instrumentation.ObserveShare(sink, sinks.Values);
                        break;
                    }
                    case Instrumentation.CycleTime:
                    {
                        // without a node: from any source to any sink
                        IEnumerable<SimpleSink> at = node == null ? sinks.Values : new[] { At(sinks, "sink") };
                        string owner = node ?? AllSinks;
                        key = $"{owner}.stat.{kind}";
                        if (attached.Add(key))
                            Instrumentation.ObserveCycleTime(model, owner, sources.Values.Cast<IItemSource<SimpleEntity>>(), at);
                        break;
                    }
                    case SchemaCatalog.LevelKind:
                    case SchemaCatalog.EmptyShareKind:
                    {
                        // SIM-126: the collectors keep the engine's statistic names
                        Stock stock = At(stocks, "stock");
                        bool level = kind == SchemaCatalog.LevelKind;
                        key = $"{stock.Identifier}.stat.{(level ? Instrumentation.StockLevel : Instrumentation.StockEmpty)}";
                        if (attached.Add(key))
                        {
                            if (level) Instrumentation.ObserveStockLevel(stock);
                            else Instrumentation.ObserveStockEmpty(stock);
                        }
                        break;
                    }
                    default:
                        throw UnknownKind(id, metric.Kind);
                }
                return key;
            }

            static TopologyException UnknownKind(string id, string? kind) =>
                new TopologyException("INVALID_METRIC", null,
                    $"Metric '{id}': kind must be one of {string.Join(", ", SchemaCatalog.MetricKinds.Select(k => k.Name))}, " +
                    $"not \"{kind}\".");
        }

        /// <summary>The owner of a cycle time measured at every sink, in its collector's identifier.</summary>
        private const string AllSinks = "all_sinks";

        /// <summary>
        /// SIM-65 (D3, corrected 2026-11-25): every station takes its work from a queue. A push reaches
        /// every server of a pool at once, which is not a c-server station. A single server refuses a
        /// push (PushAllowed is false), and the builder used to ignore that refusal, so a source or
        /// station wired straight to a station built without error and delivered nothing (SIM-135).
        /// Allowing the push does not help: a busy server gathers what is pushed meanwhile and starts
        /// its next job with all of it as one batch, of which createProduct keeps the first item.
        /// </summary>
        private static TopologyException StationNeedsAQueue(string from, string to, int servers)
        {
            string pool = servers > 1 ? $"has {servers} servers and " : "";
            return new TopologyException("STATION_NEEDS_QUEUE", to,
                $"Connection '{from}' → '{to}': station '{to}' {pool}takes its work from a queue, and '{from}' is not one.",
                $"Insert a queue between '{from}' and '{to}'.");
        }

        /// <summary>SIM-65 step 4: a router's <c>policy</c>, "probabilistic" or "shortest_queue"; required.</summary>
        private static RoutingPolicy Policy(NodeDefinition node, string label)
        {
            node.Params.TryGetValue("policy", out ParamValue? given);
            switch (given?.Text?.Trim().ToLowerInvariant())
            {
                case "probabilistic": return RoutingPolicy.Probabilistic;
                case "shortest_queue": return RoutingPolicy.ShortestQueue;
                default:
                    throw new TopologyException(given == null ? "MISSING_PARAMETER" : "INVALID_PARAMETER", node.Id,
                        $"{label} '{node.Id}': policy must be \"probabilistic\" or \"shortest_queue\", " +
                        $"not {given?.ToString() ?? "missing"}.");
            }
        }

        /// <summary>SIM-65 (D2): a station's <c>servers</c>, a whole number from 1 (the default) to 10,000.</summary>
        private static int ServerCount(NodeDefinition node, string label)
        {
            if (!TryGetNumber(node, label, "servers", out double count)) return 1;
            if (count < 1 || count > MaxServers || count != Math.Floor(count))
                throw new TopologyException("INVALID_PARAMETER", node.Id,
                    $"{label} '{node.Id}': servers must be a whole number from 1 to {MaxServers:N0}, not {count}.");
            return (int)count;
        }

        /// <summary>SIM-66: a queue's <c>capacity</c>, a whole number of at least 1; unlimited when absent.</summary>
        private static int Capacity(NodeDefinition node, string label)
        {
            if (!TryGetNumber(node, label, "capacity", out double capacity)) return int.MaxValue;
            if (capacity < 1 || capacity > int.MaxValue || capacity != Math.Floor(capacity))
                throw new TopologyException("INVALID_PARAMETER", node.Id,
                    $"{label} '{node.Id}': capacity must be a whole number of at least 1, not {capacity}.",
                    "Leave it out for an unlimited queue.");
            return (int)capacity;
        }

        /// <summary>
        /// SIM-126 schema: a process node from its steps (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>).
        /// A step without an id is <c>&lt;type&gt;#&lt;n&gt;</c>, the n-th step of its type. Every faulty step
        /// goes into <paramref name="faults"/>; null when there is any.
        /// </summary>
        private static ProcessNode? BuildProcess(Model model, NodeDefinition node, Dictionary<string, Resource> resources,
            Dictionary<string, Stock> stocks, ISet<string> failed, List<ValidationIssue> faults)
        {
            if (node.Steps == null || node.Steps.Count == 0)
            {
                faults.Add(new ValidationIssue("MISSING_STEPS", $"Process '{node.Id}' has no steps.", node.Id,
                    "Give it steps, such as [{\"type\": \"seize\", \"resource\": \"<resource id>\"}, " +
                    "{\"type\": \"delay\", \"duration\": {\"type\": \"exponential\", \"mean\": 5}}]."));
                return null;
            }

            var steps = new List<ProcessStep>();
            IReadOnlyList<string> stepIds = SchemaCatalog.StepIds(node.Steps);
            var ids = new HashSet<string>();
            var held = new Dictionary<string, int>();
            bool blocked = false;   // a step names a resource or stock that failed: its own error says why
            for (int i = 0; i < node.Steps.Count; i++)
            {
                StepDefinition definition = node.Steps[i] ?? new StepDefinition();
                string type = (definition.Type ?? string.Empty).Trim().ToLowerInvariant();
                StepTypeSpec? spec = SchemaCatalog.StepTypes.FirstOrDefault(t => t.Name == type);
                if (spec == null)
                {
                    faults.Add(new ValidationIssue("UNKNOWN_STEP_TYPE",
                        $"Process '{node.Id}', step {i + 1}: '{definition.Type}' is not a step type.", node.Id,
                        $"Use one of: {string.Join(", ", SchemaCatalog.StepTypes.Select(t => t.Name))}."));
                    continue;
                }
                string id = stepIds[i];
                if (!ids.Add(id))
                {
                    faults.Add(new ValidationIssue("DUPLICATE_STEP_ID",
                        $"Process '{node.Id}': two steps have the id '{id}'.", node.Id, "Give each step its own id."));
                    continue;
                }
                var step = new StepReader(node.Id, id, spec, definition, faults);
                if (!step.FieldsKnown()) continue;
                switch (type)
                {
                    case "delay":
                        if (step.Distribution("duration", atMost: double.PositiveInfinity, of: null) is IDistribution<double> duration)
                            steps.Add(new DelayStep(id, duration));
                        break;
                    case "seize":
                    case "release":
                    {
                        string? name = step.NodeId("resource", resources.Keys, "resource", failed, ref blocked);
                        var (patience, outlet, patient) = type == "seize" ? step.Patience() : (null, null, true);
                        if (name == null || !patient) break;
                        Resource resource = resources[name];
                        if (type == "seize")
                        {
                            held[name] = held.GetValueOrDefault(name) + 1;
                            steps.Add(new SeizeStep(id, resource, patience, outlet));
                        }
                        else if (held.GetValueOrDefault(name) == 0)
                        {
                            faults.Add(new ValidationIssue("RELEASE_NOT_HELD",
                                $"Process '{node.Id}', step '{id}' releases '{name}', which the item does not hold at that step.",
                                node.Id, "Seize it in an earlier step, or remove the release: an item leaving the process frees " +
                                         "every unit it still holds."));
                        }
                        else
                        {
                            held[name]--;
                            steps.Add(new ReleaseStep(id, resource));
                        }
                        break;
                    }
                    case "wait_until":
                    {
                        string? name = step.NodeId("stock", stocks.Keys, "stock", failed, ref blocked);
                        var (atMost, atLeast, bounded) = step.Bound();
                        if (name == null || !bounded) break;
                        steps.Add(new WaitUntilStep(id, stocks[name], atMost, atLeast));
                        break;
                    }
                    case "put" when definition.Fields.ContainsKey("up_to") || !definition.Fields.ContainsKey("amount"):
                    {
                        string? name = step.NodeId("stock", stocks.Keys, "stock", failed, ref blocked);
                        double? level = step.UpTo(name == null ? null : stocks[name]);
                        if (name == null || level == null) break;
                        steps.Add(PutStep.UpTo(id, stocks[name], level.Value));
                        break;
                    }
                    case "take":
                    case "put":
                    {
                        string? name = step.NodeId("stock", stocks.Keys, "stock", failed, ref blocked);
                        var (patience, outlet, patient) = type == "take" ? step.Patience() : (null, null, true);
                        if (name == null || !patient) break;
                        Stock stock = stocks[name];
                        if (step.Amount(stock) is not { } amount) break;
                        if (type == "take")
                            steps.Add(amount.Fixed is double fixedTake
                                ? new TakeStep(id, stock, fixedTake, patience, outlet)
                                : new TakeStep(id, stock, amount.Drawn!, patience, outlet));
                        else
                            steps.Add(amount.Fixed is double fixedPut ? new PutStep(id, stock, fixedPut) : new PutStep(id, stock, amount.Drawn!));
                        break;
                    }
                }
            }
            if (faults.Count > 0 || blocked) return null;
            return new ProcessNode(model, StableHash.Of(node.Id), steps, id: node.Id, name: node.Id);
        }

        /// <summary>SIM-126: reads one step's fields against its type, recording what is wrong with the fix.</summary>
        private sealed class StepReader
        {
            private readonly string node, id;
            private readonly StepTypeSpec spec;
            private readonly StepDefinition step;
            private readonly List<ValidationIssue> faults;

            public StepReader(string node, string id, StepTypeSpec spec, StepDefinition step, List<ValidationIssue> faults)
            {
                this.node = node; this.id = id; this.spec = spec; this.step = step; this.faults = faults;
            }

            private string Where => $"Process '{node}', step '{id}'";

            private void Fault(string code, string message, string? suggestion = null) =>
                faults.Add(new ValidationIssue(code, $"{Where}: {message}", node, suggestion));

            /// <summary>False, with a fault per field, when the step carries a field its type does not read.</summary>
            public bool FieldsKnown()
            {
                bool known = true;
                foreach (string field in step.Fields.Keys.Where(f => spec.Fields.All(s => s.Name != f)))
                {
                    known = false;
                    Fault("UNKNOWN_STEP_FIELD", $"a {spec.Name} step does not read '{field}'.",
                        $"Its fields are {string.Join(", ", spec.Fields.Select(f => f.Name))}; remove '{field}'.");
                }
                return known;
            }

            private ParamValue? Required(string field)
            {
                if (step.Fields.TryGetValue(field, out ParamValue? value) && value != null) return value;
                ParameterSpec what = spec.Fields.First(f => f.Name == field);
                Fault("MISSING_STEP_FIELD", $"a {spec.Name} step needs '{field}'.", $"Give {field}: {what.Description}");
                return null;
            }

            /// <summary>The id of a node of the kind <paramref name="kind"/> that <paramref name="field"/> names, or null.</summary>
            public string? NodeId(string field, IEnumerable<string> declared, string kind, ISet<string> failed, ref bool blocked)
            {
                ParamValue? value = Required(field);
                if (value == null) return null;
                if (value.Text is not string name)
                {
                    Fault("INVALID_STEP_FIELD", $"{field} must be the id of a {kind} node, a string, not {value}.");
                    return null;
                }
                name = name.Trim();
                if (declared.Contains(name)) return name;
                if (failed.Contains(name)) { blocked = true; return null; }
                List<string> known = declared.ToList();
                Fault(kind == "resource" ? "UNKNOWN_RESOURCE" : "UNKNOWN_STOCK", $"'{name}' is not a {kind} of this model.",
                    known.Count > 0 ? $"Name one of: {string.Join(", ", known.Select(k => $"'{k}'"))}."
                                    : $"Declare a {kind} node '{name}'.");
                return null;
            }

            /// <summary>
            /// A distribution in <paramref name="field"/> that cannot draw below 0, nor above
            /// <paramref name="atMost"/> (a stock's capacity, named by <paramref name="of"/>); null when faulty.
            /// </summary>
            public IDistribution<double>? Distribution(string field, double atMost, string? of)
            {
                ParamValue? value = Required(field);
                if (value == null) return null;
                if (value.Distribution is not DistributionSpec given)
                {
                    string example = value.Number is double n
                        ? $"{{\"type\": \"constant\", \"value\": {n.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}"
                        : "{\"type\": \"exponential\", \"mean\": 5}";
                    Fault("INVALID_STEP_FIELD", $"{field} must be a distribution object, not {value}.", $"Write {example}.");
                    return null;
                }
                return Checked(field, given, atMost, of);
            }

            private IDistribution<double>? Checked(string field, DistributionSpec given, double atMost, string? of)
            {
                // the bounds first: a family refuses a negative minimum with an argument exception that
                // does not say which field it came from
                (double lower, double upper) = (0d, 0d);
                try { (lower, upper) = DistributionFactory.Bounds(given); }
                catch (InvalidOperationException) { }   // a field is missing; Create says which
                IDistribution<double>? distribution = null;
                if (lower >= 0d)
                {
                    try
                    {
                        distribution = DistributionFactory.Create(given, $"process '{node}', step '{id}', field '{field}'");
                    }
                    catch (Exception e) when (e is InvalidOperationException or ArgumentException)
                    {
                        Fault("INVALID_STEP_FIELD", e.Message);
                        return null;
                    }
                }
                if (lower < 0d)
                {
                    Fault("INVALID_STEP_FIELD", $"{field} can draw below 0 (its least value is {Fmt(lower)}).",
                        "Give a distribution that starts at 0 or above, or shift it up.");
                    return null;
                }
                if (upper > atMost)
                {
                    Fault("INVALID_STEP_FIELD",
                        $"{field} can draw more than the capacity {Fmt(atMost)} of {of}, so such a request could never be served.",
                        "Give a bounded distribution (uniform, triangular, empirical, constant) within the capacity, or raise it.");
                    return null;
                }
                return distribution;
            }

            /// <summary>
            /// Slice 3: a seize's or take's patience and outlet, which come together or not at all; false
            /// (with a fault) when only one is given or either is faulty.
            /// </summary>
            public (IDistribution<double>? Patience, string? Outlet, bool Ok) Patience()
            {
                bool hasPatience = step.Fields.ContainsKey("patience"), hasOutlet = step.Fields.ContainsKey("outlet");
                if (!hasPatience && !hasOutlet) return (null, null, true);
                if (hasPatience && !hasOutlet)
                {
                    Fault("PATIENCE_WITHOUT_OUTLET", "it has a patience and no outlet, so an item that gives up would have nowhere to go.",
                        "Give outlet, a name such as \"gave_up\", and a connection {\"from\": \"" + node + "\", \"to\": <node>, \"outlet\": <name>}.");
                    return (null, null, false);
                }
                if (!hasPatience)
                {
                    Fault("OUTLET_WITHOUT_PATIENCE", "it has an outlet and no patience, so no item would ever leave by it.",
                        "Give patience, a distribution of how long an item waits here, or remove the outlet.");
                    return (null, null, false);
                }
                IDistribution<double>? patience = Distribution("patience", atMost: double.PositiveInfinity, of: null);
                if (step.Fields["outlet"].Text is not string outlet || outlet.Trim().Length == 0)
                {
                    Fault("INVALID_STEP_FIELD", $"outlet must be a name, a string, not {step.Fields["outlet"]}.");
                    return (null, null, false);
                }
                return patience == null ? (null, null, false) : (patience, outlet.Trim(), true);
            }

            /// <summary>Slice 4: a wait_until's bound, at_most or at_least, exactly one of them and a number.</summary>
            public (double? AtMost, double? AtLeast, bool Ok) Bound()
            {
                bool most = step.Fields.ContainsKey("at_most"), least = step.Fields.ContainsKey("at_least");
                if (most == least)
                {
                    if (most)
                        Fault("CONFLICTING_STEP_FIELDS", "give 'at_most' or 'at_least', not both.", "Remove one of them.");
                    else
                        Fault("MISSING_STEP_FIELD", "a wait_until step needs 'at_most' or 'at_least'.",
                            "Give at_most, the level to wait for at or below (a reorder point), or at_least.");
                    return (null, null, false);
                }
                string field = most ? "at_most" : "at_least";
                if (step.Fields[field].Number is not double bound || double.IsInfinity(bound))
                {
                    Fault("INVALID_STEP_FIELD", $"{field} must be a number, not {step.Fields[field]}.");
                    return (null, null, false);
                }
                return most ? (bound, null, true) : (null, bound, true);
            }

            /// <summary>
            /// Slice 4: a put's up_to, a level above 0 within <paramref name="stock"/>'s capacity, given
            /// instead of an amount; null with a fault otherwise.
            /// </summary>
            public double? UpTo(Stock? stock)
            {
                bool upTo = step.Fields.ContainsKey("up_to"), amount = step.Fields.ContainsKey("amount");
                if (upTo && amount)
                {
                    Fault("CONFLICTING_STEP_FIELDS", "give 'amount' or 'up_to', not both.", "Remove one of them.");
                    return null;
                }
                if (!upTo)
                {
                    Fault("MISSING_STEP_FIELD", "a put step needs 'amount' or 'up_to'.",
                        "Give amount, what each put adds, or up_to, the level it fills the stock to.");
                    return null;
                }
                if (step.Fields["up_to"].Number is not double level || !(level > 0d) || double.IsInfinity(level))
                {
                    Fault("INVALID_STEP_FIELD", $"up_to must be a level above 0, not {step.Fields["up_to"]}.");
                    return null;
                }
                if (stock == null) return null;
                if (level > stock.Capacity)
                {
                    Fault("INVALID_STEP_FIELD",
                        $"up_to {Fmt(level)} is more than the capacity {Fmt(stock.Capacity)} of stock '{stock.Identifier}'.",
                        "Lower it to the capacity, or raise the capacity.");
                    return null;
                }
                return level;
            }

            /// <summary>A take or put amount: a positive number within the stock's capacity, or a distribution that stays within it.</summary>
            public (double? Fixed, IDistribution<double>? Drawn)? Amount(Stock stock)
            {
                ParamValue? value = Required("amount");
                if (value == null) return null;
                string of = $"stock '{stock.Identifier}'";
                if (value.Number is double amount)
                {
                    if (!(amount > 0d) || double.IsInfinity(amount))
                    {
                        Fault("INVALID_STEP_FIELD", $"amount must be a positive number, not {Fmt(amount)}.");
                        return null;
                    }
                    if (amount > stock.Capacity)
                    {
                        Fault("INVALID_STEP_FIELD",
                            $"amount {Fmt(amount)} is more than the capacity {Fmt(stock.Capacity)} of {of}, so it could never be served.",
                            "Lower the amount, or raise the stock's capacity.");
                        return null;
                    }
                    return (amount, null);
                }
                if (value.Distribution is DistributionSpec given)
                    return Checked("amount", given, stock.Capacity, of) is IDistribution<double> drawn ? (null, drawn) : null;
                Fault("INVALID_STEP_FIELD", $"amount must be a positive number or a distribution, not {value}.");
                return null;
            }

            private static string Fmt(double value) => value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>SIM-126: a resource's <c>capacity</c>, a whole number from 1 (the default) to 10,000.</summary>
        private static int ResourceCapacity(NodeDefinition node, string label)
        {
            if (!TryGetNumber(node, label, "capacity", out double capacity)) return 1;
            if (capacity < 1 || capacity > MaxServers || capacity != Math.Floor(capacity))
                throw new TopologyException("INVALID_PARAMETER", node.Id,
                    $"{label} '{node.Id}': capacity must be a whole number of units from 1 to {MaxServers:N0}, not {capacity}.");
            return (int)capacity;
        }

        /// <summary>
        /// SIM-126: a stock's <c>initial</c> level (default 0) and <c>capacity</c> (default unlimited): the
        /// capacity positive, the initial level from 0 to the capacity.
        /// </summary>
        private static (double Initial, double Capacity) StockLevels(NodeDefinition node, string label)
        {
            double capacity = double.PositiveInfinity;
            if (TryGetNumber(node, label, "capacity", out double given))
            {
                if (!(given > 0d) || double.IsInfinity(given))
                    throw new TopologyException("INVALID_PARAMETER", node.Id,
                        $"{label} '{node.Id}': capacity must be a positive number, not {given}.",
                        "Leave it out for an unlimited stock.");
                capacity = given;
            }
            if (!TryGetNumber(node, label, "initial", out double initial)) return (0d, capacity);
            if (!(initial >= 0d) || double.IsInfinity(initial))
                throw new TopologyException("INVALID_PARAMETER", node.Id,
                    $"{label} '{node.Id}': initial must be a number of at least 0, not {initial}.");
            if (initial > capacity)
                throw new TopologyException("INVALID_PARAMETER", node.Id,
                    $"{label} '{node.Id}': initial {initial} is more than the capacity {capacity}.",
                    "Lower the initial level, or raise the capacity.");
            return (initial, capacity);
        }

        /// <summary>
        /// SIM-66: every node has an id of its own. A missing id cannot be connected to, and a second
        /// node with the same id used to replace the first without a word. Both are reported, and the
        /// nodes concerned are not built.
        /// </summary>
        private static void CheckNodeIds(TopologyDefinition topology, Action<ValidationIssue, string?> fail)
        {
            for (int i = 0; i < topology.Nodes.Count; i++)
            {
                NodeDefinition node = topology.Nodes[i];
                if (string.IsNullOrWhiteSpace(node.Id))
                    fail(new ValidationIssue("MISSING_NODE_ID", $"Node {i + 1} ({node.Type}) has no id.",
                        Suggestion: "Give every node a unique id."), null);
            }
            foreach (IGrouping<string, NodeDefinition> twins in topology.Nodes
                         .Where(n => !string.IsNullOrWhiteSpace(n.Id)).GroupBy(n => n.Id).Where(g => g.Count() > 1))
                fail(new ValidationIssue("DUPLICATE_NODE_ID", $"Node id '{twins.Key}' is used by {twins.Count()} nodes.",
                    twins.Key, "Give each node its own id."), twins.Key);
        }

        private const int MaxServers = 10_000;

        /// <summary>
        /// SIM-65: reads a parameter that must be a number. False when it is absent; a string or a
        /// distribution where a number belongs is refused with the node and the parameter named.
        /// </summary>
        private static bool TryGetNumber(NodeDefinition node, string kind, string name, out double value)
        {
            value = 0d;
            if (!node.Params.TryGetValue(name, out ParamValue? param)) return false;
            if (param?.Number is double number)
            {
                value = number;
                return true;
            }
            throw new TopologyException("INVALID_PARAMETER", node.Id,
                $"{kind} '{node.Id}': {name} must be a number, not {param?.ToString() ?? "null"}.");
        }
    }
}
