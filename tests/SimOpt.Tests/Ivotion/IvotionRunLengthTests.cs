using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SimOpt.Ivotion;
using Xunit;

namespace SimOpt.Tests.Ivotion;

/// <summary>
/// SIM-93 defaults, MG decision Q-IVO-STATS (2026-11-25): the Ivotion optimiser scores each configuration
/// over 960 minutes (two shifts) after a 120-minute warm-up. The baseline (one Roland, batches of 15) ships
/// a batch about every 83 minutes, so a score counts whole batches in its window; over 120 minutes from an
/// empty line it scored 7.5 pieces/h, over the new window 10.71 at every one of 20 seeds measured.
/// </summary>
public class IvotionRunLengthTests
{
    private static readonly int[] Baseline = { 1, 1, 1, 1, 1, 15 };

    [Fact]
    public void TheDefaults_AreTwoShiftsAfterATwoHourWarmup()
    {
        var settings = new IvotionOptimizationSettings();
        var problem = new IvotionProblem();

        settings.SimDurationMinutes.Should().Be(960);
        settings.WarmupMinutes.Should().Be(120);
        problem.SimDurationMinutes.Should().Be(960);
        problem.WarmupMinutes.Should().Be(120);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(7)]
    public void TheBaseline_ScoresItsSteadyThroughput_AtEverySeed(int seed)
    {
        var problem = new IvotionProblem { Objective = IvotionObjective.MaximizeThroughput, Seed = seed };
        var baseline = new IvotionSolution(Baseline);

        problem.Evaluate(baseline);

        baseline.Fitness.Should().BeApproximately(10.71, 0.005);
    }

    [Fact]
    public async Task TheEngine_ScoresWithTheWarmupItWasGiven()
    {
        // one random point, then the same point scored by a problem told the same run length and warm-up
        var settings = new IvotionOptimizationSettings
        {
            Strategy = IvotionStrategyKind.Random, Iterations = 1, Seed = 5,
            SimDurationMinutes = 600, WarmupMinutes = 200,
        };

        IvotionOptimizationResult result = await new IvotionOptimizationEngine().RunAsync(settings, null, CancellationToken.None);

        var same = new IvotionSolution(result.BestSolution!.Parameters);
        new IvotionProblem { Seed = 5, SimDurationMinutes = 600, WarmupMinutes = 200 }.Evaluate(same);
        result.BestSolution.Fitness.Should().Be(same.Fitness);
        result.BestKpis!.Value.SimulatedMinutes.Should().Be(600);
    }

    [Fact]
    public async Task AWarmupNotShorterThanTheRun_IsRefused()
    {
        var settings = new IvotionOptimizationSettings { Strategy = IvotionStrategyKind.Random, Iterations = 1, SimDurationMinutes = 90 };

        Func<Task> act = () => new IvotionOptimizationEngine().RunAsync(settings, null, CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage("*warm-up*");
    }
}
