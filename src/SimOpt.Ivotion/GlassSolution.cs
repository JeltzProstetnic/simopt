using System;
using System.Linq;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Glass;

/// <summary>
/// Decision vector for the generic glass base-mass production-line optimization
/// problem. 5 discrete dimensions (int values).
/// Total search space: 3×2×4×3×2 = 144.
///
/// Dim | Meaning                     | Allowed values
/// ----|-----------------------------|--------------------
///   0 | NumberOfMixers              | {1, 2, 3}
///   1 | NumberOfProcessingLines     | {1, 2}
///   2 | IntermediateBufferCapacity  | {4, 8, 12, 20}
///   3 | OperatorsQuality            | {1, 2, 3}
///   4 | OperatorsPacking            | {1, 2}
/// </summary>
public sealed class GlassSolution : ISolution, ITweakable, ICombinable<ISolution>
{
    public static readonly int[][] AllowedValues =
    {
        new[] { 1, 2, 3 },
        new[] { 1, 2 },
        new[] { 4, 8, 12, 20 },
        new[] { 1, 2, 3 },
        new[] { 1, 2 },
    };

    public const int DimensionCount = 5;

    public int[] Parameters { get; }
    public double Fitness { get; set; } = -double.MaxValue;
    public bool HasFitness { get; set; }

    public int NumberOfMixers => Parameters[0];
    public int NumberOfProcessingLines => Parameters[1];
    public int IntermediateBufferCapacity => Parameters[2];
    public int OperatorsQuality => Parameters[3];
    public int OperatorsPacking => Parameters[4];

    public GlassSolution(int[] parameters)
    {
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));
        if (parameters.Length != DimensionCount)
            throw new ArgumentException(
                $"GlassSolution requires exactly {DimensionCount} parameters, got {parameters.Length}.",
                nameof(parameters));

        Parameters = new int[DimensionCount];
        for (int i = 0; i < DimensionCount; i++)
            Parameters[i] = ClampToAllowed(i, parameters[i]);
    }

    public int CompareTo(ISolution? other)
    {
        if (other is null) return 1;
        return Fitness.CompareTo(other.Fitness);
    }

    public object Clone()
    {
        return new GlassSolution((int[])Parameters.Clone())
        {
            Fitness = this.Fitness,
            HasFitness = this.HasFitness,
        };
    }

    public void Tweak() => Tweak(new Random());

    /// <summary>Deterministic overload for testing.</summary>
    public void Tweak(Random rng)
    {
        int dim = rng.Next(DimensionCount);
        int[] allowed = AllowedValues[dim];

        // Pick a value different from the current one (guaranteed possible because
        // every dimension has at least two allowed values).
        int current = Parameters[dim];
        int pick;
        do
        {
            pick = allowed[rng.Next(allowed.Length)];
        } while (pick == current);

        Parameters[dim] = pick;
        HasFitness = false;
    }

    public ISolution CombineWith(ISolution other) => CombineWith(other, new Random());

    /// <summary>Deterministic overload for testing.</summary>
    public ISolution CombineWith(ISolution other, Random rng)
    {
        if (other is not GlassSolution b)
            throw new ArgumentException(
                $"GlassSolution can only combine with another GlassSolution (got {other?.GetType().Name ?? "null"}).",
                nameof(other));

        var childParams = new int[DimensionCount];
        for (int i = 0; i < DimensionCount; i++)
            childParams[i] = rng.Next(2) == 0 ? Parameters[i] : b.Parameters[i];

        return new GlassSolution(childParams);
    }

    public static bool IsInRange(int[] parameters)
    {
        if (parameters is null || parameters.Length != DimensionCount) return false;
        for (int i = 0; i < DimensionCount; i++)
            if (!AllowedValues[i].Contains(parameters[i]))
                return false;
        return true;
    }

    private static int ClampToAllowed(int dim, int value)
    {
        int[] allowed = AllowedValues[dim];
        if (allowed.Contains(value)) return value;

        int best = allowed[0];
        int bestDist = Math.Abs(value - best);
        for (int i = 1; i < allowed.Length; i++)
        {
            int d = Math.Abs(value - allowed[i]);
            if (d < bestDist) { bestDist = d; best = allowed[i]; }
        }
        return best;
    }
}
