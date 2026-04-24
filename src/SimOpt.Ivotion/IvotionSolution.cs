using System;
using System.Linq;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;

namespace SimOpt.Ivotion;

/// <summary>
/// Decision vector for the Ivotion packing line optimization problem.
/// 6 discrete dimensions (int values). Total search space: 2×3×3×2×2×3 = 216.
///
/// Dim | Meaning              | Allowed values
/// ----|----------------------|--------------------
///   0 | roland_count         | {1, 2}
///   1 | operators_inspect    | {1, 2, 3}
///   2 | operators_pack       | {1, 2, 3}
///   3 | operators_label      | {1, 2}
///   4 | operators_ssb        | {1, 2}
///   5 | roland_batch_size    | {10, 15, 20}
/// </summary>
public sealed class IvotionSolution : ISolution, ITweakable, ICombinable<ISolution>
{
    public static readonly int[][] AllowedValues =
    {
        new[] { 1, 2 },
        new[] { 1, 2, 3 },
        new[] { 1, 2, 3 },
        new[] { 1, 2 },
        new[] { 1, 2 },
        new[] { 10, 15, 20 },
    };

    public const int DimensionCount = 6;

    public int[] Parameters { get; }
    public double Fitness { get; set; } = -double.MaxValue;
    public bool HasFitness { get; set; }

    public int RolandCount => Parameters[0];
    public int OperatorsInspect => Parameters[1];
    public int OperatorsPack => Parameters[2];
    public int OperatorsLabel => Parameters[3];
    public int OperatorsSsb => Parameters[4];
    public int RolandBatchSize => Parameters[5];

    public IvotionSolution(int[] parameters)
    {
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));
        if (parameters.Length != DimensionCount)
            throw new ArgumentException(
                $"IvotionSolution requires exactly {DimensionCount} parameters, got {parameters.Length}.",
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
        return new IvotionSolution((int[])Parameters.Clone())
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
        if (other is not IvotionSolution b)
            throw new ArgumentException(
                $"IvotionSolution can only combine with another IvotionSolution (got {other?.GetType().Name ?? "null"}).",
                nameof(other));

        var childParams = new int[DimensionCount];
        for (int i = 0; i < DimensionCount; i++)
            childParams[i] = rng.Next(2) == 0 ? Parameters[i] : b.Parameters[i];

        return new IvotionSolution(childParams);
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
