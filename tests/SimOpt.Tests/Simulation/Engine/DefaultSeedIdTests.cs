using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-125: an entity without a seed ID was seeded from <c>Model.SeedGenerator.Next()</c> in creation
/// order, so a scenario that added or removed one block shifted the random streams of every block
/// created after it, and a paired comparison of scenario and baseline (same model seed) compared
/// different random numbers. An entity built with an explicit id now gets <c>StableHash.Of(id)</c>
/// as its seed ID unless the caller gives one. Entities with a generated id keep creation-order
/// seeds, because a generated id is itself a creation counter.
/// </summary>
public class DefaultSeedIdTests
{
    private static NegExponentialDistribution Exponential(double mean)
    {
        var d = new NegExponentialDistribution();
        d.Configure(1d / mean);
        return d;
    }

    /// <summary>The finish times of the first 50 items on a source-buffer-server-sink line.</summary>
    private static List<double> FinishTimes(bool extraBlockFirst, int? serverSeedId = null, string serverId = "srv")
    {
        var model = new Model("m", 7, 0d);
        if (extraBlockFirst)
        {
            // A block a scenario might add, built before everything else.
            var extra = new SimpleSource(model, Exponential(5), autoStartDelay: 0d, id: "extra", name: "extra");
            new SimpleSink(model, id: "extraSink", name: "extraSink").ConnectTo(extra);
        }
        int n = 0;
        var source = new SimpleSource(model, Exponential(10), () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: int.MaxValue);
        var server = serverSeedId is int s
            ? new SimpleServer(model, s, Exponential(8), id: serverId, name: serverId, createProduct: m => m[0]) { AutoContinue = true }
            : new SimpleServer(model, Exponential(8), id: serverId, name: serverId, createProduct: m => m[0]) { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);
        var finishes = new List<double>();
        server.EntityFinishedEvent.AddHandler((_, _) => { if (finishes.Count < 50) finishes.Add(model.CurrentTime); });

        model.Reset(7);
        model.Run(2000);
        return finishes;
    }

    [Fact]
    public void AddingABlock_LeavesTheOtherBlocksRandomStreamsAsTheyWere()
    {
        var baseline = FinishTimes(extraBlockFirst: false);
        var scenario = FinishTimes(extraBlockFirst: true);

        baseline.Should().HaveCount(50);
        scenario.Should().Equal(baseline,
            "the source and server have explicit ids, so their streams must not depend on what else was built before them");
    }

    [Fact]
    public void TheDefaultSeedId_IsTheStableHashOfTheId_AndAnExplicitSeedIdWins()
    {
        var byDefault = FinishTimes(extraBlockFirst: false, serverId: "srv");
        var explicitSameSeed = FinishTimes(extraBlockFirst: false, serverSeedId: StableHash.Of("srv"), serverId: "renamed");
        var explicitOtherSeed = FinishTimes(extraBlockFirst: false, serverSeedId: 12345, serverId: "srv");

        explicitSameSeed.Should().Equal(byDefault, "an explicit seed ID equal to the hash of 'srv' reproduces the default of 'srv'");
        explicitOtherSeed.Should().NotEqual(byDefault, "an explicit seed ID replaces the default");
    }

    [Fact]
    public void AnEntityWithAnExplicitId_ReportsItsDefaultSeedId_AndOneWithAGeneratedIdHasNone()
    {
        var model = new Model("m", 1, 0d);
        var named = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10);
        var unnamed = new SimpleBuffer(model, QueueRule.FIFO, maxCapacity: 10);

        named.SeedID.Should().Be(StableHash.Of("buf"));
        unnamed.SeedID.Should().BeNull("a generated id is a creation counter, so hashing it would not help");
    }
}
