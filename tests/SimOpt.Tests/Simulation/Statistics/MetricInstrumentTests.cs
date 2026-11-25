using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-65 step 5 (D6): the collectors behind the schema's declared metrics that did not exist yet.
/// <c>utilization</c> of a station that is a pool of servers (step 3) is the busy share of the pool,
/// time-weighted; <c>count</c> and <c>throughput</c> read a sink after the warm-up; <c>cycle_time</c>
/// is the time from an item's creation at a source to its arrival at a sink, which SIM-90 made
/// measurable by passing the same entity through every station.
/// </summary>
public class MetricInstrumentTests
{
    private static SimpleServer Server(Model model, string id) =>
        new SimpleServer(model, StableHashOf(id), new ConstantDoubleDistribution(1.0, initialize: false),
                         id: id, name: id, createProduct: m => m[0]) { AutoContinue = true };

    private static int StableHashOf(string key) => SimOpt.Basics.Utilities.StableHash.Of(key);

    [Fact]
    public void APoolsUtilization_IsItsTimeWeightedBusyShare()
    {
        // Three items at t = 0, two servers with a service of 1: one server works on [0, 2), the
        // other on [0, 1). Over [0, 4) the pool is busy (2 + 1) / (2 * 4) = 0.375 of the time.
        var model = new Model("m", 1, 0d);
        var queue = new SimpleBuffer(model, QueueRule.FIFO, id: "q", name: "q");
        var pool = new List<SimpleServer> { Server(model, "s#1"), Server(model, "s#2") };
        foreach (SimpleServer s in pool) s.ConnectTo(queue);
        queue.ItemReceivedEvent.AddHandler((_, _) => pool.Find(s => s.Idle)?.Start());
        TimeWeightedCollector utilization = Instrumentation.ObservePoolUtilization(pool, "s");
        model.Schedule(0, () => { for (int i = 0; i < 3; i++) queue.Put(new SimpleEntity(model, "E" + i, "E" + i)); });
        model.Schedule(4, () => { });

        model.Run(4);

        utilization.Identifier.Should().Be("s.stat." + Instrumentation.Utilization);
        utilization.Estimate(4).Should().BeApproximately(0.375, 1e-12);
    }

    [Fact]
    public void APoolOfOne_MeasuresWhatTheSingleServerInstrumentMeasures()
    {
        var model = new Model("m", 1, 0d);
        var queue = new SimpleBuffer(model, QueueRule.FIFO, id: "q", name: "q");
        SimpleServer server = Server(model, "s");
        server.ConnectTo(queue);
        queue.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        TimeWeightedCollector single = Instrumentation.ObserveUtilization(server);
        TimeWeightedCollector pool = Instrumentation.ObservePoolUtilization(new[] { server }, "pool");
        model.Schedule(0.5, () => queue.Put(new SimpleEntity(model, "E0", "E0")));
        model.Schedule(3, () => queue.Put(new SimpleEntity(model, "E1", "E1")));
        model.Schedule(5, () => { });

        model.Run(5);

        pool.Estimate(5).Should().BeApproximately(single.Estimate(5), 1e-12).And.BeApproximately(0.4, 1e-12);
    }

    [Fact]
    public void CountAndThroughput_ReadTheSinkAfterTheWarmup_AndResetToZero()
    {
        var model = new Model("m", 1, 0d);
        var sink = new SimpleSink(model, id: "done", name: "done");
        CountCollector count = Instrumentation.ObserveCount(sink, warmup: 2.5);
        CountCollector throughput = Instrumentation.ObserveThroughput(sink, warmup: 2.5);
        for (int t = 1; t <= 10; t++)
        {
            int at = t;
            model.Schedule(at, () => sink.Put(new SimpleEntity(model, "E" + at, "E" + at)));
        }

        model.Run(10);

        count.Identifier.Should().Be("done.stat." + Instrumentation.ItemCount);
        count.Estimate(10).Should().Be(8, "the items of t = 3..10 arrive after the warm-up");
        throughput.Estimate(10).Should().BeApproximately(8 / 7.5, 1e-12);
        model.Reset();
        count.Estimate(10).Should().Be(0);
    }

    [Fact]
    public void CycleTime_RunsFromCreationAtASourceToArrivalAtASink()
    {
        var model = new Model("m", 1, 0d);
        int n = 0;
        var source = new SimpleSource(model, StableHashOf("src"), new ConstantDoubleDistribution(1.0, initialize: false),
                                      () => new SimpleEntity(model, $"E{++n}", $"E{n}"), autoStartDelay: 0d, id: "src", name: "src");
        var walk = new SimpleDelay(model, new ConstantDoubleDistribution(3.0, initialize: false), StableHashOf("walk"), id: "walk", name: "walk")
        { Capacity = int.MaxValue };
        var sink = new SimpleSink(model, id: "done", name: "done");
        walk.ConnectTo((IItemSource<SimpleEntity>)source);
        sink.ConnectTo((IItemSource<SimpleEntity>)walk);
        TallyCollector cycle = Instrumentation.ObserveCycleTime(model, "model", new IItemSource<SimpleEntity>[] { source }, new[] { sink });
        // Reset before both runs: a source's first item comes at t = 1 on a fresh model and at t = 0
        // after a reset (SIM-139), which would make the two runs differ by one item.
        model.Reset();
        model.Schedule(5, () => sink.Put(new SimpleEntity(model, "stranger", "stranger")));   // made by no observed source

        model.Run(20.5);
        long first = cycle.Count;

        cycle.Identifier.Should().Be("model.stat." + Instrumentation.CycleTime);
        first.Should().Be(sink.Count - 1, "every item but the stranger was timed");
        cycle.Mean.Should().BeApproximately(3, 1e-12);
        cycle.Max.Should().BeApproximately(3, 1e-12);

        model.Reset();
        model.Schedule(5, () => sink.Put(new SimpleEntity(model, "stranger2", "stranger2")));
        model.Run(20.5);
        cycle.Count.Should().Be(first, "a reset empties the collector, so the second run counts afresh");
    }
}
