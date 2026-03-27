using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using SimOpt.Mathematics.Stochastics.Distributions;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Simulation;

/// <summary>
/// Headless tests for multi-stage factory topology wiring.
/// Validates that items flow through all connection patterns:
///   Source→Buffer, Buffer→Server, Server→Buffer, Server→Server, Server→Sink
/// </summary>
public class FactoryModelTests
{
    private readonly ITestOutputHelper _out;

    public FactoryModelTests(ITestOutputHelper output) => _out = output;

    private int _counter;

    private SimpleEntity MakeEntity(Model model)
    {
        _counter++;
        return new SimpleEntity(model, $"E{_counter}", $"Entity {_counter}");
    }

    /// <summary>
    /// Minimal chain: Source → Buffer → Server → Buffer → Server → Sink
    /// Tests Server→Buffer and Server→Server wiring without visualization.
    /// </summary>
    [Fact]
    public void MinimalChain_ServerToBuffer_ItemsFlowThrough()
    {
        var model = new Model("MinimalChain", 42, DateTime.MinValue);
        _counter = 0;

        // Source generates every 1.0 time unit
        var source = new SimpleSource(model, new ConstantDoubleDistribution(1.0, false),
            () => MakeEntity(model), name: "source");

        var buf1 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf1", maxCapacity: 10);
        var srv1 = new SimpleServer(model, new ConstantDoubleDistribution(0.5, false), name: "srv1",
            createProduct: m => m[0]);
        srv1.AutoContinue = true;

        var buf2 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf2", maxCapacity: 10);
        var srv2 = new SimpleServer(model, new ConstantDoubleDistribution(0.3, false), name: "srv2",
            createProduct: m => m[0]);
        srv2.AutoContinue = true;

        var sink = new SimpleSink(model, name: "sink");

        // Wire: Source → buf1 → srv1 → buf2 → srv2 → sink
        source.ConnectTo(buf1);

        srv1.ConnectTo(buf1);
        buf1.ItemReceivedEvent.AddHandler((sender, item) => { if (srv1.Idle) srv1.Start(); });

        // Server → Buffer: buffer subscribes to server's finish event
        buf2.ConnectTo(srv1);

        srv2.ConnectTo(buf2);
        buf2.ItemReceivedEvent.AddHandler((sender, item) => { if (srv2.Idle) srv2.Start(); });

        sink.ConnectTo(srv2);

        source.Start();

        // Run for 10 time units
        int steps = 0;
        while (model.CurrentTime < 10.0 && steps < 10000)
        {
            model.Step(0.1);
            steps++;
        }

        _out.WriteLine($"Steps: {steps}, Time: {model.CurrentTime:F2}, Sink count: {sink.Count}");

        steps.Should().BeLessThan(10000, "simulation should not hang");
        sink.Count.Should().BeGreaterThan(0, "items should reach the sink");
    }

    /// <summary>
    /// Server → Server direct push (no intermediate buffer).
    /// </summary>
    [Fact]
    public void ServerToServer_DirectPush_ItemsFlowThrough()
    {
        var model = new Model("ServerToServer", 42, DateTime.MinValue);
        _counter = 0;

        var source = new SimpleSource(model, new ConstantDoubleDistribution(1.0, false),
            () => MakeEntity(model), name: "source");

        var buf1 = new SimpleBuffer(model, QueueRule.FIFO, name: "buf1", maxCapacity: 10);
        var srv1 = new SimpleServer(model, new ConstantDoubleDistribution(0.5, false), name: "srv1",
            createProduct: m => m[0]);
        srv1.AutoContinue = true;

        var srv2 = new SimpleServer(model, new ConstantDoubleDistribution(0.3, false), name: "srv2",
            createProduct: m => m[0]);
        srv2.AutoContinue = true;
        srv2.PushAllowed = true;

        var sink = new SimpleSink(model, name: "sink");

        // Wire: Source → buf1 → srv1 → srv2 → sink
        source.ConnectTo(buf1);

        srv1.ConnectTo(buf1);
        buf1.ItemReceivedEvent.AddHandler((sender, item) => { if (srv1.Idle) srv1.Start(); });

        // Server → Server: hook finish event
        srv1.EntityCreatedEvent.AddHandler((sender, entity) => srv2.Put(entity),
            new Priority(type: PriorityType.SimWorldBeforeOthers));

        sink.ConnectTo(srv2);

        source.Start();

        int steps = 0;
        while (model.CurrentTime < 10.0 && steps < 10000)
        {
            model.Step(0.1);
            steps++;
        }

        _out.WriteLine($"Steps: {steps}, Time: {model.CurrentTime:F2}, Sink count: {sink.Count}");

        steps.Should().BeLessThan(10000, "simulation should not hang");
        sink.Count.Should().BeGreaterThan(0, "items should reach the sink");
    }

    /// <summary>
    /// Server with probabilistic rejection — items split between pass and reject targets.
    /// </summary>
    [Fact]
    public void RejectServer_SplitsOutputByProbability()
    {
        var model = new Model("RejectTest", 42, DateTime.MinValue);
        _counter = 0;

        var source = new SimpleSource(model, new ConstantDoubleDistribution(0.5, false),
            () => MakeEntity(model), name: "source");

        var buf = new SimpleBuffer(model, QueueRule.FIFO, name: "buf", maxCapacity: 100);

        // 50% reject rate for easy statistical checking
        var srv = new SimpleRejectServer(model,
            new ConstantDoubleDistribution(0.2, false), rejectRate: 0.5, name: "srv", seed: 99);

        var waste = new SimpleSink(model, name: "waste");
        var output = new SimpleSink(model, name: "output");

        // Wire
        source.ConnectTo(buf);
        srv.ConnectTo(buf);
        buf.ItemReceivedEvent.AddHandler((sender, item) => { if (srv.Idle) srv.Start(); });
        srv.SetRejectTarget(waste);
        srv.AddPassTarget(output);

        source.Start();

        int steps = 0;
        while (model.CurrentTime < 50.0 && steps < 50000)
        {
            model.Step(0.1);
            steps++;
        }

        _out.WriteLine($"Steps: {steps}, Time: {model.CurrentTime:F2}");
        _out.WriteLine($"Output: {output.Count}, Waste: {waste.Count}, Total: {output.Count + waste.Count}");

        steps.Should().BeLessThan(50000, "simulation should not hang");
        output.Count.Should().BeGreaterThan(0, "some items should pass");
        waste.Count.Should().BeGreaterThan(0, "some items should be rejected");
        (output.Count + waste.Count).Should().BeGreaterThan(50, "total processed should be substantial");
    }

    /// <summary>
    /// Full factory topology matching VizTopology.FactoryFloor() — headless.
    /// Two sources, inspection, 3 parallel assembly, QC, packing, shipping.
    /// </summary>
    [Fact]
    public void FullFactory_AllConnectionPatterns_RunsToCompletion()
    {
        var model = new Model("Factory", 42, DateTime.MinValue);
        _counter = 0;

        // Sources
        var dockA = new SimpleSource(model, new GaussianDistribution(0.75, 0.11),
            () => MakeEntity(model), name: "dock_a");
        var dockB = new SimpleSource(model, new GaussianDistribution(0.85, 0.13),
            () => MakeEntity(model), name: "dock_b");

        // Incoming buffer
        var incoming = new SimpleBuffer(model, QueueRule.FIFO, name: "incoming", maxCapacity: 40);

        // Inspection — 5% reject rate
        var inspect = new SimpleRejectServer(model, new ConstantDoubleDistribution(0.33, false),
            rejectRate: 0.05, name: "inspect", seed: 42);

        // Staging buffer
        var staging = new SimpleBuffer(model, QueueRule.FIFO, name: "staging", maxCapacity: 30);

        // Assembly lines
        var asm1 = new SimpleServer(model, new ConstantDoubleDistribution(1.5, false), name: "asm1",
            createProduct: m => m[0]);
        asm1.AutoContinue = true;
        var asm2 = new SimpleServer(model, new ConstantDoubleDistribution(1.5, false), name: "asm2",
            createProduct: m => m[0]);
        asm2.AutoContinue = true;
        var asm3 = new SimpleServer(model, new ConstantDoubleDistribution(1.5, false), name: "asm3",
            createProduct: m => m[0]);
        asm3.AutoContinue = true;

        // QC buffer
        var qcBuf = new SimpleBuffer(model, QueueRule.FIFO, name: "qc_buf", maxCapacity: 20);

        // QC
        var qc = new SimpleServer(model, new ConstantDoubleDistribution(0.5, false), name: "qc",
            createProduct: m => m[0]);
        qc.AutoContinue = true;

        // Packing
        var packing = new SimpleServer(model, new ConstantDoubleDistribution(0.25, false), name: "packing",
            createProduct: m => m[0]);
        packing.AutoContinue = true;

        // Shipping + Waste
        var shipping = new SimpleSink(model, name: "shipping");
        var waste = new SimpleSink(model, name: "waste");

        // === Wire connections (same patterns as SimulationModel.Connect) ===

        // Source → Buffer
        dockA.ConnectTo(incoming);
        dockB.ConnectTo(incoming);

        // Buffer → Server (incoming → inspect)
        inspect.ConnectTo(incoming);
        incoming.ItemReceivedEvent.AddHandler((sender, item) => { if (inspect.Idle) inspect.Start(); });

        // RejectServer output: reject → waste, pass → staging
        inspect.SetRejectTarget(waste);
        inspect.AddPassTarget(staging);

        // Buffer → Server (staging → asm1/2/3)
        asm1.ConnectTo(staging);
        asm2.ConnectTo(staging);
        asm3.ConnectTo(staging);
        staging.ItemReceivedEvent.AddHandler((sender, item) =>
        {
            if (asm1.Idle) asm1.Start();
            else if (asm2.Idle) asm2.Start();
            else if (asm3.Idle) asm3.Start();
        });

        // Server → Buffer (asm1/2/3 → qcBuf)
        qcBuf.ConnectTo(asm1);
        qcBuf.ConnectTo(asm2);
        qcBuf.ConnectTo(asm3);

        // Buffer → Server (qcBuf → qc)
        qc.ConnectTo(qcBuf);
        qcBuf.ItemReceivedEvent.AddHandler((sender, item) => { if (qc.Idle) qc.Start(); });

        // Server → Server (qc → packing)
        packing.PushAllowed = true;
        qc.EntityCreatedEvent.AddHandler((sender, entity) => packing.Put(entity),
            new Priority(type: PriorityType.SimWorldBeforeOthers));

        // Server → Sink (packing → shipping)
        shipping.ConnectTo(packing);

        // Start sources
        dockA.Start();
        dockB.Start();

        // Run for 50 time units
        int steps = 0;
        while (model.CurrentTime < 50.0 && steps < 50000)
        {
            model.Step(0.1);
            steps++;
        }

        _out.WriteLine($"Steps: {steps}, Time: {model.CurrentTime:F2}");
        _out.WriteLine($"Incoming: {incoming.Count}, Staging: {staging.Count}, QC Queue: {qcBuf.Count}");
        _out.WriteLine($"Shipping: {shipping.Count}, Waste: {waste.Count}");

        steps.Should().BeLessThan(50000, "simulation should not hang");
        model.CurrentTime.Should().BeGreaterThanOrEqualTo(50.0, "simulation should reach end time");
        shipping.Count.Should().BeGreaterThan(0, "items should reach shipping");
    }
}
