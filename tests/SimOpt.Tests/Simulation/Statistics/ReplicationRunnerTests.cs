using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Statistics;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Statistics;

/// <summary>
/// SIM-63 — replication with confidence intervals (UN-012), and the end-to-end instrumentation
/// chain that feeds it.
///
/// <para>
/// A single simulation run is one draw from a random process, and reporting it as though it were
/// the answer is how a confident wrong recommendation gets made. These tests pin both halves: that
/// the interval arithmetic is right, and that the collectors it aggregates measure what they claim.
/// </para>
/// </summary>
public class ReplicationRunnerTests
{
    // ── the interval arithmetic, pinned without simulating anything ──────────

    [Fact]
    public void TheConfidenceHalfWidth_MatchesTheHandComputedValue()
    {
        // Two observations {10, 12}: mean 11, Σ(x−x̄)² = 2, s² = 2/1 = 2, s = √2,
        // SE = √2/√2 = 1, and t(0.975, ν=1) = 12.7062047362, so the half-width IS the t value.
        double? h = ReplicationRunner.ConfidenceHalfWidth(new[] { 10d, 12d });

        h.Should().NotBeNull();
        h!.Value.Should().BeApproximately(12.7062047362, 1e-9);
    }

    [Fact]
    public void TheConfidenceHalfWidth_UsesTheSampleStandardDeviation_NotThePopulationOne()
    {
        // {2,4,4,4,5,5,7,9}: mean 5, Σ(x−x̄)² = 32. Sample variance 32/7, population variance 4.
        // With n=8, t(0.975,7) = 2.3646242516, so h = 2.3646242516·√(32/7)/√8 = 1.7862...
        // The population form would give 1.6720..., a 6% understatement of the uncertainty.
        double? h = ReplicationRunner.ConfidenceHalfWidth(new[] { 2d, 4d, 4d, 4d, 5d, 5d, 7d, 9d });

        double expected = 2.3646242516 * Math.Sqrt(32d / 7d) / Math.Sqrt(8d);
        h!.Value.Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void ASingleValue_HasNoInterval()
    {
        // Not zero. A half-width of zero reads as infinite precision and is the most dangerous
        // number this subsystem could emit.
        ReplicationRunner.ConfidenceHalfWidth(new[] { 42d }).Should().BeNull();
        ReplicationRunner.ConfidenceHalfWidth(Array.Empty<double>()).Should().BeNull();
    }

    [Fact]
    public void IdenticalValues_GiveAZeroWidthInterval()
    {
        // A deterministic model genuinely has no sampling uncertainty, so zero is correct HERE —
        // which is exactly why the single-replication case above must be null rather than zero.
        ReplicationRunner.ConfidenceHalfWidth(new[] { 3d, 3d, 3d, 3d })!.Value.Should().Be(0d);
    }

    // ── the instrumentation chain, on a model whose answer is exact ──────────

    private sealed class Fixture
    {
        public Model Model { get; init; } = null!;
        public SimpleBuffer Buffer { get; init; } = null!;
        public SimpleServer Server { get; init; } = null!;
        public SimpleSink Sink { get; init; } = null!;
    }

    /// <summary>
    /// A deterministic D/D/1: arrivals every <paramref name="interval"/>, service taking
    /// <paramref name="service"/>. With service &lt; interval nothing ever queues, so every
    /// statistic has a closed form computable on paper.
    /// </summary>
    private static Fixture BuildDeterministic(double interval, double service)
    {
        var model = new Model("dd1", 1, 0d);

        var arrivals = new ConstantDoubleDistribution(interval, initialize: false);
        var serviceTime = new ConstantDoubleDistribution(service, initialize: false);

        int n = 0;
        var source = new SimpleSource(model, arrivals,
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 1000);
        var server = new SimpleServer(model, serviceTime, id: "srv", name: "srv") { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        return new Fixture { Model = model, Buffer = buffer, Server = server, Sink = sink };
    }

    [Fact]
    public void Utilization_IsExact_WhereAPolledMeasurementCouldOnlyApproximate()
    {
        var f = BuildDeterministic(interval: 2.0, service: 0.5);
        var utilization = Instrumentation.ObserveUtilization(f.Server);

        f.Model.Reset(1);
        f.Model.Run(20.0);

        // Arrivals at 0,2,4,…,20; each occupies the server for 0.5 of every 2.0. The busy fraction
        // is therefore exactly 0.25 — and it is *exactly* that, not approximately, because the
        // measurement is driven by transitions rather than by sampling. The polled implementation
        // this replaces could not guarantee it: with a 0.1 render tick it happens to land close,
        // but its answer moves with the frame rate and any service shorter than a tick vanishes.
        utilization.Estimate(f.Model.CurrentTime).Should().BeApproximately(0.25, 1e-12);
    }

    [Fact]
    public void QueueLengthAndWaitingTime_AreZero_WhenNothingEverQueues()
    {
        var f = BuildDeterministic(interval: 2.0, service: 0.5);
        var queue = Instrumentation.ObserveQueueLength(f.Buffer);
        var wait = Instrumentation.ObserveWaitingTime(f.Buffer);

        f.Model.Reset(1);
        f.Model.Run(20.0);

        // Service is shorter than the gap between arrivals, so each item is taken immediately.
        queue.Estimate(f.Model.CurrentTime).Should().BeApproximately(0d, 1e-12);
        wait.Count.Should().BeGreaterThan(0, "items must actually have passed through the buffer");
        wait.Estimate(f.Model.CurrentTime).Should().BeApproximately(0d, 1e-12);
    }

    [Fact]
    public void WaitingTimeGrows_WhenServiceIsSlowerThanArrivals()
    {
        var f = BuildDeterministic(interval: 1.0, service: 2.0);
        var wait = Instrumentation.ObserveWaitingTime(f.Buffer);
        var queue = Instrumentation.ObserveQueueLength(f.Buffer);

        f.Model.Reset(1);
        f.Model.Run(20.0);

        // An overloaded station: arrivals outpace service, so the queue grows without bound and
        // waits grow with it. The point of this test is the direction and the ordering — a
        // collector that reported zero here, or that reported the same figure as the stable case
        // above, would be measuring nothing at all.
        wait.Estimate(f.Model.CurrentTime).Should().BeGreaterThan(0d);
        queue.Estimate(f.Model.CurrentTime).Should().BeGreaterThan(0d);
        wait.Max.Should().BeGreaterThan(wait.Min);
    }

    // ── the runner ───────────────────────────────────────────────────────────

    private static (Model Model, SimpleServer Server) BuildStochastic(int seed)
    {
        var model = new Model("mm1", seed, 0d);

        var arrivals = new NegExponentialDistribution();
        arrivals.ConfigureMean(2.0);
        var service = new NegExponentialDistribution();
        service.ConfigureMean(1.5);

        int n = 0;
        var source = new SimpleSource(model, StableSeed("src"), arrivals,
            () => new SimpleEntity(model, $"E{++n}", $"E{n}"),
            autoStartDelay: 0d, id: "src", name: "src");
        var buffer = new SimpleBuffer(model, QueueRule.FIFO, id: "buf", name: "buf", maxCapacity: 10000);
        var server = new SimpleServer(model, StableSeed("srv"), service, id: "srv", name: "srv")
        { AutoContinue = true };
        var sink = new SimpleSink(model, id: "snk", name: "snk");

        buffer.ConnectTo(source);
        server.ConnectTo(buffer);
        buffer.ItemReceivedEvent.AddHandler((_, _) => { if (server.Idle) server.Start(); });
        sink.ConnectTo(server);

        return (model, server);
    }

    private static int StableSeed(string key) => SimOpt.Basics.Utilities.StableHash.Of(key);

    [Fact]
    public void TheSameBaseSeed_ProducesBitwiseIdenticalReplications()
    {
        static double[] RunOnce()
        {
            var (model, server) = BuildStochastic(7);
            Instrumentation.ObserveUtilization(server);
            var runner = new ReplicationRunner(model, runLength: 500d, warmupTime: 50d);
            return runner.Run(replications: 5, baseSeed: 12345).Metrics.Single().ReplicateEstimates;
        }

        // Reproducibility is the property the whole product's credibility rests on (UN-009,
        // UN-033). Bitwise equality, not approximate: a figure that only nearly reproduces is not
        // reproducible, and an opposing expert will run it on different hardware.
        RunOnce().Should().Equal(RunOnce());
    }

    [Fact]
    public void DifferentReplications_ExploreDifferentSamplePaths()
    {
        var (model, server) = BuildStochastic(7);
        Instrumentation.ObserveUtilization(server);
        var runner = new ReplicationRunner(model, runLength: 500d, warmupTime: 50d);

        ReplicationResult result = runner.Run(replications: 5, baseSeed: 12345);
        double[] estimates = result.Metrics.Single().ReplicateEstimates;

        // The opposite failure to the one above: replications that all returned the same number
        // would make reproducibility vacuously true and every confidence interval zero-width.
        estimates.Distinct().Should().HaveCountGreaterThan(1);
        result.ReplicationSeeds.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void ASingleReplication_ReportsAMeanButRefusesAnInterval()
    {
        var (model, server) = BuildStochastic(7);
        Instrumentation.ObserveUtilization(server);
        var runner = new ReplicationRunner(model, runLength: 500d, warmupTime: 50d);

        MetricSummary metric = runner.Run(replications: 1, baseSeed: 1).Metrics.Single();

        metric.Replications.Should().Be(1);
        metric.Mean.Should().BeGreaterThan(0d);
        metric.HalfWidth.Should().BeNull("one replication says nothing about its own variability");
        metric.Lower.Should().Be(double.NaN);
        metric.Upper.Should().Be(double.NaN);
    }

    [Fact]
    public void TheResultCarriesEnoughProvenanceToReproduceIt()
    {
        var (model, server) = BuildStochastic(7);
        Instrumentation.ObserveUtilization(server);
        var runner = new ReplicationRunner(model, runLength: 500d, warmupTime: 50d);

        ReplicationResult result = runner.Run(replications: 4, baseSeed: 99, confidenceLevel: 0.95);

        // UN-034: a figure separated from what produced it is not a deliverable. Everything needed
        // to re-run this experiment must travel with its answer.
        result.Replications.Should().Be(4);
        result.BaseSeed.Should().Be(99);
        result.RunLength.Should().Be(500d);
        result.WarmupTime.Should().Be(50d);
        result.ConfidenceLevel.Should().Be(0.95);
        result.ReplicationSeeds.Should().HaveCount(4);
        result.ReplicationSeeds.Should().Equal(
            Enumerable.Range(0, 4).Select(r => ReplicationRunner.SeedFor(99, r)));
    }

    [Fact]
    public void AWarmupAtLeastAsLongAsTheRun_IsRejectedRatherThanReportedAsEmpty()
    {
        var (model, _) = BuildStochastic(7);

        Action act = () => new ReplicationRunner(model, runLength: 100d, warmupTime: 100d);

        act.Should().Throw<ArgumentOutOfRangeException>(
            "every statistic would be empty by construction, which is a configuration error rather " +
            "than a result");
    }
}
