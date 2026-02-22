using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// Minimal ISolution implementation for testing optimization strategies.
/// Backed by a double[] parameter vector.
/// </summary>
public class TestSolution : ISolution, ITweakable, ICombinable<ISolution>
{
    private static readonly Random Rng = new(42);

    public double[] Parameters { get; set; }
    public double Fitness { get; set; } = -double.MaxValue;
    public bool HasFitness { get; set; }

    public TestSolution(double[] parameters)
    {
        Parameters = (double[])parameters.Clone();
    }

    public TestSolution(int dimensions, Random? rng = null)
    {
        var r = rng ?? Rng;
        Parameters = new double[dimensions];
        for (int i = 0; i < dimensions; i++)
            Parameters[i] = r.NextDouble() * 10 - 5; // [-5, 5]
    }

    public int CompareTo(ISolution? other)
    {
        if (other is null) return 1;
        return Fitness.CompareTo(other.Fitness);
    }

    public object Clone()
    {
        return new TestSolution((double[])Parameters.Clone())
        {
            Fitness = this.Fitness,
            HasFitness = this.HasFitness
        };
    }

    public void Tweak()
    {
        var r = new Random();
        int idx = r.Next(Parameters.Length);
        Parameters[idx] += (r.NextDouble() - 0.5) * 0.1;
        HasFitness = false;
    }

    public ISolution CombineWith(ISolution other)
    {
        if (other is not TestSolution otherTs)
            throw new ArgumentException("Can only combine with TestSolution");

        var child = new double[Parameters.Length];
        for (int i = 0; i < Parameters.Length; i++)
            child[i] = (Parameters[i] + otherTs.Parameters[i]) / 2.0;

        return new TestSolution(child);
    }
}
