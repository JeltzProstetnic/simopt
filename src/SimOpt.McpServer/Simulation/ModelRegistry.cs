using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.McpServer.Models;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using SimOpt.Basics.Utilities;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// Thread-safe registry of active simulation models.
    /// Each model is keyed by a GUID string issued at creation time.
    /// </summary>
    public sealed class ModelRegistry
    {
        private readonly ConcurrentDictionary<string, ActiveModel> _models = new();

        /// <summary>
        /// Builds and registers a new model from the given topology definition.
        /// Returns the model ID that callers use for subsequent operations.
        /// </summary>
        public string Create(TopologyDefinition topology)
        {
            string id = Guid.NewGuid().ToString("N");
            ActiveModel active = BuildModel(id, topology);
            _models[id] = active;
            return id;
        }

        /// <summary>
        /// Returns the active model for the given id, or throws if not found.
        /// </summary>
        public ActiveModel Get(string modelId)
        {
            if (!_models.TryGetValue(modelId, out ActiveModel? active))
                throw new InvalidOperationException($"Model '{modelId}' not found. Use create_model first.");
            return active;
        }

        /// <summary>
        /// Returns all registered model IDs.
        /// </summary>
        public IEnumerable<string> AllIds() => _models.Keys;

        // ── construction ─────────────────────────────────────────────────────

        private static ActiveModel BuildModel(string registryId, TopologyDefinition topology)
        {
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
            var sinks = new Dictionary<string, SimpleSink>();

            int entityCounter = 0;

            // ── instantiate nodes ────────────────────────────────────────────

            foreach (NodeDefinition node in topology.Nodes)
            {
                string label = Label(node);
                switch (CanonicalType(node))
                {
                    case "source":
                    {
                        var dist = TimeDistribution(node, label, "arrival", "mean_interval");
                        int localCounter = entityCounter; // closure capture
                        Func<SimpleEntity> generator = () =>
                        {
                            localCounter++;
                            entityCounter = localCounter;
                            return new SimpleEntity(model, $"E{localCounter}", $"E{localCounter}");
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
                        int capacity = TryGetNumber(node, label, "capacity", out double cap)
                            ? (int)cap
                            : int.MaxValue;
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
                            throw new InvalidOperationException(
                                $"{label} '{node.Id}': give duration, a distribution object such as " +
                                "{\"type\": \"constant\", \"value\": 5}, or delay_time, an exponential mean.");
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

                    case "sink":
                    {
                        var sink = new SimpleSink(model, id: node.Id, name: node.Id);
                        sinks[node.Id] = sink;
                        break;
                    }

                    default:
                        throw new InvalidOperationException(
                            $"Unknown node type '{node.Type}' for node '{node.Id}'. Valid types: source, queue, station, delay, sink " +
                            "(buffer and server are accepted for queue and station).");
                }
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

            // A node that pushes what it releases (a source, a station's server, a delay) into the
            // node downstream. False when the destination is not a node that takes a push.
            bool Push(IItemSource<SimpleEntity> upstream, string from, string to)
            {
                if (buffers.TryGetValue(to, out SimpleBuffer? buffer)) return buffer.ConnectTo(upstream);
                if (stations.TryGetValue(to, out List<SimpleServer>? pool))
                {
                    // SIM-65 (D3, corrected 2026-11-25): every station takes its work from a queue.
                    // A push reaches every server of a pool at once, which is not a c-server station.
                    // A single server refuses a push (PushAllowed is false), and the builder used to
                    // ignore that refusal, so a source or station wired straight to a station built
                    // without error and delivered nothing. Allowing the push does not help: a busy
                    // server gathers what is pushed meanwhile and starts its next job with all of it
                    // as one batch, of which createProduct keeps the first item.
                    string servers = pool.Count > 1 ? $"has {pool.Count} servers and " : "";
                    throw new InvalidOperationException(
                        $"Connection '{from}' → '{to}': station '{to}' {servers}takes its work from a queue, " +
                        $"and '{from}' is not one. Insert a queue between '{from}' and '{to}'.");
                }
                if (sinks.TryGetValue(to, out SimpleSink? sink)) return sink.ConnectTo(upstream);
                if (delays.TryGetValue(to, out SimpleDelay? delay)) return delay.ConnectTo(upstream);
                return false;
            }

            foreach (ConnectionDefinition conn in topology.Connections)
            {
                string from = conn.From;
                string to = conn.To;

                bool connected = false;

                // SIM-65: a weight is a router's share for this edge. Routers arrive with step 4;
                // until then no node reads a weight, and ignoring one would build a different model
                // from the one described.
                if (conn.Weight is double weight)
                    throw new InvalidOperationException(
                        $"Connection '{from}' → '{to}': weight {weight} is read only on a connection leaving a router, " +
                        $"and '{from}' is not a router. Remove the weight.");

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
                        throw new InvalidOperationException(
                            $"Connection '{from}' → '{to}': Buffer cannot connect directly to Sink. " +
                            $"Insert a Server between them: Buffer → Server → Sink.");
                    }
                    else if (buffers.TryGetValue(to, out _))
                    {
                        throw new InvalidOperationException(
                            $"Connection '{from}' → '{to}': Buffer cannot connect to another Buffer.");
                    }
                    else if (delays.ContainsKey(to))
                    {
                        throw new InvalidOperationException(
                            $"Connection '{from}' → '{to}': a queue cannot feed a delay. A delay takes every item " +
                            $"at once, so the queue would stay empty. Connect the node that feeds '{from}' to '{to}' " +
                            $"directly, or put a station between '{from}' and '{to}'.");
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
                // ── from=Sink (invalid) ──────────────────────────────────────
                else if (sinks.ContainsKey(from))
                {
                    throw new InvalidOperationException(
                        $"Connection source '{from}' is a sink. Sinks are terminal nodes and cannot be connection sources.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Connection source '{from}' not found in the topology.");
                }

                if (!connected && !sinks.ContainsKey(from) && !sources.ContainsKey(from))
                {
                    throw new InvalidOperationException(
                        $"Connection '{from}' → '{to}': destination '{to}' not found or connection type is unsupported.");
                }
                else if (!connected)
                {
                    throw new InvalidOperationException(
                        $"Connection '{from}' → '{to}': destination '{to}' not found in the topology.");
                }
            }

            // Apply the real seed. This is the reset that makes every SeedID assignment above take
            // effect; from here on the per-node streams are a pure function of (topology.Seed,
            // node.Id) and survive both a process restart and a reordering of the node list.
            model.Reset(topology.Seed);

            return new ActiveModel(model, sources, buffers, servers, delays, sinks, topology);
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
        {
            string type = (node.Type ?? string.Empty).Trim().ToLowerInvariant();
            return type switch
            {
                "buffer" => "queue",
                "server" => "station",
                _ => type,
            };
        }

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
                    throw new InvalidOperationException($"{label} '{node.Id}': give {distName} or {meanName}, not both.");
                if (given?.Distribution == null)
                    throw new InvalidOperationException(
                        $"{label} '{node.Id}': {distName} must be a distribution object such as " +
                        $"{{\"type\": \"exponential\", \"mean\": 2}}, not {given?.ToString() ?? "null"}.");
                return DistributionFactory.Create(given.Distribution, $"node '{node.Id}', parameter '{distName}'");
            }
            if (!hasMean) mean = 1.0;
            if (mean <= 0)
                throw new InvalidOperationException($"{label} '{node.Id}': {meanName} must be positive.");
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
                    throw new InvalidOperationException(
                        $"{label} '{node.Id}': discipline must be \"fifo\" or \"lifo\", not {given?.ToString() ?? "null"}.");
            }
        }

        /// <summary>SIM-65 (D2): a station's <c>servers</c>, a whole number from 1 (the default) to 10,000.</summary>
        private static int ServerCount(NodeDefinition node, string label)
        {
            if (!TryGetNumber(node, label, "servers", out double count)) return 1;
            if (count < 1 || count > MaxServers || count != Math.Floor(count))
                throw new InvalidOperationException(
                    $"{label} '{node.Id}': servers must be a whole number from 1 to {MaxServers:N0}, not {count}.");
            return (int)count;
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
            throw new InvalidOperationException(
                $"{kind} '{node.Id}': {name} must be a number, not {param?.ToString() ?? "null"}.");
        }
    }
}
