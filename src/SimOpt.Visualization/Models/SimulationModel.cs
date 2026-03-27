using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using SimOpt.Mathematics.Stochastics.Distributions;

namespace SimOpt.Visualization.Models;

/// <summary>
/// Builds and runs a simulation from a VizTopology description.
/// Provides state snapshots for the visualization canvas.
/// </summary>
public class SimulationModel
{
    private Model _model = null!;
    private readonly Dictionary<string, object> _entities = new();
    private readonly Dictionary<string, VizNode> _nodeDefinitions = new();
    private readonly List<SimpleSource> _sources = new();
    private int _productCounter;

    public VizTopology Topology { get; private set; } = null!;
    public double EndTime { get; set; } = 200.0;
    public bool IsFinished => _model.CurrentTime >= EndTime;
    public double CurrentTime => _model.CurrentTime;

    public void Build(VizTopology topology)
    {
        Topology = topology;
        _productCounter = 0;
        _entities.Clear();
        _nodeDefinitions.Clear();
        _sources.Clear();

        _model = new Model(topology.Name, topology.Seed, DateTime.MinValue);

        // Create all nodes
        foreach (var node in topology.Nodes)
        {
            _nodeDefinitions[node.Id] = node;
            var entity = CreateEntity(node);
            _entities[node.Id] = entity;
        }

        // Wire connections
        foreach (var conn in topology.Connections)
        {
            Connect(conn.From, conn.To);
        }
    }

    public void Start()
    {
        foreach (var source in _sources)
            source.Start();
    }

    public bool Step(double stepSize = 0.1)
    {
        if (IsFinished) return false;
        _model.Step(stepSize);
        return !IsFinished;
    }

    /// <summary>
    /// Returns current state of all nodes for rendering.
    /// </summary>
    public List<NodeState> GetNodeStates()
    {
        var states = new List<NodeState>();
        foreach (var (id, entity) in _entities)
        {
            var def = _nodeDefinitions[id];
            var state = new NodeState { Id = id, Type = def.Type };

            switch (entity)
            {
                case SimpleBuffer buf:
                    state.Count = buf.Count;
                    state.Capacity = buf.MaxCapacity;
                    break;
                case SimpleServer srv:
                    state.Working = srv.Working;
                    state.Damaged = srv.Damaged;
                    break;
                case SimpleSink snk:
                    state.Count = snk.Count;
                    break;
            }

            states.Add(state);
        }
        return states;
    }

    private object CreateEntity(VizNode node)
    {
        switch (node.Type.ToLowerInvariant())
        {
            case "source":
            {
                double meanInterval = node.Params.GetValueOrDefault("mean_interval", 2.0);
                double stddev = node.Params.GetValueOrDefault("stddev", meanInterval * 0.15);
                var dist = new GaussianDistribution(meanInterval, stddev);
                var source = new SimpleSource(_model, dist, ProductGenerator, name: node.Id);
                _sources.Add(source);
                return source;
            }
            case "buffer":
            {
                int capacity = (int)node.Params.GetValueOrDefault("capacity", int.MaxValue);
                var buffer = new SimpleBuffer(_model, QueueRule.FIFO, name: node.Id, maxCapacity: capacity);
                return buffer;
            }
            case "server":
            {
                double serviceTime = node.Params.GetValueOrDefault("service_time", 1.0);
                double rejectRate = node.Params.GetValueOrDefault("reject_rate", 0.0);
                var dist = new ConstantDoubleDistribution(serviceTime, false);

                if (rejectRate > 0)
                {
                    // RejectServer handles its own output routing (pass + reject)
                    var rs = new SimpleRejectServer(_model, dist, rejectRate,
                        name: node.Id, seed: Topology.Seed + node.Id.GetHashCode());
                    return rs;
                }

                // Pass-through product: output = first input entity (preserves identity for downstream buffers).
                // Default createProduct generates new TProduct() with null Identifier → crashes Buffer.Put().
                var server = new SimpleServer(_model, dist, name: node.Id,
                    createProduct: material => material[0]);
                server.AutoContinue = true;
                return server;
            }
            case "sink":
            {
                var sink = new SimpleSink(_model, name: node.Id);
                return sink;
            }
            default:
                throw new ArgumentException($"Unknown node type: {node.Type}");
        }
    }

    /// <summary>
    /// Wires two nodes together based on their types.
    ///
    /// Connection patterns (order matters — checked top to bottom):
    ///   Source → any IItemSink  : source.ConnectTo(sink) — hooks EntityCreated → Put
    ///   Buffer → Server         : server.ConnectTo(buffer) — server pulls; ItemReceived starts idle server
    ///   Server → Buffer         : buffer.ConnectTo(server) — hooks EntityCreatedEvent (= finish event) → Put
    ///                             NOTE: Buffer does NOT implement IItemSink — cannot cast. Use ConnectTo(IItemSource).
    ///   Server → Server         : direct push via EntityCreatedEvent → Put; requires PushAllowed=true on receiver
    ///   Server → Sink           : sink.ConnectTo(server) — hooks EntityCreatedEvent → Put
    /// </summary>
    private void Connect(string fromId, string toId)
    {
        var from = _entities[fromId];
        var to = _entities[toId];

        // RejectServer handles its own output routing (must check before generic Server patterns)
        if (from is SimpleRejectServer rs)
        {
            if (to is SimpleSink sink0)
            {
                var toNode = _nodeDefinitions[toId];
                if (toNode.Params.ContainsKey("is_reject_target"))
                    rs.SetRejectTarget(sink0);
                else
                    rs.AddPassTarget(sink0);
            }
            else if (to is SimpleBuffer buf0)
                rs.AddPassTarget(buf0);
            return;
        }

        // Source → any IItemSink (buffer, server, or sink)
        if (from is SimpleSource source && to is IItemSink<SimpleEntity> sink1)
        {
            source.ConnectTo(sink1);
        }
        // Buffer → Server: server pulls from buffer + auto-start on item arrival
        else if (from is SimpleBuffer buffer && to is SimpleServer server)
        {
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((sender, item) =>
            {
                if (server.Idle) server.Start();
            });
        }
        // Server → Buffer: buffer subscribes to server's EntityCreatedEvent (which wraps entityFinishedEvent)
        else if (from is SimpleServer srvA && to is SimpleBuffer buf)
        {
            buf.ConnectTo(srvA);
        }
        // Server → Server: upstream finish event pushes directly to downstream's Put()
        else if (from is SimpleServer srvFrom && to is SimpleServer srvTo)
        {
            srvTo.PushAllowed = true;
            srvTo.AutoContinue = true;
            srvFrom.EntityCreatedEvent.AddHandler((sender, entity) => srvTo.Put(entity),
                new Priority(type: PriorityType.SimWorldBeforeOthers));
        }
        // Server → Sink: sink subscribes to server's EntityCreatedEvent
        else if (from is SimpleServer srv && to is SimpleSink sink)
        {
            sink.ConnectTo(srv);
        }
    }

    private SimpleEntity ProductGenerator()
    {
        _productCounter++;
        return new SimpleEntity(_model, $"E{_productCounter}", $"Entity {_productCounter}");
    }
}

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
