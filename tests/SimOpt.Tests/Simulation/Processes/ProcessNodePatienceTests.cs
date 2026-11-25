using System;
using System.Collections.Generic;
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
/// SIM-126 schema, slice 3: patience on a <see cref="SeizeStep"/> or a <see cref="TakeStep"/>. An
/// item that has waited its patience (drawn on entry) gives up: its request leaves the queue, it frees
/// every unit it holds, and it leaves by the step's named outlet instead of the node's normal exit.
/// Patience and outlet come together or not at all.
/// </summary>
public class ProcessNodePatienceTests
{
    private static IDistribution<double> Constant(double value)
    {
        var d = new ConstantDoubleDistribution();
        d.Configure(value);
        return d;
    }

    private sealed class Exits
    {
        public readonly List<(string Item, double At, string Exit)> Left = new();

        public Exits(ProcessNode node, params string[] outlets)
        {
            node.EntityCreatedEvent.AddHandler((_, item) => Left.Add((item.Identifier, node.Model.CurrentTime, "done")));
            foreach (string outlet in outlets)
                node.Outlet(outlet).EntityCreatedEvent.AddHandler((_, item) => Left.Add((item.Identifier, node.Model.CurrentTime, outlet)));
        }
    }

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
    public void AnItemThatWaitedItsPatience_GivesUp_AndLeavesByTheOutlet()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, capacity: 1, id: "bay", name: "bay");
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[]
        {
            new SeizeStep("seize#1", bay, Constant(3), "gave_up"),
            new DelayStep("washing", Constant(10)),
        }, id: "wash", name: "wash");
        var exits = new Exits(node, "gave_up");
        Feed(model, node, 0, 1, 9);

        model.Run(30);

        exits.Left.Should().Equal(new[] { ("item2", 4d, "gave_up"), ("item1", 10d, "done"), ("item3", 20d, "done") },
            "item2 waits from 1 to 4 and gives up; item3 waits from 9 and is served at 10, within its 3");
        bay.Waiting.Should().Be(0);
        bay.InUse.Should().Be(0);
        node.Count.Should().Be(0);
    }

    [Fact]
    public void GivingUp_FreesTheUnitsTheItemAlreadyHolds()
    {
        var model = new Model("m", 1, 0d);
        var room = new Resource(model, capacity: 2, id: "room", name: "room");
        var doctor = new Resource(model, capacity: 1, id: "doctor", name: "doctor");
        var node = new ProcessNode(model, StableHash.Of("exam"), new ProcessStep[]
        {
            new SeizeStep("seize#1", room),
            new SeizeStep("seize#2", doctor, Constant(2), "left"),
            new DelayStep("exam", Constant(10)),
        }, id: "exam", name: "exam");
        var exits = new Exits(node, "left");
        Feed(model, node, 0, 0);

        model.Run(5);

        exits.Left.Should().Equal(new[] { ("item2", 2d, "left") });
        room.InUse.Should().Be(1, "the item that left gave its room back");
    }

    [Fact]
    public void APatientTake_GivesUpWhenTheStockStaysShort()
    {
        var model = new Model("m", 1, 0d);
        var parts = new Stock(model, initialLevel: 1, id: "parts", name: "parts");
        var node = new ProcessNode(model, StableHash.Of("assembly"), new ProcessStep[]
        {
            new TakeStep("take#1", parts, 1, Constant(5), "short"),
            new DelayStep("assembling", Constant(1)),
        }, id: "assembly", name: "assembly");
        var exits = new Exits(node, "short");
        Feed(model, node, 0, 0);

        model.Run(10);

        exits.Left.Should().Equal(new[] { ("item1", 1d, "done"), ("item2", 5d, "short") });
        parts.WaitingTakers.Should().Be(0);
        parts.Level.Should().Be(0);
    }

    [Fact]
    public void PatienceIsDrawnOnEntry_FromItsOwnStream()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");
        var uniform = new UniformDoubleDistribution();
        uniform.Configure(5, 20);
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[]
        {
            new SeizeStep("seize#1", bay, uniform, "gave_up"),
            new DelayStep("washing", Constant(4)),
        }, id: "wash", name: "wash");
        var drawn = new List<double>();
        node.ItemEnteredEvent.AddHandler((_, values) => drawn.Add(values["seize#1.patience"]));
        Feed(model, node, 0, 0, 0);

        model.Run(1);

        drawn.Should().HaveCount(3).And.OnlyContain(v => v >= 5 && v <= 20);
    }

    [Fact]
    public void PatienceWithoutAnOutlet_OrAnOutletWithoutPatience_IsRefused()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");

        Action noOutlet = () => new SeizeStep("seize#1", bay, Constant(3), null!);
        Action noPatience = () => new SeizeStep("seize#1", bay, null!, "gave_up");

        noOutlet.Should().Throw<ArgumentException>().Where(e => e.Message.Contains("outlet"));
        noPatience.Should().Throw<ArgumentException>().Where(e => e.Message.Contains("patience"));
    }

    [Fact]
    public void AnOutletNoStepNames_IsRefused()
    {
        var model = new Model("m", 1, 0d);
        var bay = new Resource(model, id: "bay", name: "bay");
        var node = new ProcessNode(model, StableHash.Of("wash"), new ProcessStep[] { new SeizeStep("seize#1", bay) }, id: "wash", name: "wash");

        Action outlet = () => node.Outlet("gave_up");

        outlet.Should().Throw<ArgumentException>().Where(e => e.Message.Contains("gave_up"));
    }
}
