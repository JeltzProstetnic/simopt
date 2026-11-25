using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-132: <c>Delay&lt;T&gt;</c> held one item, and <c>Put</c> returned false while it was inside. The
/// handler <c>ConnectTo(IItemSource)</c> attaches ignores that return value, so a second item arriving
/// during a delay vanished without a trace (the SIM-106 defect class). A delay now has a capacity
/// (default 1, the old behaviour, so nothing downstream moves; <c>int.MaxValue</c> gives every item
/// its own timer, which is what the SIM-65 schema's <c>delay</c> node means), and a refusal is visible:
/// it is counted and reported through <c>NotifyItemNotAccepted</c>, as <c>Buffer</c> does.
/// </summary>
public class DelayTests
{
    private static Delay<string> NewDelay(Model model, double duration, string initialItem = null) =>
        new Delay<string>(model, new ConstantDoubleDistribution(duration, initialize: false),
                          id: "delay", name: "delay", initialItem: initialItem);

    private static List<(string Item, double Time)> RecordReleases(Model model, Delay<string> delay)
    {
        var log = new List<(string, double)>();
        delay.ItemReleasedSimple.AddHandler((sender, item) => log.Add((item, model.CurrentTime)));
        return log;
    }

    [Fact]
    public void TheDefaultCapacityIsOne_AndASecondItemIsRefusedVisibly()
    {
        var model = new Model("m", 1, 0d);
        var delay = NewDelay(model, 5);
        var refused = new List<string>();
        delay.NotifyItemNotAccepted = refused.Add;
        var released = RecordReleases(model, delay);
        var accepted = new List<bool>();
        model.Schedule(1, () => { accepted.Add(delay.Put("a")); accepted.Add(delay.Put("b")); });

        model.Run(10);

        delay.Capacity.Should().Be(1);
        accepted.Should().Equal(true, false);
        refused.Should().Equal("b");
        delay.RefusedCount.Should().Be(1);
        released.Should().Equal(("a", 6d));
        delay.Count.Should().Be(0);
    }

    [Fact]
    public void ACapacityOfThree_HoldsThreeItems_EachOnItsOwnTimer()
    {
        var model = new Model("m", 1, 0d);
        var delay = NewDelay(model, 5);
        delay.Capacity = 3;
        var refused = new List<string>();
        delay.NotifyItemNotAccepted = refused.Add;
        var released = RecordReleases(model, delay);
        var counts = new List<int>();
        model.Schedule(0, () => delay.Put("a"));
        model.Schedule(1, () => delay.Put("b"));
        model.Schedule(2, () => delay.Put("c"));
        model.Schedule(2.5, () => delay.Put("d"));   // full
        model.Schedule(3, () => counts.Add(delay.Count));
        model.Schedule(5.5, () => counts.Add(delay.Count));
        model.Schedule(6, () => delay.Put("e"));     // a left at 5, so there is room again

        model.Run(20);

        released.Should().Equal(("a", 5d), ("b", 6d), ("c", 7d), ("e", 11d));
        refused.Should().Equal("d");
        counts.Should().Equal(3, 2);
        delay.Count.Should().Be(0);
    }

    [Fact]
    public void AnUnlimitedCapacity_GivesEveryItemItsOwnDrawnDuration()
    {
        var model = new Model("m", 1, 0d);
        var delay = new Delay<string>(model, new UniformDoubleDistribution(1d, 9d), seedID: 7, id: "delay", name: "delay");
        delay.Capacity = int.MaxValue;
        var releases = new List<(string Item, double Time, double Drawn)>();
        delay.ItemReleased.AddHandler((sender, item, drawn) => releases.Add((item, model.CurrentTime, drawn)));
        var items = Enumerable.Range(0, 200).Select(i => "item" + i).ToList();
        model.Schedule(0, () => items.ForEach(i => delay.Put(i)));

        model.Run(20);

        releases.Select(r => r.Item).Should().BeEquivalentTo(items);
        releases.Should().OnlyContain(r => Math.Abs(r.Time - r.Drawn) < 1e-12,
            "every item put at t = 0 leaves after its own draw");
        releases.Min(r => r.Time).Should().BeLessThan(3);
        releases.Max(r => r.Time).Should().BeGreaterThan(7);
        delay.RefusedCount.Should().Be(0);
        delay.Count.Should().Be(0);
    }

    [Fact]
    public void ResetDropsTheItemsInFlight_AndTheRefusals_ButKeepsTheCapacity()
    {
        var model = new Model("m", 1, 0d);
        var delay = NewDelay(model, 5);
        delay.Capacity = 2;
        var released = RecordReleases(model, delay);
        model.Schedule(1, () => { delay.Put("a"); delay.Put("b"); delay.Put("c"); });
        model.Run(3);
        delay.Count.Should().Be(2);
        delay.RefusedCount.Should().Be(1);

        model.Reset(2);

        delay.Count.Should().Be(0);
        delay.RefusedCount.Should().Be(0);
        delay.Capacity.Should().Be(2, "the capacity is configuration, not state");
        model.Schedule(10, () => { }); // a model refuses to run with an empty calendar
        model.Run(10);
        released.Should().BeEmpty("the items in flight belonged to the previous replication");
    }

    [Fact]
    public void TheCapacityIsAtLeastOne_AndNeverBelowTheItemsInside()
    {
        var model = new Model("m", 1, 0d);
        var delay = NewDelay(model, 5);

        var zero = () => delay.Capacity = 0;
        zero.Should().Throw<ArgumentOutOfRangeException>();

        delay.Capacity = 3;
        model.Schedule(1, () => { delay.Put("a"); delay.Put("b"); });
        model.Run(2);
        var belowInside = () => delay.Capacity = 1;
        belowInside.Should().Throw<InvalidOperationException>();
        delay.Capacity = 2;
        delay.Capacity.Should().Be(2);
    }

    [Fact]
    public void AnInitialItemCountsAgainstTheCapacity_AndIsReleasedAgainAfterAReset()
    {
        var model = new Model("m", 1, 0d);
        var delay = NewDelay(model, 5, initialItem: "init");
        var released = RecordReleases(model, delay);
        var accepted = new List<bool>();
        model.Schedule(1, () => accepted.Add(delay.Put("a")));
        delay.Count.Should().Be(1);

        model.Run(10);

        accepted.Should().Equal(false);
        released.Should().Equal(("init", 5d));

        released.Clear();
        model.Reset(2);
        delay.Count.Should().Be(1);
        model.Run(10);
        released.Should().Equal(("init", 5d));
    }

    [Fact]
    public void WiredFromAFasterSource_EveryItemIsReleasedRefusedOrStillInside()
    {
        var model = new Model("m", 42, 0d);
        var source = new Source(model, new ConstantDoubleDistribution(1.0, initialize: false), autoStartDelay: 0.0);
        var delay = new Delay(model, new ConstantDoubleDistribution(2.5, initialize: false), id: "delay", name: "delay");
        var sink = new Sink(model, name: "sink");
        var created = new List<double>();
        source.EntityCreatedEvent.AddHandler((sender, entity) => created.Add(model.CurrentTime));
        delay.ConnectTo((IItemSource<object>)source);
        delay.ConnectTo(sink);

        model.Run(10.5);

        // The delay takes the items of t = 1, 4, 7 and 10 and refuses the other six; three are out.
        created.Should().Equal(1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 10d);
        sink.Count.Should().Be(3);
        delay.Count.Should().Be(1);
        delay.RefusedCount.Should().Be(6);
        (sink.Count + delay.Count + delay.RefusedCount).Should().Be(created.Count);
    }
}
