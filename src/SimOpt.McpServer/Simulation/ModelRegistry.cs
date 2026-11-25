using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var servers = new Dictionary<string, SimpleServer>();
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
                        if (TryGetNumber(node, label, "servers", out double count) && count != 1d)
                            throw new InvalidOperationException(
                                $"{label} '{node.Id}': servers = {count} is not supported yet; a station has one server in this version.");
                        var dist = TimeDistribution(node, label, "service", "service_time");
                        var server = new SimpleServer(
                            model,
                            StableHash.Of(node.Id),   // see the source case above
                            dist,
                            id: node.Id,
                            name: node.Id,
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
                        servers[node.Id] = server;
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
                            $"Unknown node type '{node.Type}' for node '{node.Id}'. Valid types: source, queue, station, sink " +
                            "(buffer and server are accepted for queue and station).");
                }
            }

            // ── wire connections ─────────────────────────────────────────────
            // ConnectTo semantics: downstream.ConnectTo(upstream)
            //
            // Valid connection patterns:
            //   Source  → Buffer  : buffer.ConnectTo(source)    source is IItemSource<SimpleEntity>
            //   Source  → Server  : server.ConnectTo(source)    source is IItemSource<SimpleEntity>
            //   Source  → Sink    : sink.ConnectTo(source)      source is IItemSource<SimpleEntity>
            //   Buffer  → Server  : server.ConnectTo(buffer)    buffer is IItemBuffer<SimpleEntity>
            //   Server  → Sink    : sink.ConnectTo(server)      server is IItemSource<SimpleEntity>
            //   Server  → Server  : server.ConnectTo(source)    server is IItemSource<SimpleEntity>
            //
            // Buffer → Sink directly is not supported (Buffer is IItemBuffer, Sink needs IItemSource).
            // Use Buffer → Server → Sink instead.

            foreach (ConnectionDefinition conn in topology.Connections)
            {
                string from = conn.From;
                string to = conn.To;

                bool connected = false;

                // ── from=Source ──────────────────────────────────────────────
                if (sources.TryGetValue(from, out SimpleSource? upSrc))
                {
                    if (buffers.TryGetValue(to, out SimpleBuffer? downBuf))
                    {
                        downBuf.ConnectTo(upSrc);
                        connected = true;
                    }
                    else if (servers.TryGetValue(to, out SimpleServer? downSrv))
                    {
                        downSrv.ConnectTo((IItemSource<SimpleEntity>)upSrc);
                        connected = true;
                    }
                    else if (sinks.TryGetValue(to, out SimpleSink? downSnk))
                    {
                        downSnk.ConnectTo(upSrc);
                        connected = true;
                    }
                }
                // ── from=Buffer ──────────────────────────────────────────────
                else if (buffers.TryGetValue(from, out SimpleBuffer? upBuf))
                {
                    if (servers.TryGetValue(to, out SimpleServer? downSrv))
                    {
                        downSrv.ConnectTo(upBuf);
                        // SIM-89: a server *pulls* from its buffer, so connecting the two is only
                        // half the wiring — an idle server has nothing telling it that work has
                        // arrived. Without this handler the buffer fills and never drains, the run
                        // completes "successfully", and every sink reports zero. Every other
                        // builder in the repository does this (SimulationModel.cs:197,
                        // IvotionTopologyBuilder.cs:106, and both example programs); ModelRegistry
                        // was the only one that did not, which is why no MCP-built model has ever
                        // produced throughput.
                        SimpleServer pulling = downSrv;
                        upBuf.ItemReceivedEvent.AddHandler((_, _) =>
                        {
                            if (pulling.Idle) pulling.Start();
                        });
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
                }
                // ── from=Server ──────────────────────────────────────────────
                else if (servers.TryGetValue(from, out SimpleServer? upSrv))
                {
                    if (sinks.TryGetValue(to, out SimpleSink? downSnk))
                    {
                        downSnk.ConnectTo(upSrv);
                        connected = true;
                    }
                    else if (servers.TryGetValue(to, out SimpleServer? downSrv2))
                    {
                        downSrv2.ConnectTo((IItemSource<SimpleEntity>)upSrv);
                        connected = true;
                    }
                    else if (buffers.TryGetValue(to, out SimpleBuffer? downBuf2))
                    {
                        downBuf2.ConnectTo(upSrv);
                        connected = true;
                    }
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

            return new ActiveModel(model, sources, buffers, servers, sinks, topology);
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
