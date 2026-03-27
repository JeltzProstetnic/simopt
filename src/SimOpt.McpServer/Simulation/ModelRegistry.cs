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
            var model = new Model(topology.Name, topology.Seed, 0d);
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

                        // Build a seed-initialised NegExponentialDistribution
                        var dist = CreateNegExp(topology.Seed ^ node.Id.GetHashCode(), meanInterval);
                        int localCounter = entityCounter; // closure capture
                        Func<SimpleEntity> generator = () =>
                        {
                            localCounter++;
                            entityCounter = localCounter;
                            return new SimpleEntity(model, $"E{localCounter}", $"E{localCounter}");
                        };

                        var source = new SimpleSource(
                            model,
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

                        var dist = CreateNegExp(topology.Seed ^ (node.Id.GetHashCode() + 1), serviceTime);
                        var server = new SimpleServer(
                            model,
                            dist,
                            id: node.Id,
                            name: node.Id);
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

            return new ActiveModel(model, sources, buffers, servers, sinks, topology);
        }

        /// <summary>
        /// Creates a seed-initialised negative exponential distribution with the given mean.
        /// </summary>
        private static NegExponentialDistribution CreateNegExp(int seed, double mean)
        {
            var dist = new NegExponentialDistribution();
            dist.Initialize(seed);
            dist.ConfigureMean(mean);
            return dist;
        }
    }
}
