using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SimOpt.Ivotion;
using Xunit;

namespace SimOpt.Tests.Ivotion;

public class IvotionOptimizationEngineTests
{
    [Fact]
    public async Task RunAsync_Random_ReturnsBestSolutionWithKpis()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Objective = IvotionObjective.MaximizeThroughput,
            Iterations = 4,
            SimDurationMinutes = 30.0,
            Seed = 7,
        };

        var result = await engine.RunAsync(settings, progress: null, CancellationToken.None);

        result.BestSolution.Should().NotBeNull();
        result.BestKpis.Should().NotBeNull();
        result.TotalIterations.Should().Be(4);
        result.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_Random_ReportsMonotoneBestFitness()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Iterations = 5,
            SimDurationMinutes = 30.0,
            Seed = 13,
        };
        var samples = new List<IvotionFitnessSample>();
        var progress = new Progress<IvotionFitnessSample>(samples.Add);

        var result = await engine.RunAsync(settings, progress, CancellationToken.None);

        // Progress events are marshalled async by Progress<T>. Wait briefly for them.
        await Task.Delay(50);

        samples.Should().NotBeEmpty();
        for (int i = 1; i < samples.Count; i++)
            samples[i].BestSoFarFitness.Should().BeGreaterThanOrEqualTo(samples[i - 1].BestSoFarFitness);
        result.BestSolution.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_Evolutionary_ReturnsBestSolution()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Evolutionary,
            Objective = IvotionObjective.MaximizeThroughput,
            Iterations = 2,
            PopulationSize = 6,
            SimDurationMinutes = 30.0,
            Seed = 21,
        };

        var result = await engine.RunAsync(settings, progress: null, CancellationToken.None);

        result.BestSolution.Should().NotBeNull();
        result.BestKpis.Should().NotBeNull();
        result.BestKpis!.Value.ThroughputPerHour.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RunAsync_Cancellation_StopsEarly()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Iterations = 10_000,
            SimDurationMinutes = 30.0,
            Seed = 1,
        };
        var cts = new CancellationTokenSource(millisecondsDelay: 100);

        var result = await engine.RunAsync(settings, progress: null, cts.Token);

        result.TotalIterations.Should().BeLessThan(10_000);
        result.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ParticleSwarm_Throws()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.ParticleSwarm,
            Iterations = 3,
        };

        var act = () => engine.RunAsync(settings, progress: null, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task RunAsync_Sweep_Throws()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Sweep,
            Iterations = 3,
        };

        var act = () => engine.RunAsync(settings, progress: null, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task RunAsync_ZeroIterations_Throws()
    {
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Iterations = 0,
        };

        var act = () => engine.RunAsync(settings, progress: null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task RunAsync_WageOverride_ChangesReportedCost()
    {
        // Use MaximizeThroughput so wage does NOT affect fitness (same seed
        // ⇒ same winning solution). Only the KPI cost changes with wage.
        var engine = new IvotionOptimizationEngine();
        var baseSettings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Objective = IvotionObjective.MaximizeThroughput,
            Iterations = 3,
            SimDurationMinutes = 30.0,
            Seed = 5,
        };

        var lowResult = await engine.RunAsync(
            baseSettings with { OperatorWagePerHour = 10.0 }, progress: null, CancellationToken.None);
        var highResult = await engine.RunAsync(
            baseSettings with { OperatorWagePerHour = 80.0 }, progress: null, CancellationToken.None);

        lowResult.BestSolution.Should().NotBeNull();
        highResult.BestSolution.Should().NotBeNull();
        lowResult.BestSolution!.Parameters.Should().Equal(highResult.BestSolution!.Parameters,
            "same seed + throughput objective must pick the same winner");
        highResult.BestKpis!.Value.TotalCostPerHour
            .Should().BeGreaterThan(lowResult.BestKpis!.Value.TotalCostPerHour,
                "wage change must propagate into KPI cost computation");
    }
}
