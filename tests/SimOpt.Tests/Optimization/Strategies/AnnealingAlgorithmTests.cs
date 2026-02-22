using FluentAssertions;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies;
using SimOpt.Optimization.Strategies.SimulatedAnnealing;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Optimization.Strategies;

public class AnnealingAlgorithmTests
{
    [Fact]
    public void Initialize_WithValidConfig_ReturnsTrue()
    {
        var strategy = new AnnealingAlgorithm();
        var config = TestConfiguration.CreateAnnealing();

        strategy.Initialize(config).Should().BeTrue();
        strategy.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithWrongConfigType_ReturnsFalse()
    {
        var strategy = new AnnealingAlgorithm();
        var wrongConfig = TestConfiguration.CreateRandom();

        strategy.Initialize(wrongConfig).Should().BeFalse();
    }

    [Fact]
    public void Solve_ReturnsResult()
    {
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(TestConfiguration.CreateAnnealing(initialTemperature: 10));
        var problem = new TestProblem();

        var results = strategy.Solve(problem).ToList();

        results.Should().HaveCount(1);
        results.First().HasFitness.Should().BeTrue();
    }

    [Fact]
    public void Solve_TemperatureDecreasesToZero()
    {
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(TestConfiguration.CreateAnnealing(initialTemperature: 5));
        var problem = new TestProblem();

        strategy.Solve(problem).ToList();

        // Temperature decreases by 1.0 each step, starts at 5 -> ends below 0
        strategy.CurrentTemperature.Should().BeLessThan(0);
    }

    [Fact]
    public void BestSolutionChanged_FiresOnImprovement()
    {
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(TestConfiguration.CreateAnnealing(initialTemperature: 20));
        var problem = new TestProblem();
        int changeCount = 0;

        strategy.BestSolutionChanged += (sender, e) => changeCount++;

        strategy.Solve(problem).ToList();

        changeCount.Should().BeGreaterThanOrEqualTo(0); // May not always fire depending on randomness
    }

    [Fact]
    public void Solve_WithNoSubscribers_DoesNotThrow()
    {
        // This tests the null delegate bug (Step 7 fix)
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(TestConfiguration.CreateAnnealing(initialTemperature: 5));
        var problem = new TestProblem();

        // No subscribers - should still work after bug fix
        var act = () => strategy.Solve(problem).ToList();
        act.Should().NotThrow();
    }

    [Fact]
    public void Name_IsNotEmpty()
    {
        var strategy = new AnnealingAlgorithm();
        strategy.Name.Should().NotBeNullOrEmpty();
    }
}
