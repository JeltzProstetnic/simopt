using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SimOpt.Simulation.Engine;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-129: the event list moved from a sorted dictionary of sorted dictionaries to a binary heap.
/// The contract tests here were written against the old list and pass on both; the allocation test
/// is what the heap was built for.
/// </summary>
public class EventSchedulerContractTests
{
    private sealed class Probe : IEventInstance
    {
        private readonly List<double>? log;
        private readonly System.Action? onRaise;

        public Probe(string name, List<double>? log = null, System.Action? onRaise = null, double priority = 0)
        {
            Name = name;
            this.log = log;
            this.onRaise = onRaise;
            Priority = new Priority(priority);
        }

        public string Name { get; set; }
        public Priority Priority { get; set; }
        public double Time { get; set; } = double.NaN;
        public int HandlerCount => 1;
        public bool Log { get; set; }
        public int Raised { get; private set; }

        public void Raise()
        {
            Raised++;
            log?.Add(Time);
            onRaise?.Invoke();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }

    private static void RunAll(EventScheduler scheduler)
    {
        while (!double.IsPositiveInfinity(scheduler.TimeOfNextScheduledEvent)) scheduler.ProcessNextPointInTime();
    }

    [Fact]
    public void AnInstanceScheduledTwice_FiresAtBothTimes()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        var fired = new List<double>();
        var probe = new Probe("p", fired);

        scheduler.Add(1, probe);
        scheduler.Add(2, probe);
        RunAll(scheduler);

        probe.Raised.Should().Be(2);
        scheduler.EventCounter.Should().Be(2);
    }

    [Fact]
    public void RemovingAnInstanceScheduledTwice_CancelsOnlyItsLatestSchedule()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        var probe = new Probe("p");

        scheduler.Add(1, probe);
        scheduler.Add(2, probe);
        scheduler.Remove(probe);

        scheduler.EventfulMomentsCount.Should().Be(1);
        scheduler.TimeOfNextScheduledEvent.Should().Be(1);
        RunAll(scheduler);
        probe.Raised.Should().Be(1);
    }

    [Fact]
    public void WhileAnInstantIsProcessed_TheNextScheduledTimeIsThatInstant()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        double seen = double.NaN;
        scheduler.Add(1, new Probe("last at 1", onRaise: () => seen = scheduler.TimeOfNextScheduledEvent));
        scheduler.Add(2, new Probe("later"));

        scheduler.ProcessNextPointInTime();

        seen.Should().Be(1, "a handler sees the instant being processed, not the next one");
        scheduler.TimeOfNextScheduledEvent.Should().Be(2);
    }

    [Fact]
    public void ARemovalDuringProcessing_LeavesTheNextScheduledTimeAtThatInstant()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        var later = new Probe("later");
        var last = new Probe("last");
        double seen = double.NaN;
        scheduler.Add(1, new Probe("remover", onRaise: () => { scheduler.Remove(later); seen = scheduler.TimeOfNextScheduledEvent; }));
        scheduler.Add(5, later);
        scheduler.Add(9, last);

        scheduler.ProcessNextPointInTime();

        seen.Should().Be(1);
        scheduler.TimeOfNextScheduledEvent.Should().Be(9);
        RunAll(scheduler);
        later.Raised.Should().Be(0);
        last.Raised.Should().Be(1);
    }

    [Fact]
    public void AnInterrupt_LeavesTheRestOfTheInstantScheduled()
    {
        var model = new Mock<IModel>();
        bool interrupt = false;
        model.Setup(m => m.IsInterruptRequested).Returns(() => interrupt);
        var scheduler = new EventScheduler(model.Object);
        var first = new Probe("first", onRaise: () => interrupt = true, priority: 0);
        var second = new Probe("second", priority: 1);
        scheduler.Add(1, first);
        scheduler.Add(1, second);
        scheduler.Add(2, new Probe("later"));

        scheduler.ProcessNextPointInTime();

        first.Raised.Should().Be(1);
        second.Raised.Should().Be(0);
        scheduler.TimeOfNextScheduledEvent.Should().Be(1, "the interrupted instant is not finished");
        scheduler.EventfulMomentsCount.Should().Be(2);
        scheduler.NextScheduledEvent.Should().BeSameAs(second);

        interrupt = false;
        scheduler.ProcessNextPointInTime();
        second.Raised.Should().Be(1);
        scheduler.TimeOfNextScheduledEvent.Should().Be(2);
    }

    [Fact]
    public void AnEventScheduledWithAPriorityObjectThatLaterChanges_KeepsItsPlace()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        var order = new List<string>();
        var a = new Probe("a", onRaise: () => order.Add("a"), priority: 1);
        var b = new Probe("b", onRaise: () => order.Add("b"), priority: 2);
        scheduler.Add(1, a);
        scheduler.Add(1, b);
        a.Priority.PriorityNumber = 3;   // after scheduling: must not reorder

        RunAll(scheduler);

        order.Should().Equal("a", "b");
    }

    [Fact]
    public void OnceWarm_SchedulingAndProcessingEventsAllocatesNothing()
    {
        var scheduler = new EventScheduler(new Model("m", 1, 0d));
        var probes = new Probe[64];
        for (int i = 0; i < probes.Length; i++) probes[i] = new Probe("p" + i, priority: i % 3);

        void Cycle(double t0)
        {
            // eight instants with eight events each, plus one removal per instant
            for (int i = 0; i < probes.Length; i++) scheduler.Add(t0 + 1 + i % 8, probes[i]);
            for (int i = 0; i < 8; i++) scheduler.Remove(probes[i]);
            RunAll(scheduler);
        }

        Cycle(0);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int c = 1; c <= 10; c++) Cycle(c * 100);
        long bytes = System.GC.GetAllocatedBytesForCurrentThread() - before;

        scheduler.EventCounter.Should().Be(11 * 56);
        bytes.Should().Be(0, "the event list is the innermost loop of every model");
    }
}
