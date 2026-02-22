using SimOpt.Optimization.Interfaces;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// Sphere function problem for testing: fitness = -sum(x_i^2).
/// Optimum at origin with fitness 0. Deterministic.
/// </summary>
public class TestProblem : IProblem
{
    public int Dimensions { get; }

    public double OptimumFitness => 0.0;

    public TestProblem(int dimensions = 3)
    {
        Dimensions = dimensions;
    }

    public bool IsValid(ISolution solutionCandidate) => true;

    public bool Evaluate(ISolution solutionCandidate)
    {
        if (solutionCandidate is not TestSolution ts)
            throw new ArgumentException("Expected TestSolution");

        double sum = 0;
        for (int i = 0; i < ts.Parameters.Length; i++)
            sum += ts.Parameters[i] * ts.Parameters[i];

        ts.Fitness = -sum;
        ts.HasFitness = true;
        return true;
    }

    public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
    {
        var rng = new Random(seed);
        for (int i = 0; i < count; i++)
            yield return new TestSolution(Dimensions, rng);
    }
}
