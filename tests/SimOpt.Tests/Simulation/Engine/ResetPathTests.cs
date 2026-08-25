using System;
using System.Linq;
using FluentAssertions;
using Moq;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Events;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using SimOpt.Simulation.Tools;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

/// <summary>
/// SIM-58 — the reset path.
///
/// <para>
/// Reset-and-re-run is not a convenience here, it is the load-bearing loop: every
/// <c>IProblem.Evaluate</c> and every MCP <c>run_simulation</c> call resets the model and runs it
/// again. A defect that makes evaluation #2 differ from evaluation #1 does not announce itself —
/// it silently changes the fitness landscape the optimizer is searching, so the optimizer returns
/// a confident recommendation derived from a model that drifted underneath it.
/// </para>
/// <para>
/// The 2026-07-05 review called reset "comprehensive and deterministic" at the machinery level and
/// located the defects in the templates. These tests pin the template behaviour.
/// </para>
/// </summary>
public class ResetPathTests
{
    // ------------------------------------------------------------------------------------
    // Delay<T> — DES-1 (null initial item) and DES-2 (release never re-scheduled)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>Reset()</c> called <c>initialItem.Equals(...)</c>, which dereferences null whenever the
    /// item type is a reference type and no initial item was supplied — the common case. Reset
    /// runs on every evaluation, so this fires immediately.
    /// </summary>
    [Fact]
    public void Delay_Reset_WithNullInitialItem_DoesNotThrow()
    {
        var model = new Model("Test", 42, 0.0);
        _ = new Delay<string>(model, new ConstantDoubleDistribution(1.0, initialize: false));

        var act = () => model.Reset();

        act.Should().NotThrow<NullReferenceException>(
            "a Delay with no initial item is the ordinary case and Reset runs on every evaluation");
    }

    /// <summary>
    /// The decisive one. <c>InitializeDelay</c> schedules the initial item's release, but
    /// <c>Reset()</c> restored <c>hasItem</c> without re-scheduling it — so after any reset the
    /// item sat in the delay forever and blocked every <c>Put</c>. Evaluation #1 and evaluation #2
    /// of the same model therefore behaved differently, silently.
    /// </summary>
    [Fact]
    public void Delay_AfterReset_StillReleasesItsInitialItem()
    {
        var model = new Model("Test", 42, 0.0);
        var delay = new Delay<string>(
            model,
            new ConstantDoubleDistribution(1.0, initialize: false),
            initialItem: "initial");

        // First run: the initial item is released, so the delay is free to accept another.
        model.Run(2.5);
        delay.Put("first-run").Should().BeTrue("the initial item should have been released by t=1");

        model.Reset();
        model.Run(2.5);

        delay.Put("second-run").Should().BeTrue(
            "after a reset the initial item must be re-scheduled and released exactly as on the " +
            "first run; otherwise it blocks the delay forever and evaluation #2 diverges from #1");
    }

    /// <summary>
    /// The same defect stated as the property that actually matters to the optimizer: two
    /// consecutive evaluations of an unchanged model must agree.
    /// </summary>
    [Fact]
    public void Delay_ConsecutiveEvaluations_Agree()
    {
        static bool RunOnceAndProbe(Model model, Delay<string> delay)
        {
            model.Run(2.5);
            return delay.Put("probe");
        }

        var model = new Model("Test", 42, 0.0);
        var delay = new Delay<string>(
            model,
            new ConstantDoubleDistribution(1.0, initialize: false),
            initialItem: "initial");

        var first = RunOnceAndProbe(model, delay);
        model.Reset();
        var second = RunOnceAndProbe(model, delay);
        model.Reset();
        var third = RunOnceAndProbe(model, delay);

        second.Should().Be(first, "evaluation #2 must agree with evaluation #1");
        third.Should().Be(first, "evaluation #3 must agree with evaluation #1");
    }

    // ------------------------------------------------------------------------------------
    // ResourceManager — DES-4 (reset aliases its own snapshot)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>Reset()</c> assigned the snapshot list itself rather than a copy, so the snapshot and the
    /// live pool became the same object. The next <c>Manage</c>/<c>UnManage</c> then mutated the
    /// snapshot, and every subsequent reset restored the corrupted pool — capacity-planning models
    /// drift a little further from their baseline on each evaluation.
    /// </summary>
    [Fact]
    public void ResourceManager_Reset_RestoresTheOriginalPool_AcrossRepeatedResets()
    {
        static IResource FreeResource()
        {
            var mock = new Mock<IResource>();
            mock.Setup(r => r.Free).Returns(true);
            return mock.Object;
        }

        var manager = new ResourceManager();
        var first = FreeResource();
        var second = FreeResource();

        manager.Manage(first);
        manager.Manage(second);
        manager.Initialize();

        manager.ManagedResources.Should().HaveCount(2);

        // Simulate a run that removes a resource, then reset — the baseline must come back.
        manager.Reset();
        manager.UnManage(first);
        manager.ManagedResources.Should().HaveCount(1, "the run removed one resource");

        manager.Reset();
        manager.ManagedResources.Should().HaveCount(2,
            "reset must restore the initial pool");

        // And it must keep coming back — this is the half the aliasing broke.
        manager.UnManage(second);
        manager.Reset();
        manager.ManagedResources.Should().HaveCount(2,
            "the snapshot must not be mutated by activity after a previous reset");
    }

    // ------------------------------------------------------------------------------------
    // Server<> — DES-5 (ClearCurrentMaterial clears the wrong list)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// A copy-paste defect: <c>ClearCurrentMaterial()</c> cleared <c>activeMaterial</c>, the list
    /// <c>ClearActiveMaterial()</c> already owns. Work-in-progress accounting is wrong as a result.
    /// </summary>
    [Fact]
    public void Server_ClearCurrentMaterial_ClearsCurrentMaterial()
    {
        var model = new Model("Test", 42, 0.0);
        var server = new SimpleServer(model, new ConstantDoubleDistribution(1.0, initialize: false));

        server.CurrentMaterial.Add(new SimpleEntity(model));
        server.CurrentMaterial.Should().HaveCount(1);

        server.ClearCurrentMaterial();

        server.CurrentMaterial.Should().BeEmpty(
            "ClearCurrentMaterial must clear currentMaterial, not activeMaterial");
    }

    // ------------------------------------------------------------------------------------
    // EventScheduler — DES-3 (stale time / inconsistent sentinel)
    // ------------------------------------------------------------------------------------

    private static Mock<IEventInstance> CreateEventMock(double time, double priorityNumber = 0)
    {
        var evt = new Mock<IEventInstance>();
        evt.SetupProperty(e => e.Time);
        evt.Setup(e => e.Priority).Returns(new Priority(priorityNumber, SimOpt.Simulation.Enum.PriorityType.User));
        evt.Setup(e => e.Name).Returns("TestEvent");
        evt.Setup(e => e.HandlerCount).Returns(1);
        evt.Setup(e => e.Log).Returns(false);
        evt.Object.Time = time;
        return evt;
    }

    /// <summary>
    /// Removing the last event left <c>timeOfNextScheduledEvent</c> at its previous finite value.
    /// Any external consumer — the visualization, the logger, a progress reporter — then asked for
    /// the next event at a time that no longer has one.
    /// </summary>
    [Fact]
    public void EventScheduler_RemovingTheLastEvent_ReportsNoNextEvent()
    {
        var modelMock = new Mock<IModel>();
        modelMock.Setup(m => m.IsInterruptRequested).Returns(false);
        var scheduler = new EventScheduler(modelMock.Object);

        var evt = CreateEventMock(5.0);
        scheduler.Add(5.0, evt.Object);
        scheduler.TimeOfNextScheduledEvent.Should().Be(5.0);

        scheduler.Remove(evt.Object);

        scheduler.EventfulMomentsCount.Should().Be(0);
        double.IsPositiveInfinity(scheduler.TimeOfNextScheduledEvent).Should().BeTrue(
            "an empty schedule has no next event time, and the sentinel must be the same one the " +
            "rest of the class uses");

        var act = () => { _ = scheduler.NextScheduledEvent; };
        act.Should().NotThrow("querying an empty schedule must answer 'nothing', not throw");
        scheduler.NextScheduledEvent.Should().BeNull();
    }

    // ------------------------------------------------------------------------------------
    // Model.RemoveEvent — DES-6 (no same-time guard)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>ProcessNextPointInTime</c> iterates the events at the current time live. A handler that
    /// removes a sibling event at that same time mutates the collection being enumerated, and the
    /// run dies with an opaque "Collection was modified". <c>RemoveEvent</c> documented that it
    /// throws in exactly this case but never did; <c>TryRemoveEvent</c> has always guarded it.
    /// </summary>
    [Fact]
    public void Model_RemoveEvent_DuringProcessing_FailsWithADiagnosticNotACollectionError()
    {
        var model = new Model("Test", 42, 0.0);

        var first = new UnaryEvent<string>("First");
        var sibling = new UnaryEvent<string>("Sibling");
        var siblingInstance = sibling.GetInstance("sibling");

        // The handler of the first event tries to cancel a sibling scheduled at the same instant.
        first.AddHandler(_ => model.RemoveEvent(siblingInstance));

        model.AddEvent(1.0, first.GetInstance("first"));
        model.AddEvent(1.0, siblingInstance);

        var act = () => model.Run(2.0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*current point in time*",
                "the failure must name the actual problem rather than surfacing as a bare " +
                "collection-modified error from deep inside the scheduler");
    }

    /// <summary>
    /// The guard must not interfere with the ordinary case — cancelling a future event, which is
    /// what Server, Source and Sensor all do.
    /// </summary>
    [Fact]
    public void Model_RemoveEvent_ForAFutureEvent_StillWorksDuringProcessing()
    {
        var model = new Model("Test", 42, 0.0);

        var trigger = new UnaryEvent<string>("Trigger");
        var later = new UnaryEvent<string>("Later");
        var laterRaised = false;
        later.AddHandler(_ => laterRaised = true);
        var laterInstance = later.GetInstance("later");

        trigger.AddHandler(_ => model.RemoveEvent(laterInstance));

        model.AddEvent(1.0, trigger.GetInstance("trigger"));
        model.AddEvent(5.0, laterInstance);

        model.Run(10.0);

        laterRaised.Should().BeFalse("cancelling a future event from a handler is the ordinary case");
    }

    /// <summary>
    /// <c>Reset()</c> cleared the event list but never reset the cached next-event time, so a
    /// freshly reset model still advertised the last run's next event.
    /// </summary>
    [Fact]
    public void EventScheduler_AfterReset_ReportsNoNextEvent()
    {
        var modelMock = new Mock<IModel>();
        modelMock.Setup(m => m.IsInterruptRequested).Returns(false);
        var scheduler = new EventScheduler(modelMock.Object);

        scheduler.Add(7.0, CreateEventMock(7.0).Object);
        scheduler.TimeOfNextScheduledEvent.Should().Be(7.0);

        scheduler.Reset();

        double.IsPositiveInfinity(scheduler.TimeOfNextScheduledEvent).Should().BeTrue(
            "Reset cleared the event list but left the cached next-event time behind, so a freshly " +
            "reset model still advertised the previous run's schedule");

        var act = () => { _ = scheduler.NextScheduledEvent; };
        act.Should().NotThrow();
        scheduler.NextScheduledEvent.Should().BeNull();
    }
}
