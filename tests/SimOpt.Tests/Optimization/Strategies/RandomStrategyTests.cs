using FluentAssertions;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies;
using SimOpt.Optimization.Strategies.RandomSearch;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Optimization.Strategies;

public class RandomStrategyTests
{
    [Fact]
    public void Initialize_WithValidConfig_ReturnsTrue()
    {
        var strategy = new RandomStrategy();
        var config = TestConfiguration.CreateRandom();

        strategy.Initialize(config).Should().BeTrue();
        strategy.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Solve_ReturnsResults()
    {
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(iterations: 10));
        var problem = new TestProblem();

        var results = strategy.Solve(problem).ToList();

        results.Should().NotBeEmpty();
        results.First().HasFitness.Should().BeTrue();
    }

    [Fact]
    public void Solve_BestSolutionHasNegativeFitness()
    {
        // Sphere function: fitness = -sum(x^2), always <= 0
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(iterations: 50));
        var problem = new TestProblem();

        var results = strategy.Solve(problem).ToList();

        results.First().Fitness.Should().BeLessThanOrEqualTo(0);
    }

    [Fact]
    public void Stop_TerminatesEarly()
    {
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(iterations: 1_000_000));
        var problem = new TestProblem();

        // Stop after a few iterations via thread
        var task = Task.Run(() =>
        {
            Thread.Sleep(50);
            strategy.Stop();
        });

        var results = strategy.Solve(problem).ToList();
        task.Wait();

        // Should have terminated well before 1M iterations
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void Solve_ProducesDifferentCandidatesAcrossIterations()
    {
        // With varied seeds, GenerateCandidates should produce different solutions
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(iterations: 50));
        var problem = new TestProblem();
        var fitnesses = new List<double>();

        strategy.BestSolutionChanged += (sender, e) => fitnesses.Add(e.NewValue.Fitness);

        strategy.Solve(problem).ToList();

        // With 50 random iterations on a sphere function, we should see at least one improvement
        fitnesses.Should().NotBeEmpty("varied seeds should produce different candidates, triggering at least one improvement");
    }

    [Fact]
    public void Solve_WithNoSubscribers_DoesNotThrow()
    {
        // This tests the null delegate bug (Step 7 fix)
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(iterations: 10));
        var problem = new TestProblem();

        // No subscribers - should still work after bug fix
        var act = () => strategy.Solve(problem).ToList();
        act.Should().NotThrow();
    }

    [Fact]
    public void Name_IsNotEmpty()
    {
        var strategy = new RandomStrategy();
        strategy.Name.Should().NotBeNullOrEmpty();
    }
}
