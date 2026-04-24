using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Ivotion;
using SimOpt.Optimization.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Ivotion;

public class IvotionProblemTests
{
    private readonly ITestOutputHelper _out;

    public IvotionProblemTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void IsValid_RejectsNonIvotionSolution()
    {
        var problem = new IvotionProblem();
        var foreign = new ForeignSolution();

        problem.IsValid(foreign).Should().BeFalse();
    }

    [Fact]
    public void IsValid_AcceptsWellFormedIvotionSolution()
    {
        var problem = new IvotionProblem();
        var sol = new IvotionSolution(new[] { 1, 2, 2, 1, 1, 15 });

        problem.IsValid(sol).Should().BeTrue();
    }

    [Fact]
    public void GenerateCandidates_ReturnsRequestedCount()
    {
        var problem = new IvotionProblem();

        var candidates = problem.GenerateCandidates(seed: 42, count: 20).ToList();

        candidates.Should().HaveCount(20);
        candidates.Should().AllBeOfType<IvotionSolution>();
    }

    [Fact]
    public void GenerateCandidates_AllInRange()
    {
        var problem = new IvotionProblem();

        var candidates = problem.GenerateCandidates(seed: 7, count: 100).Cast<IvotionSolution>();

        foreach (var c in candidates)
            IvotionSolution.IsInRange(c.Parameters).Should().BeTrue(
                $"got [{string.Join(",", c.Parameters)}]");
    }

    [Fact]
    public void GenerateCandidates_SameSeed_ReturnsIdenticalCandidates()
    {
        var problem = new IvotionProblem();

        var a = problem.GenerateCandidates(seed: 123, count: 10)
            .Cast<IvotionSolution>().Select(s => s.Parameters).ToList();
        var b = problem.GenerateCandidates(seed: 123, count: 10)
            .Cast<IvotionSolution>().Select(s => s.Parameters).ToList();

        for (int i = 0; i < 10; i++)
            a[i].Should().Equal(b[i]);
    }

    [Fact]
    public void GenerateCandidates_DifferentSeeds_ReturnDifferentCandidates()
    {
        var problem = new IvotionProblem();

        var a = problem.GenerateCandidates(seed: 1, count: 20)
            .Cast<IvotionSolution>().Select(s => string.Join(",", s.Parameters)).ToList();
        var b = problem.GenerateCandidates(seed: 2, count: 20)
            .Cast<IvotionSolution>().Select(s => string.Join(",", s.Parameters)).ToList();

        a.SequenceEqual(b).Should().BeFalse("different seeds should explore different parts of the space");
    }

    [Fact]
    public void Evaluate_SetsFitnessAndHasFitness()
    {
        var problem = new IvotionProblem
        {
            Objective = IvotionObjective.MaximizeThroughput,
            SimDurationMinutes = 60.0
        };
        var sol = new IvotionSolution(new[] { 1, 2, 2, 1, 1, 15 });

        bool ok = problem.Evaluate(sol);

        ok.Should().BeTrue();
        sol.HasFitness.Should().BeTrue();
        sol.Fitness.Should().NotBe(-double.MaxValue);
    }

    [Fact]
    public void Evaluate_IsDeterministic_ForSameSeed()
    {
        var problem = new IvotionProblem
        {
            Objective = IvotionObjective.MaximizeThroughput,
            SimDurationMinutes = 60.0,
            Seed = 999
        };
        var solA = new IvotionSolution(new[] { 1, 2, 2, 1, 1, 15 });
        var solB = (IvotionSolution)solA.Clone();

        problem.Evaluate(solA);
        problem.Evaluate(solB);

        solB.Fitness.Should().Be(solA.Fitness);
    }

    [Fact]
    public void Evaluate_MinimizeTotalCost_RanksCheaperSolutionHigher()
    {
        var problem = new IvotionProblem
        {
            Objective = IvotionObjective.MinimizeTotalCost,
            SimDurationMinutes = 60.0
        };
        var cheap = new IvotionSolution(new[] { 1, 1, 1, 1, 1, 15 });  // 4 ops + 1 Roland
        var pricey = new IvotionSolution(new[] { 2, 3, 3, 2, 2, 15 }); // 10 ops + 2 Rolands

        problem.Evaluate(cheap);
        problem.Evaluate(pricey);

        cheap.Fitness.Should().BeGreaterThan(pricey.Fitness,
            "minimizing cost means cheaper config earns higher fitness");
    }

    [Fact]
    public void Evaluate_MaximizeThroughput_TwoRolandsBeatsOneAtSaturation()
    {
        var problem = new IvotionProblem
        {
            Objective = IvotionObjective.MaximizeThroughput,
            SimDurationMinutes = 480.0,
            ArrivalIntervalMinutes = 0.1
        };
        var oneR = new IvotionSolution(new[] { 1, 3, 3, 2, 2, 15 });
        var twoR = new IvotionSolution(new[] { 2, 3, 3, 2, 2, 15 });

        problem.Evaluate(oneR);
        problem.Evaluate(twoR);

        _out.WriteLine($"1× Roland fitness: {oneR.Fitness}");
        _out.WriteLine($"2× Roland fitness: {twoR.Fitness}");

        twoR.Fitness.Should().BeGreaterThan(oneR.Fitness);
    }

    [Fact]
    public void Evaluate_NullCandidate_Throws()
    {
        var problem = new IvotionProblem();

        Action act = () => problem.Evaluate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Evaluate_InvalidCandidateType_ReturnsFalse()
    {
        var problem = new IvotionProblem();
        var foreign = new ForeignSolution();

        problem.Evaluate(foreign).Should().BeFalse();
        foreign.Fitness.Should().Be(-double.MaxValue);
    }

    private sealed class ForeignSolution : ISolution
    {
        public double Fitness { get; set; } = -double.MaxValue;
        public bool HasFitness { get; set; }
        public int CompareTo(ISolution? other) => 0;
        public object Clone() => new ForeignSolution();
    }
}
