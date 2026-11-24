using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-124: every entity registers with its model through <c>Model.AddEntity</c>, and nothing ever
/// unregistered the items a run creates. A model that is reset and rerun (replications, every
/// evaluation of an optimisation) kept every item of every earlier run in <c>Model.Items</c>:
/// measured +1.7 MB of live heap per replication of ~8,600 items, and each <c>Reset</c> walked all
/// of them. A generator that restarts its ids per replication threw on the duplicate key.
///
/// <para>
/// The rule: an entity registered while the model is running or resetting was made by the
/// simulation and is transient. A sink unregisters a transient item when it absorbs it, and
/// <c>Reset</c> unregisters every transient item still registered. Entities registered while the
/// model is stopped, which is how a model's blocks and any preloaded items are built, are structure
/// and stay registered.
/// </para>
/// </summary>
public class TransientEntityRetentionTests
{
    private static ConstantDoubleDistribution Constant(double v) => new(v, initialize: false);

    /// <summary>Source every 1, FIFO buffer, server taking <paramref name="service"/>, sink.</summary>
    private static (Model Model, SimpleBuffer Buffer, SimpleSink Sink, List<SimpleEntity> Created) Line(
        double service, Func<Model, Func<SimpleEntity>>? generator = null)
    {
        var model = new Model("m", 1, 0d);
        var created = new List<SimpleEntity>();
        int n = 0;
        Func<SimpleEntity> make = generator?.Invoke(model) ?? (() => new SimpleEntity(model, $"E{++n}", $"E{n}"));
        var source = new SimpleSource(model, Constant(1),
            () => { var e = make(); created.Add(e); return e; },
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: int.MaxValue);
        var server = new SimpleServer(model, Constant(service), id: "srv", name: "srv", createProduct: m => m[0])
        { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");
        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);
        return (model, buffer, sink, created);
    }

    [Fact]
    public void RepeatedReplications_DoNotAccumulateEntities()
    {
        var (model, _, sink, _) = Line(service: 0.5);
        var registeredAfterReset = new List<int>();

        for (int r = 0; r < 5; r++)
        {
            model.Reset(r + 1);
            registeredAfterReset.Add(model.Items.Count);
            model.Run(100);
            sink.Count.Should().BeGreaterThan(90, "about one item a time unit reaches the sink");
        }

        registeredAfterReset.Distinct().Should().ContainSingle(
            "a reset must leave only the model's structure registered, not the items of earlier runs");
    }

    [Fact]
    public void TheSinkUnregistersWhatItAbsorbs()
    {
        var (model, _, sink, _) = Line(service: 0.5);
        var absorbed = new List<string>();
        sink.ItemReceived.AddHandler((_, item) => absorbed.Add(item.Identifier));
        model.Reset(1);
        int structure = model.Items.Count;

        model.Run(100);

        absorbed.Should().HaveCountGreaterThan(90);
        absorbed.Where(model.HasEntity).Should().BeEmpty("an absorbed item is finished and must not stay registered");
        model.Items.Count.Should().BeLessThanOrEqualTo(structure + 2,
            "only the item in service and the one just created may still be registered");
    }

    [Fact]
    public void ItemsLeftInTheSystemAtTheEndOfARun_AreUnregisteredByTheNextReset()
    {
        var (model, buffer, _, created) = Line(service: 2); // the queue grows by one every two time units
        model.Reset(1);
        model.Run(100);
        buffer.Count.Should().BeGreaterThan(10);
        var leftOver = created.Select(e => e.Identifier).Where(model.HasEntity).ToList();
        leftOver.Should().NotBeEmpty();

        model.Reset(2);

        leftOver.Where(model.HasEntity).Should().BeEmpty("a reset discards the items of the previous run");
    }

    [Fact]
    public void StructureAndPreloadedItems_StayRegistered_EvenWhenAddedAfterARun()
    {
        var (model, buffer, _, _) = Line(service: 0.5);
        var preloaded = new SimpleEntity(model, "preloaded", "preloaded");
        model.Reset(1);
        model.Run(10);
        var addedBetweenRuns = new SimpleSink(model, id: "snk2", name: "snk2");

        model.Reset(2);

        foreach (string id in new[] { "src", "buf", "srv", "snk", "preloaded", "snk2" })
            model.HasEntity(id).Should().BeTrue($"'{id}' was built while the model was stopped");
        model.FindEntities<SimpleSink>().Should().Contain(addedBetweenRuns);
    }

    [Fact]
    public void AGeneratorThatRestartsItsIdsEachReplication_DoesNotCollideWithEarlierRuns()
    {
        int n = 0;
        var (model, _, sink, _) = Line(service: 0.5,
            generator: m => () => new SimpleEntity(m, $"item{++n}", $"item{n}"));
        var counts = new List<int>();

        for (int r = 0; r < 3; r++)
        {
            n = 0;
            model.Reset(1);
            model.Run(50);
            counts.Add(sink.Count);
        }

        counts.Distinct().Should().ContainSingle("the same seed and the same ids give the same run every time");
    }
}
