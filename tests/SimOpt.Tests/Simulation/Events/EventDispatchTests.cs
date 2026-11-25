using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Events;
using Xunit;

namespace SimOpt.Tests.Simulation.Events;

/// <summary>
/// SIM-129: raising an event enumerated its sorted handler dictionary, which allocates an
/// enumerator stack on every raise, and a scheduled instance wrapped its arguments in a tuple
/// through a delegate made per instance. Every event of a run paid for both. The dispatch contract
/// below held before the change and holds after it; the allocation tests are what changed.
/// </summary>
public class EventDispatchTests
{
    private static long AllocatedBy(Action action)
    {
        action();
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void HandlersRunInPriorityOrder_TiesInTheOrderAdded()
    {
        var evnt = new UnaryEvent<int>("u");
        var calls = new List<string>();
        evnt.AddHandler(_ => calls.Add("late"), new Priority(2));
        evnt.AddHandler(_ => calls.Add("first"), new Priority(1));
        evnt.AddHandler(_ => calls.Add("second"), new Priority(1));

        evnt.Raise(0);

        calls.Should().Equal("first", "second", "late");
    }

    [Fact]
    public void AHandlerThatDetachesItself_RunsThisTimeAndNeverAgain()
    {
        var evnt = new BinaryEvent<object, int>("b");
        var calls = new List<string>();
        evnt.AddHandler((_, _) => { calls.Add("once"); evnt.DetachCurrentHandler(); });
        evnt.AddHandler((_, _) => calls.Add("always"));

        evnt.Raise(this, 1);
        evnt.Raise(this, 2);

        calls.Should().Equal("once", "always", "always");
        evnt.HandlerCount.Should().Be(1);
    }

    [Fact]
    public void HandlersAddedOrRemovedBetweenRaises_AreSeenByTheNextRaise()
    {
        var evnt = new UnaryEvent<int>("u");
        var seen = new List<string>();
        Action<int> a = _ => seen.Add("a");
        Action<int> b = _ => seen.Add("b");
        evnt.AddHandler(a);
        evnt.Raise(0);
        evnt.AddHandler(b);
        evnt.Raise(0);
        evnt.RemoveHandler(a);
        evnt.Raise(0);
        evnt.ClearHandlers();
        evnt.Raise(0);

        seen.Should().Equal("a", "a", "b", "b");
    }

    [Fact]
    public void ClearingTheHandlerDictionaryDirectly_IsSeenByTheNextRaise()
    {
        // examples/SimOpt.Examples.SupplyChain clears Handlers directly between simulations
        var evnt = new UnaryEvent<int>("u");
        int calls = 0;
        evnt.AddHandler(_ => calls++);
        evnt.Raise(0);

        evnt.Handlers.Clear();
        evnt.Raise(0);

        calls.Should().Be(1);
    }

    [Fact]
    public void EveryInstanceForm_DeliversItsValuesAtRaiseTime()
    {
        var unary = new UnaryEvent<int>("u");
        var binary = new BinaryEvent<object, int>("b");
        var ternary = new TernaryEvent<object, int, string>("t");
        var got = new List<string>();
        unary.AddHandler(d => got.Add($"u{d}"));
        binary.AddHandler((s, a) => got.Add($"b{s}{a}"));
        ternary.AddHandler((s, a, d) => got.Add($"t{s}{a}{d}"));
        int late = 0;

        var instances = new IEventInstance[]
        {
            unary.GetInstance(1),
            unary.GetInstance(() => late),
            binary.GetInstance("s", 2),
            binary.GetInstance("s", () => late),
            binary.GetInstance(() => Tuple.Create<object, int>("x", late)),
            ternary.GetInstance("s", 3, "d"),
            ternary.GetInstance("s", 4, () => "late" + late),
            ternary.GetInstance("s", () => Tuple.Create(late, "e")),
            ternary.GetInstance(() => Tuple.Create<object, int, string>("y", late, "f")),
        };
        late = 7;
        foreach (IEventInstance i in instances) i.Raise();

        got.Should().Equal("u1", "u7", "bs2", "bs7", "bx7", "ts3d", "ts4late7", "ts7e", "ty7f");
    }

    [Fact]
    public void RaisingAnEventWithHandlers_AllocatesNothing()
    {
        var unary = new UnaryEvent<int>("u");
        var binary = new BinaryEvent<object, int>("b");
        var ternary = new TernaryEvent<object, int, int>("t");
        int sum = 0;
        for (int h = 0; h < 3; h++)
        {
            int k = h;   // a closure per handler: equal delegates count as the same handler
            unary.AddHandler(d => sum += d + k);
            binary.AddHandler((_, a) => sum += a + k);
            ternary.AddHandler((_, a, d) => sum += a + d + k);
        }
        object sender = new object();

        long bytes = AllocatedBy(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                unary.Raise(i);
                binary.Raise(sender, i);
                ternary.Raise(sender, i, 1);
            }
        });

        bytes.Should().Be(0, "every event of every run is raised through these loops");
    }

    [Fact]
    public void RaisingAnInstanceBuiltWithItsValues_AllocatesNothing()
    {
        var unary = new UnaryEvent<int>("u");
        var binary = new BinaryEvent<object, int>("b");
        var ternary = new TernaryEvent<object, int, int>("t");
        int sum = 0;
        unary.AddHandler(d => sum += d);
        binary.AddHandler((_, a) => sum += a);
        ternary.AddHandler((_, a, d) => sum += a + d);
        object sender = new object();
        IEventInstance u = unary.GetInstance(1), b = binary.GetInstance(sender, 2), t = ternary.GetInstance(sender, 3, 4);

        long bytes = AllocatedBy(() =>
        {
            for (int i = 0; i < 1000; i++) { u.Raise(); b.Raise(); t.Raise(); }
        });

        bytes.Should().Be(0, "an instance that holds its values needs no tuple to hand them over");
    }
}
