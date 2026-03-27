using FluentAssertions;
using Moq;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

public class EventSchedulerTests
{
    private readonly Mock<IModel> _modelMock;
    private readonly EventScheduler _scheduler;

    public EventSchedulerTests()
    {
        _modelMock = new Mock<IModel>();
        _modelMock.Setup(m => m.IsInterruptRequested).Returns(false);
        _scheduler = new EventScheduler(_modelMock.Object);
    }

    private static Mock<IEventInstance> CreateEventMock(string name = "TestEvent", double priorityNumber = 0)
    {
        var priority = new Priority(priorityNumber, PriorityType.User);
        var evt = new Mock<IEventInstance>();
        evt.SetupProperty(e => e.Time);
        evt.Setup(e => e.Priority).Returns(priority);
        evt.Setup(e => e.Name).Returns(name);
        evt.Setup(e => e.HandlerCount).Returns(1);
        evt.Setup(e => e.Log).Returns(false);
        return evt;
    }

    [Fact]
    public void Constructor_CreatesEmptyScheduler()
    {
        _scheduler.EventfulMomentsCount.Should().Be(0);
        _scheduler.EventCounter.Should().Be(0);
        _scheduler.HandlerCounter.Should().Be(0);
    }

    [Fact]
    public void Add_SingleEvent_IncreasesCount()
    {
        var evt = CreateEventMock();
        _scheduler.Add(1.0, evt.Object);

        _scheduler.EventfulMomentsCount.Should().Be(1);
    }

    [Fact]
    public void Add_MultipleEventsAtSameTime_SingleMoment()
    {
        var evt1 = CreateEventMock("E1", 0);
        var evt2 = CreateEventMock("E2", 1);

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);

        _scheduler.EventfulMomentsCount.Should().Be(1);
    }

    [Fact]
    public void Add_EventsAtDifferentTimes_MultipleMoments()
    {
        var evt1 = CreateEventMock("E1");
        var evt2 = CreateEventMock("E2");

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(2.0, evt2.Object);

        _scheduler.EventfulMomentsCount.Should().Be(2);
    }

    [Fact]
    public void Add_SetsEventTime()
    {
        var evt = CreateEventMock();
        _scheduler.Add(5.0, evt.Object);

        evt.Object.Time.Should().Be(5.0);
    }

    [Fact]
    public void TimeOfNextScheduledEvent_ReturnsEarliestTime()
    {
        var evt1 = CreateEventMock("E1");
        var evt2 = CreateEventMock("E2");

        _scheduler.Add(3.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);

        _scheduler.TimeOfNextScheduledEvent.Should().Be(1.0);
    }

    [Fact]
    public void Remove_OneOfTwoEvents_DecreasesCount()
    {
        var evt1 = CreateEventMock("E1", 0);
        var evt2 = CreateEventMock("E2", 1);
        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(2.0, evt2.Object);
        _scheduler.EventfulMomentsCount.Should().Be(2);

        _scheduler.Remove(evt1.Object);
        _scheduler.EventfulMomentsCount.Should().Be(1);
    }

    [Fact]
    public void Remove_LastEvent_DoesNotThrow()
    {
        var evt = CreateEventMock();
        _scheduler.Add(1.0, evt.Object);

        var act = () => _scheduler.Remove(evt.Object);
        act.Should().NotThrow();
        _scheduler.EventfulMomentsCount.Should().Be(0);
    }

    [Fact]
    public void ProcessNextPointInTime_RaisesEventsInPriorityOrder()
    {
        var callOrder = new List<string>();

        var evt1 = CreateEventMock("Low", 10);
        evt1.Setup(e => e.Raise()).Callback(() => callOrder.Add("Low"));

        var evt2 = CreateEventMock("High", 1);
        evt2.Setup(e => e.Raise()).Callback(() => callOrder.Add("High"));

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);

        _scheduler.ProcessNextPointInTime();

        callOrder.Should().Equal("High", "Low");
    }

    [Fact]
    public void ProcessNextPointInTime_IncrementsEventCounter()
    {
        var evt = CreateEventMock();
        _scheduler.Add(1.0, evt.Object);

        _scheduler.ProcessNextPointInTime();

        _scheduler.EventCounter.Should().Be(1);
        _scheduler.HandlerCounter.Should().Be(1);
    }

    [Fact]
    public void ProcessNextPointInTime_RemovesProcessedMoment()
    {
        var evt = CreateEventMock();
        _scheduler.Add(1.0, evt.Object);

        _scheduler.ProcessNextPointInTime();

        _scheduler.EventfulMomentsCount.Should().Be(0);
    }

    [Fact]
    public void Reset_ClearsAllEvents()
    {
        var evt1 = CreateEventMock("E1");
        var evt2 = CreateEventMock("E2");
        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(2.0, evt2.Object);

        // Reset is internal, call via reflection
        var resetMethod = typeof(EventScheduler).GetMethod("Reset",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        resetMethod!.Invoke(_scheduler, null);

        _scheduler.EventfulMomentsCount.Should().Be(0);
        _scheduler.EventCounter.Should().Be(0);
    }

    [Fact]
    public void ResetEventCounter_ZeroesCountersOnly()
    {
        var evt = CreateEventMock();
        _scheduler.Add(1.0, evt.Object);
        _scheduler.ProcessNextPointInTime();
        _scheduler.EventCounter.Should().BeGreaterThan(0);

        _scheduler.ResetEventCounter();

        _scheduler.EventCounter.Should().Be(0);
        _scheduler.HandlerCounter.Should().Be(0);
    }

    // SIM-05: Remove duplicate-priority edge cases

    [Fact]
    public void Remove_SamePriorityNumber_RemovesCorrectEvent()
    {
        // Two events at same time with same priority number — each gets unique AddedOrder
        var evt1 = CreateEventMock("E1", 5);
        var evt2 = CreateEventMock("E2", 5);

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);
        _scheduler.EventfulMomentsCount.Should().Be(1);

        _scheduler.Remove(evt1.Object);

        // evt2 should still be scheduled — process and verify it fires
        var fired = new List<string>();
        evt2.Setup(e => e.Raise()).Callback(() => fired.Add("E2"));

        _scheduler.ProcessNextPointInTime();
        fired.Should().Equal("E2");
    }

    [Fact]
    public void Remove_SharedPriorityObject_BothEventsPreserved()
    {
        // Two events sharing the SAME Priority object — second Add must not overwrite first
        var sharedPriority = new Priority(0, PriorityType.User);
        var evt1 = new Mock<IEventInstance>();
        evt1.SetupProperty(e => e.Time);
        evt1.Setup(e => e.Priority).Returns(sharedPriority);
        evt1.Setup(e => e.Name).Returns("E1");
        evt1.Setup(e => e.HandlerCount).Returns(1);
        evt1.Setup(e => e.Log).Returns(false);

        var evt2 = new Mock<IEventInstance>();
        evt2.SetupProperty(e => e.Time);
        evt2.Setup(e => e.Priority).Returns(sharedPriority);
        evt2.Setup(e => e.Name).Returns("E2");
        evt2.Setup(e => e.HandlerCount).Returns(1);
        evt2.Setup(e => e.Log).Returns(false);

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);

        // Both events should be scheduled, not just the second one
        var fired = new List<string>();
        evt1.Setup(e => e.Raise()).Callback(() => fired.Add("E1"));
        evt2.Setup(e => e.Raise()).Callback(() => fired.Add("E2"));

        _scheduler.ProcessNextPointInTime();
        fired.Should().Contain("E1");
        fired.Should().Contain("E2");
        fired.Should().HaveCount(2);
    }

    [Fact]
    public void Remove_SharedPriority_RemovesOnlyTargetEvent()
    {
        // With shared Priority, removing one event must not affect the other
        var sharedPriority = new Priority(0, PriorityType.User);
        var evt1 = new Mock<IEventInstance>();
        evt1.SetupProperty(e => e.Time);
        evt1.Setup(e => e.Priority).Returns(sharedPriority);
        evt1.Setup(e => e.Name).Returns("E1");
        evt1.Setup(e => e.HandlerCount).Returns(1);
        evt1.Setup(e => e.Log).Returns(false);

        var evt2 = new Mock<IEventInstance>();
        evt2.SetupProperty(e => e.Time);
        evt2.Setup(e => e.Priority).Returns(sharedPriority);
        evt2.Setup(e => e.Name).Returns("E2");
        evt2.Setup(e => e.HandlerCount).Returns(1);
        evt2.Setup(e => e.Log).Returns(false);

        _scheduler.Add(1.0, evt1.Object);
        _scheduler.Add(1.0, evt2.Object);

        _scheduler.Remove(evt1.Object);

        // Only evt2 should fire
        var fired = new List<string>();
        evt2.Setup(e => e.Raise()).Callback(() => fired.Add("E2"));
        evt1.Setup(e => e.Raise()).Callback(() => fired.Add("E1"));

        _scheduler.ProcessNextPointInTime();
        fired.Should().Equal("E2");
    }
}
