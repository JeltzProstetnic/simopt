using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Basics.Utilities;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126 schema, slice 4: a process node nothing feeds repeats its steps beside the item flow (a
/// supplier, a breakdown), one instance from the start of every run to its end, drawing its step values
/// anew at the start of every cycle. <see cref="WaitUntilStep"/> waits until a stock's level is at most
/// or at least a figure, at once when it already is; a <see cref="PutStep"/> may fill a stock up to a
/// level. A node that would repeat its steps at one instant for ever stops the run with the reason.
/// </summary>
public class ProcessNodeRepeatTests
{
    private static IDistribution<double> Constant(double value)
    {
        var d = new ConstantDoubleDistribution();
        d.Configure(value);
        return d;
    }

    [Fact]
    public void AnUnfedNode_RepeatsItsSteps_FromTheStartOfEveryRun()
    {
        var model = new Model("m", 1, 0d);
        var bins = new Stock(model, id: "bins", name: "bins");
        var maker = new ProcessNode(model, StableHash.Of("maker"), new ProcessStep[]
        {
            new DelayStep("delay#1", Constant(2)),
            new PutStep("put#1", bins, 1),
        }, id: "maker", name: "maker");
        maker.Repeat();

        model.Run(9);
        bins.Level.Should().Be(4, "puts at 2, 4, 6 and 8");

        model.Reset(2);
        model.Run(5);
        bins.Level.Should().Be(2, "a reset empties the stock and starts the cycle again");
    }

    [Fact]
    public void ASupplier_WaitsUntilTheLevelIsLow_ThenFillsTheStockUpToItsLevel()
    {
        var model = new Model("m", 1, 0d);
        var soap = new Stock(model, initialLevel: 10, capacity: 10, id: "soap", name: "soap");
        var supplier = new ProcessNode(model, StableHash.Of("supplier"), new ProcessStep[]
        {
            new WaitUntilStep("low", soap, atMost: 3),
            new DelayStep("delay#1", Constant(5)),
            PutStep.UpTo("put#1", soap, 10),
        }, id: "supplier", name: "supplier");
        supplier.Repeat();
        model.StartProcess(Consumer, id: "consumer");
        var deliveries = new List<(double At, double Level)>();
        double last = 10;
        soap.LevelChangedEvent.AddHandler((_, level) =>
        {
            if (level > last) deliveries.Add((model.CurrentTime, level));
            last = level;
        });

        IEnumerable<Wait> Consumer(Process p)
        {
            while (true)
            {
                yield return p.Timeout(1);
                yield return soap.TakeWait(1);
            }
        }

        model.Run(30);

        // the level reaches 3 at 7 and 18; each delivery comes 5 later and fills the empty tank to 10
        deliveries.Should().Equal(new[] { (12d, 10d), (23d, 10d) });
    }

    [Fact]
    public void WaitUntilAtLeast_PassesAtOnceWhenTheLevelAlreadyIs()
    {
        var model = new Model("m", 1, 0d);
        var parts = new Stock(model, initialLevel: 5, id: "parts", name: "parts");
        var ticks = new List<double>();
        var watcher = new ProcessNode(model, StableHash.Of("watcher"), new ProcessStep[]
        {
            new WaitUntilStep("ready", parts, atLeast: 5),
            new DelayStep("delay#1", Constant(3)),
        }, id: "watcher", name: "watcher");
        watcher.ItemEnteredEvent.AddHandler((_, _) => ticks.Add(model.CurrentTime));
        watcher.Repeat();

        model.Run(10);

        ticks.Should().Equal(new[] { 0d, 3d, 6d, 9d }, "the level stays at 5, so every cycle passes the wait at once");
    }

    [Fact]
    public void APutUpToALevelAlreadyReached_PutsNothing()
    {
        var model = new Model("m", 1, 0d);
        var tank = new Stock(model, initialLevel: 8, capacity: 10, id: "tank", name: "tank");
        var topUp = new ProcessNode(model, StableHash.Of("top"), new ProcessStep[]
        {
            PutStep.UpTo("put#1", tank, 6),
            new DelayStep("delay#1", Constant(1)),
        }, id: "top", name: "top");
        topUp.Repeat();

        model.Run(5);

        tank.Level.Should().Be(8);
    }

    [Fact]
    public void ANodeThatWouldRepeatAtOneInstantForEver_StopsTheRun_SayingWhy()
    {
        var model = new Model("m", 1, 0d);
        var crew = new Resource(model, id: "crew", name: "crew");
        var spinner = new ProcessNode(model, StableHash.Of("spinner"), new ProcessStep[]
        {
            new SeizeStep("seize#1", crew),
            new ReleaseStep("release#1", crew),
        }, id: "spinner", name: "spinner");
        spinner.Repeat();

        Action run = () => model.Run(1);

        run.Should().Throw<InvalidOperationException>().Where(e => e.Message.Contains("spinner") && e.Message.Contains("one instant"));
    }

    [Fact]
    public void AWaitUntilNamesOneBound()
    {
        var model = new Model("m", 1, 0d);
        var soap = new Stock(model, id: "soap", name: "soap");

        Action neither = () => new WaitUntilStep("w", soap);
        Action both = () => new WaitUntilStep("w", soap, atMost: 3, atLeast: 1);

        neither.Should().Throw<ArgumentException>();
        both.Should().Throw<ArgumentException>();
    }
}
