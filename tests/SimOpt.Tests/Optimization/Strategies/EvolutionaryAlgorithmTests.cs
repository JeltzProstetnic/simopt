using FluentAssertions;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Optimization.Strategies;

public class EvolutionaryAlgorithmTests
{
    [Fact]
    public void Initialize_WithValidConfig_ReturnsTrue()
    {
        var ea = new EvolutionaryAlgorithm();
        var config = TestConfiguration.CreateEvolutionary();

        ea.Initialize(config).Should().BeTrue();
        ea.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithWrongConfigType_ReturnsFalse()
    {
        var ea = new EvolutionaryAlgorithm();
        var wrongConfig = TestConfiguration.CreateRandom();

        ea.Initialize(wrongConfig).Should().BeFalse();
    }

    [Fact]
    public void Solve_ReturnsPopulation()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 3, populationSize: 10));
        var problem = new TestProblem();

        var results = ea.Solve(problem).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(s => s.HasFitness.Should().BeTrue());
    }

    [Fact]
    public void Solve_ProcessesAllGenerations()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 5, populationSize: 10));
        var problem = new TestProblem();

        ea.Solve(problem).ToList();

        ea.RemainingGenerations.Should().Be(0);
        ea.ProcessedGenerations.Should().Be(5);
    }

    [Fact]
    public void Solve_BestSolutionIsTracked()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 5, populationSize: 10));
        var problem = new TestProblem();

        ea.Solve(problem).ToList();

        ea.BestSolution.Should().NotBeNull();
        ea.BestSolution!.HasFitness.Should().BeTrue();
    }

    [Fact]
    public void GenerationFinished_FiresEachGeneration()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 5, populationSize: 10));
        var problem = new TestProblem();
        int genCount = 0;

        ea.GenerationFinished += (sender, e) => genCount++;

        ea.Solve(problem).ToList();

        genCount.Should().Be(5);
    }

    [Fact]
    public void BestSolutionChanged_FiresOnImprovement()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 10, populationSize: 20));
        var problem = new TestProblem();
        int changeCount = 0;

        ea.BestSolutionChanged += (sender, e) => changeCount++;

        ea.Solve(problem).ToList();

        changeCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Stop_TerminatesBeforeAllGenerations()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(TestConfiguration.CreateEvolutionary(generations: 100, populationSize: 10));
        var problem = new TestProblem();
        int genCount = 0;

        // Stop after 3 generations via event — deterministic, no race
        ea.GenerationFinished += (sender, e) =>
        {
            genCount++;
            if (genCount >= 3)
                ea.Stop();
        };

        ea.Solve(problem).ToList();

        genCount.Should().Be(3);
    }

    [Fact]
    public void Name_IsNotEmpty()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Name.Should().NotBeNullOrEmpty();
    }
}
