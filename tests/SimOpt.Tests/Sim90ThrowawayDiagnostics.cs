// THROWAWAY DIAGNOSTIC FILE for SIM-90 — must be deleted before the session ends.
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests;

public class Sim90ThrowawayDiagnostics
{
    private readonly ITestOutputHelper _out;
    public Sim90ThrowawayDiagnostics(ITestOutputHelper output) => _out = output;

    private static TopologyDefinition TwoStage() => new()
    {
        Name = "Tandem",
        Seed = 42,
        Nodes =
        {
            new NodeDefinition { Id = "arrivals", Type = "source", Params = { ["mean_interval"] = 2.0 } },
            new NodeDefinition { Id = "q1", Type = "buffer", Params = { ["capacity"] = 100 } },
            new NodeDefinition { Id = "s1", Type = "server", Params = { ["service_time"] = 1.0 } },
            new NodeDefinition { Id = "q2", Type = "buffer", Params = { ["capacity"] = 100 } },
            new NodeDefinition { Id = "s2", Type = "server", Params = { ["service_time"] = 1.0 } },
            new NodeDefinition { Id = "done", Type = "sink" },
        },
        Connections =
        {
            new ConnectionDefinition { From = "arrivals", To = "q1" },
            new ConnectionDefinition { From = "q1", To = "s1" },
            new ConnectionDefinition { From = "s1", To = "q2" },
            new ConnectionDefinition { From = "q2", To = "s2" },
            new ConnectionDefinition { From = "s2", To = "done" },
        },
    };

    // M1: real ModelRegistry, default generator, two-stage — what exactly fails, and where?
    [Fact]
    public void M1_DefaultGenerator_TwoStage_RealRegistry()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(TwoStage());
        var active = registry.Get(id);
        Exception? caught = null;
        try { active.Model.Run(1000.0); }
        catch (Exception ex) { caught = ex; }
        _out.WriteLine("Exception: " + (caught?.GetType().FullName ?? "none"));
        _out.WriteLine("Message: " + (caught?.Message ?? ""));
        _out.WriteLine("Stack: " + (caught?.StackTrace ?? ""));
        _out.WriteLine("Sink done count: " + active.Sinks["done"].Count);
        _out.WriteLine("Time: " + active.Model.CurrentTime);
    }

    // M2: same topology, manually wired exactly like ModelRegistry, but with instrumented
    // pass-through createProduct. NegExp, Reset(seed), Run(1000).
    [Fact]
    public void M2_PassThrough_TwoStage_McpStyleWiring()
    {
        var model = new Model("Tandem", 43, 0d);
        model.LoggingEnabled = false;
        int counter = 0;

        var src = new SimpleSource(model, 101, NegExp(2.0),
            () => { counter++; return new SimpleEntity(model, $"E{counter}", $"E{counter}"); },
            autoStartDelay: 0d, id: "arrivals", name: "arrivals");

        var q1 = new SimpleBuffer(model, QueueRule.FIFO, id: "q1", name: "q1", maxCapacity: 100);
        var q2 = new SimpleBuffer(model, QueueRule.FIFO, id: "q2", name: "q2", maxCapacity: 100);

        int s1Calls = 0, s1Empty = 0, s2Calls = 0, s2Empty = 0;
        var s1CallTimes = new List<double>();

        var s1 = new SimpleServer(model, 102, NegExp(1.0), id: "s1", name: "s1",
            createProduct: m =>
            {
                s1Calls++;
                s1CallTimes.Add(model.CurrentTime);
                if (m.Count == 0) { s1Empty++; return null!; }
                return m[0];
            });
        s1.AutoContinue = true;

        var s2 = new SimpleServer(model, 103, NegExp(1.0), id: "s2", name: "s2",
            createProduct: m =>
            {
                s2Calls++;
                if (m.Count == 0) { s2Empty++; return null!; }
                return m[0];
            });
        s2.AutoContinue = true;

        var sink = new SimpleSink(model, id: "done", name: "done");

        // wiring exactly as ModelRegistry does it
        q1.ConnectTo(src);
        s1.ConnectTo(q1);
        q1.ItemReceivedEvent.AddHandler((_, _) => { if (s1.Idle) s1.Start(); });
        q2.ConnectTo(s1);
        s2.ConnectTo(q2);
        q2.ItemReceivedEvent.AddHandler((_, _) => { if (s2.Idle) s2.Start(); });
        sink.ConnectTo(s2);

        model.Reset(42);

        Exception? caught = null;
        try { model.Run(1000.0); }
        catch (Exception ex) { caught = ex; }

        _out.WriteLine("Exception: " + (caught?.GetType().FullName ?? "none"));
        _out.WriteLine("Message: " + (caught?.Message ?? ""));
        _out.WriteLine("StackTop: " + (caught?.StackTrace?.Split('\n').FirstOrDefault() ?? ""));
        _out.WriteLine($"s1Calls={s1Calls} s1Empty={s1Empty} s2Calls={s2Calls} s2Empty={s2Empty}");
        _out.WriteLine($"sink={sink.Count} time={model.CurrentTime}");
    }

    // M3: invocation timing and multiplicity of the deferred factory.
    [Fact]
    public void M3_FactoryInvocationTiming_SingleService()
    {
        var model = new Model("Timing", 7, 0d);
        model.LoggingEnabled = false;

        var invocations = new List<double>();
        SimpleEntity? seen = null;

        var srv = new SimpleServer(model, new ConstantDoubleDistribution(0.5, false),
            id: "srv", name: "srv",
            createProduct: m => { invocations.Add(model.CurrentTime); seen = m[0]; return m[0]; });
        srv.PushAllowed = true;
        var sink = new SimpleSink(model, id: "done", name: "done");
        sink.ConnectTo(srv);

        var e = new SimpleEntity(model, "E1", "E1");
        srv.Put(e);
        srv.Start();
        _out.WriteLine("after Start, invocations so far: " + invocations.Count);
        model.Run(10.0);

        _out.WriteLine("invocation times: " + string.Join(",", invocations));
        _out.WriteLine("sink count: " + sink.Count);
        _out.WriteLine("identity preserved: " + ReferenceEquals(seen, e));
        invocations.Count.Should().Be(1);
    }

    // M4: identity survival + ordering probe under AutoContinue with a queue backlog:
    // two items pre-queued, deterministic times. If the repeater re-entered StartWorking
    // before the finish instance materialised the product, batch 1's product would be item 2.
    [Fact]
    public void M4_IdentityAndOrdering_BacklogAutoContinue()
    {
        var model = new Model("Order", 7, 0d);
        model.LoggingEnabled = false;

        var products = new List<string>();
        var srv = new SimpleServer(model, new ConstantDoubleDistribution(0.5, false),
            id: "srv", name: "srv",
            createProduct: m => { products.Add(m.Count == 0 ? "EMPTY" : m[0].Identifier); return m.Count == 0 ? null! : m[0]; });
        srv.AutoContinue = true;

        var buf = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);
        srv.ConnectTo(buf);
        buf.ItemReceivedEvent.AddHandler((_, _) => { if (srv.Idle) srv.Start(); });
        var sink = new SimpleSink(model, id: "done", name: "done");
        sink.ConnectTo(srv);

        buf.Put(new SimpleEntity(model, "A", "A"));
        buf.Put(new SimpleEntity(model, "B", "B"));

        model.Run(10.0);

        _out.WriteLine("products in order: " + string.Join(",", products));
        _out.WriteLine("sink count: " + sink.Count);
        products.Should().Equal("A", "B");
    }

    private static NegExponentialDistribution NegExp(double mean)
    {
        var d = new NegExponentialDistribution();
        d.ConfigureMean(mean);
        return d;
    }
}
