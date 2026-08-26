using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-63 — the two engine seams the output-statistics subsystem needs, neither of which existed.
///
/// <para>
/// <c>Buffer</c> announced arrivals but not departures, so a waiting time could not be closed and a
/// queue length could not be decremented from inside the engine. <c>Server</c> had no busy/idle
/// notification at all — <c>working</c> was set and cleared by private methods — which is why
/// utilisation ended up being sampled once per UI render tick
/// (<c>SimulationCanvas.cs:297-332</c>), a measurement that cannot see an interval shorter than the
/// poll gap and is unreachable from a headless or MCP run.
/// </para>
/// <para>
/// Both new events are raised synchronously, draw no random numbers and schedule nothing, so the
/// event calendar of an instrumented run is identical to an uninstrumented one and the SIM-58
/// reset guarantees are untouched.
/// </para>
/// </summary>
public class InstrumentationSeamTests
{
    [Fact]
    public void BufferItemRemovedEvent_FiresFromAllThreeGetOverloads()
    {
        var model = new Model("m", 1, 0d);
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);

        var removed = new List<string>();
        buffer.ItemRemovedEvent.AddHandler((_, item) => removed.Add(item.Identifier));

        model.Reset(1);

        var e1 = new SimpleEntity(model, "E1", "E1");
        var e2 = new SimpleEntity(model, "E2", "E2");
        var e3 = new SimpleEntity(model, "E3", "E3");
        buffer.Put(e1);
        buffer.Put(e2);
        buffer.Put(e3);

        // Buffer exposes three separate removal paths. A collector that watched only the common one
        // would drift away from the buffer's real contents the first time anything used another,
        // and the drift would be silent — so all three are pinned here rather than just Get().
        buffer.Get(0).Should().BeSameAs(e1);        // by internal id
        buffer.Get().Should().BeSameAs(e2);         // by the buffer's own selector
        buffer.Get("E3").Should().BeSameAs(e3);     // by entity id

        removed.Should().Equal("E1", "E2", "E3");
        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void ServerWorkingChangedEvent_BracketsExactlyOneServiceInterval()
    {
        var model = new Model("m", 1, 0d);
        var service = new ConstantDoubleDistribution();
        service.Configure(2.0);

        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);
        var server = new SimpleServer(model, service, id: "srv", name: "srv");
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        server.ConnectTo(buffer);
        sink.ConnectTo(server);

        var transitions = new List<(double Time, bool Working)>();
        server.WorkingChangedEvent.AddHandler((_, w) => transitions.Add((model.CurrentTime, w)));

        model.Reset(1);
        buffer.Put(new SimpleEntity(model, "E1", "E1"));
        server.Start();
        model.Run(10.0);

        // One entity, constant service of 2: the busy interval is exactly [0, 2] and nothing else
        // happens for the remaining 8 time units. A polled measurement can only approximate these
        // instants; an event-driven one lands on them exactly, which is the whole point.
        transitions.Should().Equal((0d, true), (2d, false));
    }

    [Fact]
    public void ServerWorkingChangedEvent_DoesNotFireOnReset()
    {
        var model = new Model("m", 1, 0d);
        var service = new ConstantDoubleDistribution();
        service.Configure(2.0);

        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);
        var server = new SimpleServer(model, service, id: "srv", name: "srv");
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        server.ConnectTo(buffer);
        sink.ConnectTo(server);

        model.Reset(1);
        buffer.Put(new SimpleEntity(model, "E1", "E1"));
        server.Start();
        model.Run(1.0);   // stops mid-service, so the server is left working

        var transitions = new List<(double Time, bool Working)>();
        server.WorkingChangedEvent.AddHandler((_, w) => transitions.Add((model.CurrentTime, w)));

        model.Reset(1);

        // A reset is not a transition. If it emitted one, every collector would record a spurious
        // interval at time zero on every replication — a systematic bias, not noise, and one that
        // would grow with the replication count rather than average out.
        transitions.Should().BeEmpty();
    }
}
