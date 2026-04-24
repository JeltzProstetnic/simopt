using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Ivotion;

/// <summary>
/// Default <see cref="IIvotionOptimizationEngine"/>. Supports Random and
/// Evolutionary strategies; ParticleSwarm and Sweep throw
/// <see cref="NotSupportedException"/> until Phase A.2 / later lands.
/// </summary>
public sealed class IvotionOptimizationEngine : IIvotionOptimizationEngine
{
    public Task<IvotionOptimizationResult> RunAsync(
        IvotionOptimizationSettings settings,
        IProgress<IvotionFitnessSample>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "Iterations must be > 0.");
        if (settings.Strategy is IvotionStrategyKind.ParticleSwarm or IvotionStrategyKind.Sweep)
            throw new NotSupportedException(
                $"Strategy {settings.Strategy} is not yet wired (Phase A.2 / later).");

        return Task.Run(() => Run(settings, progress, ct), ct);
    }

    private static IvotionOptimizationResult Run(
        IvotionOptimizationSettings settings,
        IProgress<IvotionFitnessSample>? progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var problem = new IvotionProblem
        {
            Objective = settings.Objective,
            SimDurationMinutes = settings.SimDurationMinutes,
            Seed = settings.Seed,
            OperatorWagePerHour = settings.OperatorWagePerHour,
        };

        return settings.Strategy switch
        {
            IvotionStrategyKind.Random => RunRandom(problem, settings, progress, ct, sw),
            IvotionStrategyKind.Evolutionary => RunEvolutionary(problem, settings, progress, ct, sw),
            _ => throw new NotSupportedException($"Strategy {settings.Strategy} not wired."),
        };
    }

    private static IvotionOptimizationResult RunRandom(
        IvotionProblem problem,
        IvotionOptimizationSettings settings,
        IProgress<IvotionFitnessSample>? progress,
        CancellationToken ct,
        Stopwatch sw)
    {
        var rnd = new Random(settings.Seed);
        IvotionSolution? best = null;
        int done = 0;

        for (int i = 0; i < settings.Iterations; i++)
        {
            if (ct.IsCancellationRequested) break;

            var candidate = (IvotionSolution)problem.GenerateCandidates(rnd.Next(), 1).First();
            problem.Evaluate(candidate);
            done++;

            if (best is null || candidate.Fitness > best.Fitness)
                best = (IvotionSolution)candidate.Clone();

            progress?.Report(new IvotionFitnessSample(done, best.Fitness, (IvotionSolution)best.Clone()));
        }

        return BuildResult(problem, settings, best, done, sw, ct.IsCancellationRequested);
    }

    private static IvotionOptimizationResult RunEvolutionary(
        IvotionProblem problem,
        IvotionOptimizationSettings settings,
        IProgress<IvotionFitnessSample>? progress,
        CancellationToken ct,
        Stopwatch sw)
    {
        int lambda = Math.Max(2, settings.PopulationSize);
        int mu = Math.Max(1, lambda / 2);

        var config = new EvolutionaryAlgorithmConfiguration(
            seed: settings.Seed,
            iterations: settings.Iterations,
            mu: mu,
            lambda: lambda,
            mutation: null,
            crossover: null,
            keepParentsAlive: false);

        var strategy = new EvolutionaryAlgorithm();
        strategy.Initialize(config);

        IvotionSolution? bestSoFar = null;
        int gen = 0;

        strategy.GenerationFinished += (_, e) =>
        {
            gen++;
            var bestInGen = e.NewGeneration
                .OfType<IvotionSolution>()
                .Where(s => s.HasFitness)
                .OrderByDescending(s => s.Fitness)
                .FirstOrDefault();
            if (bestInGen is null) return;

            if (bestSoFar is null || bestInGen.Fitness > bestSoFar.Fitness)
                bestSoFar = (IvotionSolution)bestInGen.Clone();

            progress?.Report(new IvotionFitnessSample(gen, bestSoFar.Fitness, (IvotionSolution)bestSoFar.Clone()));
        };

        using var reg = ct.Register(() => strategy.Stop());
        _ = strategy.Solve(problem).ToList();

        return BuildResult(problem, settings, bestSoFar, gen, sw, ct.IsCancellationRequested);
    }

    private static IvotionOptimizationResult BuildResult(
        IvotionProblem problem,
        IvotionOptimizationSettings settings,
        IvotionSolution? best,
        int iterations,
        Stopwatch sw,
        bool cancelled)
    {
        sw.Stop();

        IvotionKpis? kpis = null;
        if (best is not null)
        {
            var handles = IvotionTopologyBuilder.Build(best, settings.Seed, problem.ArrivalIntervalMinutes);
            handles.RunFor(settings.SimDurationMinutes);
            kpis = IvotionKpis.Extract(handles, settings.SimDurationMinutes, settings.OperatorWagePerHour);
        }

        return new IvotionOptimizationResult(
            BestSolution: best,
            BestKpis: kpis,
            TotalIterations: iterations,
            ElapsedMilliseconds: sw.ElapsedMilliseconds,
            WasCancelled: cancelled);
    }
}
