using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Ivotion;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;
using Xunit;

namespace SimOpt.Tests.Ivotion;

/// <summary>
/// Tests for IvotionSolution — 6-dimensional discrete decision vector for the
/// Ivotion packing line optimization. Ranges:
///   [0] roland_count        ∈ {1, 2}
///   [1] operators_inspect   ∈ {1, 2, 3}
///   [2] operators_pack      ∈ {1, 2, 3}
///   [3] operators_label     ∈ {1, 2}
///   [4] operators_ssb       ∈ {1, 2}
///   [5] roland_batch_size   ∈ {10, 15, 20}
/// </summary>
public class IvotionSolutionTests
{
    private static readonly int[] ValidSample = { 1, 2, 2, 1, 1, 15 };

    [Fact]
    public void Constructor_WithValidValues_StoresThem()
    {
        var sol = new IvotionSolution(ValidSample);

        sol.Parameters.Should().Equal(ValidSample);
        sol.RolandCount.Should().Be(1);
        sol.OperatorsInspect.Should().Be(2);
        sol.OperatorsPack.Should().Be(2);
        sol.OperatorsLabel.Should().Be(1);
        sol.OperatorsSsb.Should().Be(1);
        sol.RolandBatchSize.Should().Be(15);
    }

    [Fact]
    public void Constructor_WrongLength_Throws()
    {
        Action act = () => new IvotionSolution(new[] { 1, 1, 1 });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_OutOfRangeValue_ClampsToNearestAllowed()
    {
        // roland_count=5 → clamps to 2, batch_size=12 → clamps to 10
        var sol = new IvotionSolution(new[] { 5, 2, 2, 1, 1, 12 });

        sol.RolandCount.Should().Be(2);
        sol.RolandBatchSize.Should().Be(10);
    }

    [Fact]
    public void InitialFitness_IsUnset()
    {
        var sol = new IvotionSolution(ValidSample);

        sol.HasFitness.Should().BeFalse();
        sol.Fitness.Should().Be(-double.MaxValue);
    }

    [Fact]
    public void Clone_IsDeepCopy()
    {
        var sol = new IvotionSolution(ValidSample) { Fitness = 42.0, HasFitness = true };
        var clone = (IvotionSolution)sol.Clone();

        clone.Parameters.Should().Equal(sol.Parameters);
        clone.Fitness.Should().Be(42.0);
        clone.HasFitness.Should().BeTrue();

        // Mutate clone — original must stay intact
        clone.Parameters[0] = 2;
        sol.Parameters[0].Should().Be(1);
    }

    [Fact]
    public void CompareTo_UsesFitness()
    {
        var a = new IvotionSolution(ValidSample) { Fitness = 10.0, HasFitness = true };
        var b = new IvotionSolution(ValidSample) { Fitness = 20.0, HasFitness = true };

        a.CompareTo(b).Should().BeLessThan(0);
        b.CompareTo(a).Should().BeGreaterThan(0);
        a.CompareTo(a).Should().Be(0);
    }

    [Fact]
    public void Tweak_MutatesExactlyOneVariable()
    {
        var sol = new IvotionSolution(ValidSample) { Fitness = 5.0, HasFitness = true };
        var before = (int[])sol.Parameters.Clone();

        // Seed the RNG for determinism by tweaking many times and checking invariant
        int differences = 0;
        for (int trial = 0; trial < 50; trial++)
        {
            var copy = (IvotionSolution)sol.Clone();
            copy.Tweak();
            int diffs = copy.Parameters.Zip(before, (x, y) => x != y ? 1 : 0).Sum();
            diffs.Should().Be(1, "Tweak must mutate exactly one dimension per call");
            if (diffs == 1) differences++;
        }
        differences.Should().Be(50);
    }

    [Fact]
    public void Tweak_ProducesInRangeValue()
    {
        var sol = new IvotionSolution(ValidSample);

        for (int i = 0; i < 200; i++)
        {
            sol.Tweak();
            IvotionSolution.IsInRange(sol.Parameters).Should().BeTrue(
                $"all 6 parameters must stay in their allowed sets (got [{string.Join(",", sol.Parameters)}])");
        }
    }

    [Fact]
    public void Tweak_ResetsHasFitness()
    {
        var sol = new IvotionSolution(ValidSample) { Fitness = 99.0, HasFitness = true };
        sol.Tweak();

        sol.HasFitness.Should().BeFalse();
    }

    [Fact]
    public void CombineWith_ChildValuesComeFromEitherParent()
    {
        var parentA = new IvotionSolution(new[] { 1, 1, 1, 1, 1, 10 });
        var parentB = new IvotionSolution(new[] { 2, 3, 3, 2, 2, 20 });

        for (int trial = 0; trial < 30; trial++)
        {
            var child = (IvotionSolution)parentA.CombineWith(parentB);

            for (int i = 0; i < 6; i++)
            {
                (child.Parameters[i] == parentA.Parameters[i]
                    || child.Parameters[i] == parentB.Parameters[i])
                    .Should().BeTrue(
                        $"dim {i} must inherit from A ({parentA.Parameters[i]}) or B ({parentB.Parameters[i]}), got {child.Parameters[i]}");
            }
            child.HasFitness.Should().BeFalse();
        }
    }

    [Fact]
    public void CombineWith_WrongType_Throws()
    {
        var sol = new IvotionSolution(ValidSample);
        var other = new DummySolution();

        Action act = () => sol.CombineWith(other);
        act.Should().Throw<ArgumentException>();
    }

    private sealed class DummySolution : ISolution
    {
        public double Fitness { get; set; }
        public bool HasFitness { get; set; }
        public int CompareTo(ISolution? other) => 0;
        public object Clone() => new DummySolution();
    }
}
