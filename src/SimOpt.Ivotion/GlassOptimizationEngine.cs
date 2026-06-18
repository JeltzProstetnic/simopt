using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Glass;

/// <summary>
/// Default <see cref="IGlassOptimizationEngine"/>. Supports Random and
/// Evolutionary strategies; ParticleSwarm and Sweep throw
/// <see cref="NotSupportedException"/> until later phases land.
/// </summary>
public sealed class GlassOptimizationEngine : IGlassOptimizationEngine
{
    public Task<GlassOptimizationResult> RunAsync(
        GlassOptimizationSettings settings,
        IProgress<GlassFitnessSample>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings),
                "Iterations must be > 0.");
        if (settings.Strategy is GlassStrategyKind.ParticleSwarm or GlassStrategyKind.Sweep)
            throw new NotSupportedException(
                $"Strategy {settings.Strategy} is not yet wired.");

        return Task.Run(() => Run(settings, progress, ct), ct);
    }

    private static GlassOptimizationResult Run(
        GlassOptimizationSettings settings,
        IProgress<GlassFitnessSample>? progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var problem = new GlassProblem
        {
            Objective = settings.Objective,
            SimDurationMinutes = settings.SimDurationMinutes,
            Seed = settings.Seed,
            OperatorWagePerHour = settings.OperatorWagePerHour,
        };

        return settings.Strategy switch
        {
            GlassStrategyKind.Random => RunRandom(problem, settings, progress, ct, sw),
            GlassStrategyKind.Evolutionary => RunEvolutionary(problem, settings, progress, ct, sw),
            _ => throw new NotSupportedException($"Strategy {settings.Strategy} not wired."),
        };
    }

    private static GlassOptimizationResult RunRandom(
        GlassProblem problem,
        GlassOptimizationSettings settings,
        IProgress<GlassFitnessSample>? progress,
        CancellationToken ct,
        Stopwatch sw)
    {
        var rnd = new Random(settings.Seed);
        GlassSolution? best = null;
        int done = 0;

        for (int i = 0; i < settings.Iterations; i++)
        {
            if (ct.IsCancellationRequested) break;

            var candidate = (GlassSolution)problem.GenerateCandidates(rnd.Next(), 1).First();
            problem.Evaluate(candidate);
            done++;

            if (best is null || candidate.Fitness > best.Fitness)
                best = (GlassSolution)candidate.Clone();

            progress?.Report(new GlassFitnessSample(done, best.Fitness, (GlassSolution)best.Clone()));
        }

        return BuildResult(problem, settings, best, done, sw, ct.IsCancellationRequested);
    }

    private static GlassOptimizationResult RunEvolutionary(
        GlassProblem problem,
        GlassOptimizationSettings settings,
        IProgress<GlassFitnessSample>? progress,
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

        GlassSolution? bestSoFar = null;
        int gen = 0;

        strategy.GenerationFinished += (_, e) =>
        {
            gen++;
            var bestInGen = e.NewGeneration
                .OfType<GlassSolution>()
                .Where(s => s.HasFitness)
                .OrderByDescending(s => s.Fitness)
                .FirstOrDefault();
            if (bestInGen is null) return;

            if (bestSoFar is null || bestInGen.Fitness > bestSoFar.Fitness)
                bestSoFar = (GlassSolution)bestInGen.Clone();

            progress?.Report(new GlassFitnessSample(gen, bestSoFar.Fitness, (GlassSolution)bestSoFar.Clone()));
        };

        using var reg = ct.Register(() => strategy.Stop());
        _ = strategy.Solve(problem).ToList();

        return BuildResult(problem, settings, bestSoFar, gen, sw, ct.IsCancellationRequested);
    }

    private static GlassOptimizationResult BuildResult(
        GlassProblem problem,
        GlassOptimizationSettings settings,
        GlassSolution? best,
        int iterations,
        Stopwatch sw,
        bool cancelled)
    {
        sw.Stop();

        GlassKpis? kpis = null;
        if (best is not null)
        {
            var handles = GlassTopologyBuilder.Build(best, settings.Seed, problem.ArrivalIntervalMinutes);
            handles.RunFor(settings.SimDurationMinutes);
            kpis = GlassKpis.Extract(handles, settings.SimDurationMinutes, settings.OperatorWagePerHour);
        }

        return new GlassOptimizationResult(
            BestSolution: best,
            BestKpis: kpis,
            TotalIterations: iterations,
            ElapsedMilliseconds: sw.ElapsedMilliseconds,
            WasCancelled: cancelled);
    }
}
