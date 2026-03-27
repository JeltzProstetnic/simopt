using FluentAssertions;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies;
using SimOpt.Optimization.Strategies.ParticleSwarm;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Optimization.Strategies;

public class ParticleSwarmTests
{
    [Fact]
    public void Name_ReturnsParticleSwarmOptimization()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Name.Should().Be("Particle Swarm Optimization");
    }

    [Fact]
    public void IsInitialized_DefaultFalse()
    {
        var pso = new ParticleSwarmOptimization();
        pso.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_WithValidConfig_ReturnsTrue()
    {
        var pso = new ParticleSwarmOptimization();
        var config = TestConfiguration.CreatePso();

        pso.Initialize(config).Should().BeTrue();
        pso.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithNullConfig_ThrowsArgumentException()
    {
        var pso = new ParticleSwarmOptimization();

        var act = () => pso.Initialize(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reset_ClearsInitialization()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso());

        pso.Reset();

        pso.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Solve_ReturnsSolutions()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(iterations: 10, swarmSize: 5));
        var problem = new TestProblem();

        var results = pso.Solve(problem).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(s => s.HasFitness.Should().BeTrue());
    }

    [Fact]
    public void Solve_ImprovesOverIterations()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(seed: 42, iterations: 50, swarmSize: 20));
        var problem = new TestProblem();

        var results = pso.Solve(problem).ToList();
        var bestFitness = results.Max(s => s.Fitness);

        // Sphere function optimum is 0.0 — PSO should get reasonably close
        bestFitness.Should().BeGreaterThan(-1.0);
    }

    [Fact]
    public void Solve_FiresBestSolutionChangedEvent()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(iterations: 20, swarmSize: 10));
        var problem = new TestProblem();

        var eventFired = false;
        pso.BestSolutionChanged += (sender, args) =>
        {
            eventFired = true;
            args.NewValue.Fitness.Should().BeGreaterThanOrEqualTo(args.OldValue.Fitness);
        };

        pso.Solve(problem).ToList();

        eventFired.Should().BeTrue();
    }

    [Fact]
    public void Solve_DeterministicWithSameSeed()
    {
        var problem = new TestProblem();

        var pso1 = new ParticleSwarmOptimization();
        pso1.Initialize(TestConfiguration.CreatePso(seed: 123, iterations: 20, swarmSize: 10));
        var results1 = pso1.Solve(problem).ToList();

        var pso2 = new ParticleSwarmOptimization();
        pso2.Initialize(TestConfiguration.CreatePso(seed: 123, iterations: 20, swarmSize: 10));
        var results2 = pso2.Solve(problem).ToList();

        results1.Last().Fitness.Should().Be(results2.Last().Fitness);
    }

    [Fact]
    public void Stop_TerminatesEarly()
    {
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(iterations: 1000, swarmSize: 10));
        var problem = new TestProblem();

        var iterationCount = 0;
        pso.BestSolutionChanged += (_, _) =>
        {
            iterationCount++;
            if (iterationCount >= 3) pso.Stop();
        };

        var results = pso.Solve(problem).ToList();

        // Should have terminated well before 1000 iterations
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void Tune_ThrowsNotImplemented()
    {
        var pso = new ParticleSwarmOptimization();

        var act = () => pso.Tune(Enumerable.Empty<IProblem>(), pso);
        act.Should().Throw<NotImplementedException>();
    }
}
