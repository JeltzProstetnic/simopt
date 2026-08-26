using System;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-63 — time-persistent statistics: queue length, utilisation, work in progress.
///
/// <para>
/// These integrate a step function over simulated time; they are emphatically <b>not</b> sample
/// means of observed values. The distinction is the whole point of the subsystem: utilisation was
/// previously computed by sampling a busy flag once per render tick
/// (<c>SimulationCanvas.cs:297-332</c>), which misses every busy interval shorter than the gap
/// between ticks and cannot be reached from a headless or MCP run at all.
/// </para>
/// <para>
/// Every expected value below is hand-computed arithmetic over a step function.
/// </para>
/// </summary>
public class TimeWeightedCollectorTests
{
    private static Model NewModel() => new("stats", 1, 0d);

    private static TimeWeightedCollector NewCollector(
        Model model, Func<double> probe = null, double warmup = 0d) =>
        new(model, "queue_length", probe ?? (() => 0d), id: "q", name: "q") { WarmupTime = warmup };

    [Fact]
    public void AConstantValueHeldForTheWholeRun_AveragesToThatValue()
    {
        var collector = NewCollector(NewModel());
        collector.Record(1d, time: 0d);

        // Nothing changes after t=0, so the entire interval [0,10] is one open step. If the final
        // interval were dropped the average would be NaN or 0; if it were double-counted it would
        // be 2. Both are classic implementations of this collector.
        collector.TimeIntegral(10d).Should().Be(10d);
        collector.TimeAverage(10d).Should().Be(1d);
    }

    [Fact]
    public void ReadingTheCollectorTwice_ReturnsTheSameAnswer()
    {
        var collector = NewCollector(NewModel());
        collector.Record(3d, time: 7d);

        double first = collector.TimeIntegral(10d);
        double second = collector.TimeIntegral(10d);

        // Reads must close the open interval virtually rather than by mutating state. An
        // implementation that "finalises" on read gives a different answer the second time and
        // corrupts the run if the model is continued afterwards.
        second.Should().Be(first);
        first.Should().Be(9d, "the value 3 was in force over [7,10]");
    }

    [Fact]
    public void AStepFunction_IntegratesToItsHandComputedArea()
    {
        var collector = NewCollector(NewModel());

        collector.Record(0d, time: 0d);
        collector.Record(2d, time: 1d);
        collector.Record(5d, time: 3d);
        collector.Record(0d, time: 7d);

        // 0 over [0,1) = 0, 2 over [1,3) = 4, 5 over [3,7) = 20, 0 over [7,10] = 0 ⇒ total 24.
        collector.TimeIntegral(10d).Should().Be(24d);
        collector.TimeAverage(10d).Should().Be(2.4d);
        collector.Max(10d).Should().Be(5d);
        collector.Min(10d).Should().Be(0d);
    }

    [Fact]
    public void AWarmupBoundaryFallingMidInterval_CountsOnlyThePartAfterIt()
    {
        var collector = NewCollector(NewModel(), warmup: 4d);
        collector.Record(5d, time: 0d);

        // The value 5 is in force from t=0 to the end of the run, and the warm-up boundary cuts
        // that single interval in half. Only [4,10] counts ⇒ 5 × 6 = 30, average 5.
        //
        // This is the case implementations get wrong. The common alternative — scheduling a
        // "clear statistics" event at W — zeroes the current value and loses the step that was in
        // force across the boundary, so Max silently becomes 0 here instead of 5. Clamping the
        // interval arithmetically has no such state to lose.
        collector.TimeIntegral(10d).Should().Be(30d);
        collector.TimeAverage(10d).Should().Be(5d);
        collector.Max(10d).Should().Be(5d, "the value held across the boundary was in force at W");
    }

    [Fact]
    public void ARunEndingBeforeWarmup_ReportsNoData_RatherThanZero()
    {
        var collector = NewCollector(NewModel(), warmup: 10d);
        collector.Record(3d, time: 1d);

        collector.HasData.Should().BeFalse();
        collector.TimeAverage(5d).Should().Be(double.NaN, "a zero here would be a confident lie");
    }

    [Fact]
    public void IncrementAndDecrement_TrackTheRunningValue()
    {
        var collector = NewCollector(NewModel());

        collector.Record(0d, time: 0d);
        collector.Increment(1, time: 2d);
        collector.Increment(1, time: 4d);
        collector.Decrement(1, time: 6d);

        // 0 over [0,2) = 0, 1 over [2,4) = 2, 2 over [4,6) = 4, 1 over [6,10] = 4 ⇒ 10.
        collector.CurrentValue.Should().Be(1d);
        collector.TimeIntegral(10d).Should().Be(10d);
        collector.Max(10d).Should().Be(2d);
    }

    [Fact]
    public void Reset_TakesItsStartingValueFromTheProbe_NotFromZero()
    {
        int queueDepth = 3;
        var collector = NewCollector(NewModel(), probe: () => queueDepth);

        collector.Record(9d, time: 5d);
        collector.Reset();

        // A source that auto-starts creates its first entity *during* Model.Reset, so a collector
        // may be reset either before or after the thing it observes. Re-reading the live value on
        // reset makes the collector correct either way, instead of depending on the order entities
        // happen to sit in the model's item collection.
        collector.CurrentValue.Should().Be(3d);
        collector.TimeIntegral(0d).Should().Be(0d);
        collector.HasData.Should().BeFalse();
    }

    [Fact]
    public void Reset_IsRunToRunRepeatable()
    {
        var collector = NewCollector(NewModel());
        collector.Record(2d, time: 0d);
        double first = collector.TimeIntegral(10d);

        collector.Reset();
        collector.Record(2d, time: 0d);

        collector.TimeIntegral(10d).Should().Be(first);
    }
}
