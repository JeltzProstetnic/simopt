using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126 schema, slice 2: the engine side of a topology's <c>process</c> node. Items pushed into it
/// (as into a delay) each run through a closed list of steps, any number at once, and are passed on
/// after the last. An item leaving frees every unit it still holds; <see cref="ReleaseStep"/> frees one
/// earlier. Every random step value is drawn when the item enters, from a stream the node owns per
/// step and field, seeded from the node's seed and the step's id, so inserting a step does not move
/// the other steps' numbers.
/// </summary>
public class ProcessNodeTests
{
    private static IDistribution<double> Constant(double value)
    {
        var d = new ConstantDoubleDistribution();
        d.Configure(value);
        return d;
    }

    private static IDistribution<double> Uniform(double min, double max)
    {
        var d = new UniformDoubleDistribution();
        d.Configure(min, max);
        return d;
    }

    private sealed class Out
    {
        public readonly List<(string Item, double At)> Left = new();
        public void Watch(ProcessNode node) => node.EntityCreatedEvent.AddHandler((_, item) => Left.Add((item.Identifier, node.Model.CurrentTime)));
    }

    /// <summary>Puts a new item into <paramref name="node"/> at each of <paramref name="times"/>.</summary>
    private static void Feed(Model model, ProcessNode node, params double[] times)
    {
        int n = 0;
        foreach (double t in times)
        {
            string id = "item" + (++n);
            model.StartProcess(p => Arrive(p, t, id, node), id: "feed-" + id);
        }
    }

    private static IEnumerable<Wait> Arrive(Process p, double at, string id, ProcessNode node)
    {
        if (at > 0) yield return p.Timeout(at);
        node.Put(new SimpleEntity(p.Model, id, id));
    }

    [Fact]
    public void AnItem_RunsTheStepsInOrder_AndIsPassedOnAfterTheLast()
    {
        var model = new Model("m", 1, 0d);
        var node = new ProcessNode(model, StableHash.Of("p"), new ProcessStep[]
        {
            new DelayStep("a", Constant(2)),
            new DelayStep("b", Constant(3)),
        }, id: "p", name: "p");
        var output = new Out();
        output.Watch(node);
        Feed(model, node, 0, 1);

        model.Run(10);

        output.Left.Should().Equal(("item1", 5d), ("item2", 6d));
        node.Count.Should().Be(0);
    }

    [Fact]
    public void LeavingFreesEveryUnitTheItemHolds()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, capacity: 1, id: "bay", name: "bay");
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[]
        {
            new SeizeStep("seize#1", bay),
            new DelayStep("washing", Constant(2)),
        }, id: "wash", name: "wash");
        var output = new Out();
        output.Watch(node);
        Feed(model, node, 0, 0, 0);

        model.Run(5);

        output.Left.Should().Equal(("item1", 2d), ("item2", 4d));
        bay.InUse.Should().Be(1, "the third item holds the bay from 4");
        node.Count.Should().Be(1);
    }

    [Fact]
    public void AReleaseStep_FreesTheUnitBeforeTheItemIsDone()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, capacity: 1, id: "bay", name: "bay");
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[]
        {
            new SeizeStep("seize#1", bay),
            new DelayStep("washing", Constant(2)),
            new ReleaseStep("release#1", bay),
            new DelayStep("paying", Constant(5)),
        }, id: "wash", name: "wash");
        var output = new Out();
        output.Watch(node);
        Feed(model, node, 0, 0);

        model.Run(20);

        output.Left.Should().Equal(new[] { ("item1", 7d), ("item2", 9d) }, "the second item washes from 2, when the first pays");
    }

    [Fact]
    public void ATakeAndAPut_MoveMaterial_AndATakeWaitsUntilTheWholeAmountIsThere()
    {
        var model = new Model("m", 1, 0d);
        var parts = new Stock(model, initialLevel: 3, id: "parts", name: "parts");
        var bins = new Stock(model, id: "bins", name: "bins");
        var node = new ProcessNode(model, StableHash.Of("assembly"), new ProcessStep[]
        {
            new TakeStep("take#1", parts, 2),
            new DelayStep("assembling", Constant(1)),
            new PutStep("put#1", bins, Constant(1)),
        }, id: "assembly", name: "assembly");
        var output = new Out();
        output.Watch(node);
        Feed(model, node, 0, 0);
        model.StartProcess(Restock, id: "restock");

        IEnumerable<Wait> Restock(Process p)
        {
            yield return p.Timeout(4);
            yield return parts.PutWait(5);
        }

        model.Run(10);

        output.Left.Should().Equal(new[] { ("item1", 1d), ("item2", 5d) }, "the second item waits for the delivery at 4");
        parts.Level.Should().Be(3 - 2 + 5 - 2);
        bins.Level.Should().Be(2);
    }

    [Fact]
    public void ReleasingAResourceTheItemDoesNotHoldThere_IsRefused()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");

        System.Action build = () => new ProcessNode(model, StableHash.Of("p"), new ProcessStep[]
        {
            new ReleaseStep("release#1", bay),
            new SeizeStep("seize#1", bay),
        }, id: "p", name: "p");

        build.Should().Throw<System.ArgumentException>().Where(e => e.Message.Contains("release#1") && e.Message.Contains("bay"));
    }

    /// <summary>The duration each item drew at the step with id <paramref name="step"/>.</summary>
    private static List<double> Durations(ProcessStep[] steps, string step, int items)
    {
        var model = new Model("m", 2026, 0d);
        var node = new ProcessNode(model, StableHash.Of("wash"), steps, id: "wash", name: "wash");
        var drawn = new List<double>();
        node.ItemEnteredEvent.AddHandler((_, values) => drawn.Add(values[step + ".duration"]));
        Feed(model, node, Enumerable.Range(0, items).Select(i => (double)i).ToArray());
        model.Run(items + 100);
        return drawn;
    }

    [Fact]
    public void StepValues_AreDrawnOnEntry_FromStreamsKeyedToTheStepId()
    {
        List<double> alone = Durations(new ProcessStep[] { new DelayStep("washing", Uniform(4, 6)) }, "washing", 20);
        List<double> afterAnInsertedStep = Durations(new ProcessStep[]
        {
            new DelayStep("queueing", Uniform(0, 1)),
            new DelayStep("washing", Uniform(4, 6)),
        }, "washing", 20);

        alone.Should().HaveCount(20).And.OnlyContain(v => v >= 4 && v <= 6);
        alone.Distinct().Should().HaveCount(20);
        afterAnInsertedStep.Should().Equal(alone, "the washing stream is keyed to the node and the step id, not to its position");
    }

    [Fact]
    public void AReset_EmptiesTheNode_AndReseedsItsStreams_AsARebuiltModelWould()
    {
        var model = new Model("m", 5, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[]
        {
            new SeizeStep("seize#1", bay),
            new DelayStep("washing", Uniform(4, 6)),
        }, id: "wash", name: "wash");
        var drawn = new List<double>();
        node.ItemEnteredEvent.AddHandler((_, values) => drawn.Add(values["washing.duration"]));
        Feed(model, node, 0, 0, 0);

        model.Reset(9);
        model.Run(3);
        List<double> first = drawn.ToList();
        node.Count.Should().Be(3);
        model.Reset(9);

        node.Count.Should().Be(0);
        bay.InUse.Should().Be(0);
        drawn.Clear();
        model.Run(3);
        drawn.Should().Equal(first);
    }
}
