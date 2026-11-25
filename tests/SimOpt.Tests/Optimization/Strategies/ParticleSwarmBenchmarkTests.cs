using FluentAssertions;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.ParticleSwarm;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Optimization.Strategies;

/// <summary>
/// SIM-36: the particle swarm against the two textbook benchmarks, with the configuration's own
/// constriction defaults (w = 0.7298, c1 = c2 = 1.49618), at every one of five seeds so a lucky seed
/// cannot carry it. The sphere is smooth and unimodal: a working swarm gets to its optimum at the origin
/// almost exactly. Rosenbrock's optimum at (1, 1) lies at the end of a long curved valley, which a
/// swarm whose velocity update is wrong does not follow.
/// </summary>
public class ParticleSwarmBenchmarkTests
{
    private static ISolution Best(IProblem problem, int seed, int iterations, int swarm = 20)
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(new ParticleSwarmConfiguration { Seed = seed, NumberOfIterations = iterations, SwarmSize = swarm });
        return pso.Solve(problem).Last();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void TheFiveDimensionalSphere_IsSolvedToWithinOneMillionth(int seed)
    {
        ISolution best = Best(new TestProblem(dimensions: 5), seed, iterations: 200);

        best.Fitness.Should().BeGreaterThan(-1e-6);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void TheRosenbrockValley_IsFollowedToItsOptimumAtOneOne(int seed)
    {
        var best = (IVectorSolution)Best(new Rosenbrock(), seed, iterations: 500);

        best.Fitness.Should().BeGreaterThan(-1e-4);
        best.Parameters[0].Should().BeApproximately(1, 0.02);
        best.Parameters[1].Should().BeApproximately(1, 0.04);
    }

    /// <summary>f(x, y) = (1 - x)^2 + 100 (y - x^2)^2, maximised as -f; start points uniform in [-5, 5]^2.</summary>
    private sealed class Rosenbrock : IProblem
    {
        public double OptimumFitness => 0.0;

        public bool IsValid(ISolution solutionCandidate) => true;

        public bool Evaluate(ISolution solutionCandidate)
        {
            double[] p = ((IVectorSolution)solutionCandidate).Parameters;
            double x = p[0], y = p[1];
            solutionCandidate.Fitness = -((1 - x) * (1 - x) + 100 * (y - x * x) * (y - x * x));
            solutionCandidate.HasFitness = true;
            return true;
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
        {
            var rng = new Random(seed);
            for (int i = 0; i < count; i++) yield return new TestSolution(2, rng);
        }
    }
}
