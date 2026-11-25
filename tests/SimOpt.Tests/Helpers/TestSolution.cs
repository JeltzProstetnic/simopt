using SimOpt.Optimization;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// Minimal ISolution implementation for testing optimization strategies.
/// Backed by a double[] parameter vector. Its moves draw from a generator that travels with it (SIM-59),
/// so a run is reproducible from its seed.
/// </summary>
public class TestSolution : IVectorSolution, ITweakable, ICombinable<ISolution>
{
    private static readonly Random Rng = new(42);

    private SplitMix64 moves;

    public double[] Parameters { get; set; }
    public double Fitness { get; set; } = -double.MaxValue;
    public bool HasFitness { get; set; }

    public TestSolution(double[] parameters)
    {
        Parameters = (double[])parameters.Clone();
    }

    private TestSolution(double[] parameters, SplitMix64 moves) : this(parameters) => this.moves = moves;

    public TestSolution(int dimensions, Random? rng = null)
    {
        var r = rng ?? Rng;
        Parameters = new double[dimensions];
        for (int i = 0; i < dimensions; i++)
            Parameters[i] = r.NextDouble() * 10 - 5; // [-5, 5]
        moves = new SplitMix64((ulong)r.NextInt64());
    }

    public int CompareTo(ISolution? other)
    {
        if (other is null) return 1;
        return Fitness.CompareTo(other.Fitness);
    }

    public object Clone()
    {
        return new TestSolution(Parameters, moves.Split())
        {
            Fitness = this.Fitness,
            HasFitness = this.HasFitness
        };
    }

    public void Tweak()
    {
        int idx = moves.Below(Parameters.Length);
        Parameters[idx] += (moves.Uniform() - 0.5) * 0.1;
        HasFitness = false;
    }

    public ISolution CombineWith(ISolution other)
    {
        if (other is not TestSolution otherTs)
            throw new ArgumentException("Can only combine with TestSolution");

        var child = new double[Parameters.Length];
        for (int i = 0; i < Parameters.Length; i++)
            child[i] = (Parameters[i] + otherTs.Parameters[i]) / 2.0;

        return new TestSolution(child, new SplitMix64(moves.Next()));
    }
}
