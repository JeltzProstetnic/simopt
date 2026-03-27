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
                var dist = new ConstantDoubleDistribution(serviceTime, false);
                var server = new SimpleServer(_model, dist, name: node.Id);
                server.AutoContinue = true;
                // Server starts via ItemReceived handler on connected buffer, not AutoStart
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

    private void Connect(string fromId, string toId)
    {
        var from = _entities[fromId];
        var to = _entities[toId];

        // Source pushes to buffer/server/sink (adds EntityCreated handler that calls Put)
        if (from is SimpleSource source && to is IItemSink<SimpleEntity> sink1)
        {
            source.ConnectTo(sink1);
        }
        // Server pulls from buffer + start server when items arrive
        else if (to is SimpleServer server && from is SimpleBuffer buffer)
        {
            server.ConnectTo(buffer);
            buffer.ItemReceivedEvent.AddHandler((sender, item) =>
            {
                if (server.Idle) server.Start();
            });
        }
        // Sink pulls from server
        else if (to is SimpleSink sink && from is SimpleServer srv)
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
