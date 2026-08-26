using System;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-63 — observation-based (tally) statistics: waiting time, cycle time, time in a delay.
///
/// <para>
/// Every expected value here is computable by hand, deliberately. A statistics subsystem validated
/// only against its own sampled output proves nothing; these are the arithmetic assertions that
/// make the sampled analytic battery (SIM-64) meaningful rather than circular.
/// </para>
/// </summary>
public class TallyCollectorTests
{
    private static Model NewModel() => new("stats", 1, 0d);

    private static TallyCollector NewTally(Model model, double warmup = 0d) =>
        new(model, "wait_time", id: "t", name: "t") { WarmupTime = warmup };

    [Fact]
    public void AnEmptyTally_ReportsNoData_NotZero()
    {
        var tally = NewTally(NewModel());

        // The distinction that matters: a station nothing passed through has no mean waiting time.
        // Reporting 0.0 would read as "nobody waited", which is a different and much more
        // flattering claim than "there is no evidence either way".
        tally.Count.Should().Be(0);
        tally.HasData.Should().BeFalse();
        tally.Mean.Should().Be(double.NaN);
        tally.Min.Should().Be(double.NaN);
        tally.Max.Should().Be(double.NaN);
        tally.Variance.Should().Be(double.NaN);
    }

    [Fact]
    public void ASingleObservation_HasAMeanButNoVariance()
    {
        var tally = NewTally(NewModel());

        tally.Observe(3.0);

        tally.Count.Should().Be(1);
        tally.HasData.Should().BeTrue();
        tally.Mean.Should().Be(3.0);
        tally.Min.Should().Be(3.0);
        tally.Max.Should().Be(3.0);
        tally.Variance.Should().Be(double.NaN, "a sample variance needs at least two observations");
        tally.StdDev.Should().Be(double.NaN);
    }

    [Fact]
    public void MeanMinMaxAndSampleVariance_MatchHandComputedValues()
    {
        var tally = NewTally(NewModel());
        foreach (double x in new[] { 2d, 4d, 4d, 4d, 5d, 5d, 7d, 9d })
            tally.Observe(x);

        // Sum 40 over 8 observations ⇒ mean 5. Squared deviations: 9+1+1+1+0+0+4+16 = 32.
        // Sample variance uses n−1 ⇒ 32/7. (The population form would give 4 — the two differ
        // visibly here on purpose, so an accidental switch to n cannot pass.)
        tally.Count.Should().Be(8);
        tally.Mean.Should().Be(5.0);
        tally.Min.Should().Be(2.0);
        tally.Max.Should().Be(9.0);
        tally.Variance.Should().BeApproximately(32d / 7d, 1e-12);
        tally.StdDev.Should().BeApproximately(Math.Sqrt(32d / 7d), 1e-12);
    }

    [Fact]
    public void VarianceOfLargeNearlyEqualValues_SurvivesCancellation()
    {
        var tally = NewTally(NewModel());
        foreach (double x in new[] { 1e9 + 1, 1e9 + 2, 1e9 + 3 })
            tally.Observe(x);

        // The textbook shortcut, variance = (Σx² − n·x̄²)/(n−1), loses every significant digit at
        // this magnitude and returns garbage — often a negative number. Welford's incremental form
        // returns exactly 1. Simulated waiting times sit on a large mean with small spread, so this
        // is the realistic case rather than a contrived one.
        tally.Variance.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void ObservationsBeforeTheWarmupBoundary_AreExcluded()
    {
        var model = NewModel();
        var tally = NewTally(model, warmup: 10d);

        tally.Observe(100d, time: 9.999);
        tally.Observe(2d, time: 10.0);
        tally.Observe(4d, time: 11.0);

        // The boundary is inclusive: an observation completing exactly at W counts. Whichever
        // convention is chosen it must be stated and pinned, because a silent off-by-one at the
        // boundary is invisible in aggregate and shifts every reported mean.
        tally.Count.Should().Be(2);
        tally.Mean.Should().Be(3.0);
        tally.Max.Should().Be(4.0, "the pre-warm-up value of 100 must not survive in the extremes");
    }

    [Fact]
    public void ARunThatEndsBeforeWarmup_ReportsNoData_RatherThanZero()
    {
        var tally = NewTally(NewModel(), warmup: 10d);

        tally.Observe(5d, time: 1.0);
        tally.Observe(6d, time: 2.0);

        // Asking for a steady-state measure from a run shorter than its own warm-up period is a
        // user error, and the honest answer is "no data". Returning 0 would be a confident lie.
        tally.HasData.Should().BeFalse();
        tally.Count.Should().Be(0);
        tally.Mean.Should().Be(double.NaN);
    }

    [Fact]
    public void Reset_ClearsObservationsButKeepsTheWarmupSetting()
    {
        var tally = NewTally(NewModel(), warmup: 4d);
        tally.Observe(7d, time: 5.0);

        tally.Reset();

        tally.Count.Should().Be(0);
        tally.HasData.Should().BeFalse();
        tally.WarmupTime.Should().Be(4d, "warm-up is an experiment setting, not accumulated state");
    }

    [Fact]
    public void Reset_IsRunToRunRepeatable()
    {
        var tally = NewTally(NewModel());

        tally.Observe(1d);
        tally.Observe(3d);
        double firstMean = tally.Mean;

        tally.Reset();
        tally.Observe(1d);
        tally.Observe(3d);

        // The replication runner resets between replications, so a collector that carried anything
        // across a reset would blend replications together and shrink every confidence interval.
        tally.Mean.Should().Be(firstMean);
        tally.Count.Should().Be(2);
    }
}
