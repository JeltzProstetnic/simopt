using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-65 step 4 (D4): a router passes each item straight on to one of its targets, either at random
/// in proportion to the targets' weights or to the target with the fewest items waiting. Its draws come
/// from its own stream, keyed to its seed ID and reset with the model, so routing is a function of
/// (model seed, router id); <c>SimpleRejectServer</c>'s routing is not (SIM-99), which is why this is a
/// new template. A target that refuses the item makes the router refuse it, visibly, as SIM-132's
/// delay does.
/// </summary>
public class RouterTests
{
    private sealed class RecordingSink : IItemSink<SimpleEntity>
    {
        private readonly string name;
        private readonly List<string> log;
        public RecordingSink(string name, List<string> log) { this.name = name; this.log = log; }
        public bool Put(SimpleEntity item) { log.Add(name); return true; }
        public bool ConnectTo(IItemSource<SimpleEntity> source) => true;
        public void ConnectTo(params IItemSource<SimpleEntity>[] sources) { }
        public void ConnectTo(IEnumerable<IItemSource<SimpleEntity>> sources) { }
        public bool IsConnectionAllowed(IItemSource<SimpleEntity> fromSource) => true;
    }

    private static void PutAt(Model model, double time, SimpleRouter router, int count, int first = 0) =>
        model.Schedule(time, () =>
        {
            for (int i = first; i < first + count; i++) router.Put(new SimpleEntity(model, "E" + i, "E" + i));
        });

    private static List<string> Sequence(int seedID, bool resetFirst = false)
    {
        var model = new Model("m", 1, 0d);
        var log = new List<string>();
        var router = new SimpleRouter(model, RoutingPolicy.Probabilistic, seedID, id: "r", name: "r");
        router.AddTarget(new RecordingSink("a", log));
        router.AddTarget(new RecordingSink("b", log));
        PutAt(model, 0, router, 40);
        model.Run(1);
        if (!resetFirst) return log;

        log.Clear();
        model.Reset();
        PutAt(model, 0, router, 40);
        model.Run(1);
        return log;
    }

    [Fact]
    public void AProbabilisticRouter_SplitsInTheProportionOfItsWeights()
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.Probabilistic, seedID: 11, id: "r", name: "r");
        SimpleSink[] sinks = { new(model, id: "a", name: "a"), new(model, id: "b", name: "b"), new(model, id: "c", name: "c") };
        router.AddTarget(sinks[0], weight: 1);
        router.AddTarget(sinks[1], weight: 2);
        router.AddTarget(sinks[2], weight: 7);
        PutAt(model, 0, router, 100_000);

        model.Run(1);

        // 0.005 is more than three standard errors for each share at n = 100,000.
        (sinks[0].Count / 1e5).Should().BeApproximately(0.1, 0.005);
        (sinks[1].Count / 1e5).Should().BeApproximately(0.2, 0.005);
        (sinks[2].Count / 1e5).Should().BeApproximately(0.7, 0.005);
        router.Routed.Should().Equal(sinks.Select(s => s.Count));
        router.RefusedCount.Should().Be(0);
    }

    [Fact]
    public void TheRoutingStream_RestartsWithTheModel_AndBelongsToTheRouter()
    {
        List<string> first = Sequence(seedID: 11);

        Sequence(seedID: 11, resetFirst: true).Should().Equal(first, "a reset model routes as a fresh one does");
        first.Distinct().Should().HaveCount(2);
        Sequence(seedID: 12).Should().NotEqual(first, "the stream is keyed to the router's seed ID");
    }

    [Fact]
    public void AShortestQueueRouter_PicksTheQueueWithFewestItems_AndATieGoesToTheFirst()
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.ShortestQueue, seedID: 11, id: "r", name: "r");
        var q1 = new SimpleBuffer(model, QueueRule.FIFO, id: "q1", name: "q1");
        var q2 = new SimpleBuffer(model, QueueRule.FIFO, id: "q2", name: "q2");
        router.AddTarget(q1, length: () => q1.Count);
        router.AddTarget(q2, length: () => q2.Count);
        var counts = new List<(int, int)>();
        PutAt(model, 0, router, 5);                                   // q1, q2, q1, q2, q1
        model.Schedule(1, () =>
        {
            counts.Add((q1.Count, q2.Count));
            for (int i = 100; i < 103; i++) q1.Put(new SimpleEntity(model, "X" + i, "X" + i));
        });
        PutAt(model, 2, router, 4, first: 5);                         // all four to q2, the shorter one

        model.Run(3);

        counts.Should().Equal((3, 2));
        (q1.Count, q2.Count).Should().Be((6, 6));
    }

    [Fact]
    public void ATargetThatRefuses_MakesTheRouterRefuseVisibly()
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.Probabilistic, seedID: 11, id: "r", name: "r");
        var full = new SimpleBuffer(model, QueueRule.FIFO, id: "q", name: "q", maxCapacity: 1);
        router.AddTarget(full);
        var refused = new List<string>();
        router.NotifyItemNotAccepted = e => refused.Add(e.Identifier);
        var accepted = new List<bool>();
        model.Schedule(0, () =>
        {
            accepted.Add(router.Put(new SimpleEntity(model, "E0", "E0")));
            accepted.Add(router.Put(new SimpleEntity(model, "E1", "E1")));
        });

        model.Run(1);

        accepted.Should().Equal(true, false);
        refused.Should().Equal("E1");
        router.RefusedCount.Should().Be(1);
        router.Routed.Should().Equal(1);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AWeightIsAPositiveFiniteNumber(double weight)
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.Probabilistic, seedID: 11, id: "r", name: "r");

        Action add = () => router.AddTarget(new SimpleSink(model, id: "a", name: "a"), weight);

        add.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AShortestQueueRouter_NeedsEachTargetsLength()
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.ShortestQueue, seedID: 11, id: "r", name: "r");

        Action add = () => router.AddTarget(new SimpleSink(model, id: "a", name: "a"));

        add.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ARouterWithoutTargets_CannotRoute()
    {
        var model = new Model("m", 1, 0d);
        var router = new SimpleRouter(model, RoutingPolicy.Probabilistic, seedID: 11, id: "r", name: "r");

        Action put = () => router.Put(new SimpleEntity(model, "E0", "E0"));

        put.Should().Throw<InvalidOperationException>().Where(e => e.Message.Contains("r"));
    }
}
