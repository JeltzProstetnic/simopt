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
    public async Task RunAsync_Evolutionary_TheSameSeedGivesTheSameSearch()
    {
        // SIM-59: the mutation and crossover drew from wall-clock generators
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Evolutionary,
            Objective = IvotionObjective.MaximizeThroughput,
            Iterations = 3,
            PopulationSize = 6,
            SimDurationMinutes = 30.0,
            Seed = 21,
        };
        async Task<List<string>> Search()
        {
            var seen = new List<string>();
            var progress = new SynchronousProgress(s => seen.Add($"{s.BestSoFarFitness}:{string.Join(",", s.BestSoFarSolution.Parameters)}"));
            var result = await new IvotionOptimizationEngine().RunAsync(settings, progress, CancellationToken.None);
            seen.Add(string.Join(",", result.BestSolution!.Parameters));
            return seen;
        }

        (await Search()).Should().Equal(await Search());
    }

    private sealed class SynchronousProgress : IProgress<IvotionFitnessSample>
    {
        private readonly Action<IvotionFitnessSample> report;
        public SynchronousProgress(Action<IvotionFitnessSample> report) => this.report = report;
        public void Report(IvotionFitnessSample value) => report(value);
    }

    [Fact]
    public async Task RunAsync_Cancellation_StopsEarly()
    {
        // SIM-121: cancelled from the progress callback after five iterations, not after 100 ms of wall
        // clock, which failed under a loaded parallel suite
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Iterations = 10_000,
            SimDurationMinutes = 30.0,
            Seed = 1,
        };
        using var cts = new CancellationTokenSource();
        var progress = new SynchronousProgress(s => { if (s.Iteration >= 5) cts.Cancel(); });

        var result = await engine.RunAsync(settings, progress, cts.Token);

        result.TotalIterations.Should().Be(5);
        result.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WithATokenAlreadyCancelled_ReturnsACancelledResult_InsteadOfThrowing()
    {
        // SIM-121: Task.Run(..., ct) returned a cancelled task when the token was cancelled before the
        // thread pool started the work, so the same cancellation sometimes threw and sometimes returned
        var engine = new IvotionOptimizationEngine();
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random,
            Iterations = 100,
            SimDurationMinutes = 30.0,
            Seed = 1,
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await engine.RunAsync(settings, progress: null, cts.Token);

        result.WasCancelled.Should().BeTrue();
        result.TotalIterations.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_ParticleSwarm_SearchesTheLine_Reproducibly()
    {
        // SIM-36: the swarm moves through the real-valued view of IvotionSolution; Iterations are swarm
        // steps and PopulationSize the swarm, as they are generations and offspring for the EA
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.ParticleSwarm,
            Iterations = 5,
            PopulationSize = 6,
            SimDurationMinutes = 30.0,
            Seed = 3,
        };

        var first = await new IvotionOptimizationEngine().RunAsync(settings, progress: null, CancellationToken.None);
        var second = await new IvotionOptimizationEngine().RunAsync(settings, progress: null, CancellationToken.None);

        first.BestSolution.Should().NotBeNull();
        IvotionSolution.IsInRange(first.BestSolution!.Parameters).Should().BeTrue();
        first.TotalIterations.Should().Be(5);
        first.WasCancelled.Should().BeFalse();
        second.BestSolution!.Parameters.Should().Equal(first.BestSolution.Parameters);
        second.BestSolution.Fitness.Should().Be(first.BestSolution.Fitness);
    }

    [Fact]
    public async Task RunAsync_ParticleSwarm_ReportsEachStep_AndStopsWhenCancelled()
    {
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.ParticleSwarm,
            Iterations = 50,
            PopulationSize = 4,
            SimDurationMinutes = 30.0,
            Seed = 1,
        };
        using var cts = new CancellationTokenSource();
        var steps = new List<int>();
        var progress = new SynchronousProgress(s => { steps.Add(s.Iteration); if (s.Iteration >= 2) cts.Cancel(); });

        var result = await new IvotionOptimizationEngine().RunAsync(settings, progress, cts.Token);

        steps.Should().Equal(1, 2);
        result.TotalIterations.Should().Be(2);
        result.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public void ParticleSwarm_IsOffered_UnderItsOwnName()
    {
        IvotionStrategyInfo.IsEnabled(IvotionStrategyKind.ParticleSwarm).Should().BeTrue();
        IvotionStrategyInfo.DisplayName(IvotionStrategyKind.ParticleSwarm).Should().Be("Particle Swarm");
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
