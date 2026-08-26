using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SimOpt.Mathematics.Stochastics.Distributions;
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
                switch (node.Type.ToLowerInvariant())
                {
                    case "source":
                    {
                        double meanInterval = node.Params.TryGetValue("mean_interval", out double mi) ? mi : 1.0;
                        if (meanInterval <= 0)
                            throw new InvalidOperationException($"Source '{node.Id}': mean_interval must be positive.");

                        var dist = CreateNegExp(meanInterval);
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

                    case "buffer":
                    {
                        int capacity = node.Params.TryGetValue("capacity", out double cap)
                            ? (int)cap
                            : int.MaxValue;
                        var buffer = new SimpleBuffer(
                            model,
                            QueueRule.FIFO,
                            id: node.Id,
                            name: node.Id,
                            maxCapacity: capacity);
                        buffers[node.Id] = buffer;
                        break;
                    }

                    case "server":
                    {
                        double serviceTime = node.Params.TryGetValue("service_time", out double st) ? st : 1.0;
                        if (serviceTime <= 0)
                            throw new InvalidOperationException($"Server '{node.Id}': service_time must be positive.");

                        var dist = CreateNegExp(serviceTime);
                        var server = new SimpleServer(
                            model,
                            StableHash.Of(node.Id),   // see the source case above
                            dist,
                            id: node.Id,
                            name: node.Id);
                        // SIM-90 (open): this server deliberately keeps the DEFAULT product
                        // generator, which manufactures a fresh SimpleEntity with a null
                        // Identifier rather than passing the input entity through. That is wrong
                        // in two ways — Buffer.Put keys on Identifier, so a server feeding a
                        // downstream buffer will fail, and entity identity is destroyed, which
                        // makes an end-to-end cycle time unmeasurable. The obvious fix
                        // (createProduct: m => m[0]) cannot be applied yet: Server defers the
                        // product factory to event-firing time while InternalFinishedHandler
                        // clears activeMaterial, so the delegate is invoked against an empty list.
                        // Tracked separately rather than bundled into this repair.
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
                            $"Unknown node type '{node.Type}' for node '{node.Id}'. Valid types: source, buffer, server, sink.");
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
    }
}
