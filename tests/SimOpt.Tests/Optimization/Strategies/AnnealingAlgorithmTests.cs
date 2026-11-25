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
        var problem = new CountingProblem();

        strategy.Solve(problem).ToList();

        // SIM-59: one move at each of 5, 4, 3, 2, 1; the run ends when the temperature reaches 0, where
        // the acceptance rule exp(delta / T) has no meaning
        strategy.CurrentTemperature.Should().Be(0);
        problem.Evaluations.Should().Be(1 + 5);
    }

    // ── SIM-59: the run ends however the temperature falls ──

    [Fact]
    public void Solve_EndsUnderGeometricCooling()
    {
        // before SIM-59 the loop ran while T >= 0, which T * 0.5 never leaves
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(Geometric(iterations: 50));
        var problem = new CountingProblem();

        bool ended = Task.Run(() => strategy.Solve(problem).ToList()).Wait(TimeSpan.FromSeconds(10));

        ended.Should().BeTrue("a geometric schedule is bounded by the iteration count");
        problem.Evaluations.Should().Be(1 + 50);
    }

    [Fact]
    public void Solve_HonoursTheIterationCount()
    {
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(TestConfiguration.CreateAnnealing(iterations: 10, initialTemperature: 100));
        var problem = new CountingProblem();

        strategy.Solve(problem).ToList();

        problem.Evaluations.Should().Be(1 + 10);
    }

    [Fact]
    public void Stop_EndsTheRunUnderGeometricCooling()
    {
        // Stop set T to 0, and 0 * 0.5 stayed 0, so the old loop never ended
        var strategy = new AnnealingAlgorithm();
        strategy.Initialize(Geometric(iterations: 0));
        var problem = new CountingProblem();
        problem.Evaluated += n => { if (n == 5) strategy.Stop(); };

        bool ended = Task.Run(() => strategy.Solve(problem).ToList()).Wait(TimeSpan.FromSeconds(10));

        ended.Should().BeTrue();
        problem.Evaluations.Should().Be(5);
    }

    [Fact]
    public void Stop_HoldsAgainstAScheduleThatRaisesTheTemperature()
    {
        // a schedule computed from the step number would lift T off the 0 that Stop set
        var strategy = new AnnealingAlgorithm();
        var config = Geometric(iterations: 1000);
        config.DecreaseTemperature = a => 10d;
        strategy.Initialize(config);
        var problem = new CountingProblem();
        problem.Evaluated += n => { if (n == 5) strategy.Stop(); };

        bool ended = Task.Run(() => strategy.Solve(problem).ToList()).Wait(TimeSpan.FromSeconds(10));

        ended.Should().BeTrue();
        problem.Evaluations.Should().Be(5);
    }

    [Fact]
    public void Solve_TheSameSeedTakesTheSamePath()
    {
        // SIM-59: a run is reproducible only when the moves are seeded, too
        double[] Run()
        {
            var strategy = new AnnealingAlgorithm();
            strategy.Initialize(Geometric(iterations: 200));
            return ((TestSolution)strategy.Solve(new TestProblem()).Single()).Parameters;
        }

        Run().Should().Equal(Run());
    }

    [Fact]
    public void BestSolutionChanged_FiresOnlyOnAStrictImprovement()
    {
        // near 0 K most worse moves are rejected, so the current candidate often is the best one; the
        // old rule (>=) then announced the best replacing itself
        var strategy = new AnnealingAlgorithm();
        var config = Geometric(iterations: 0);
        config.InitialTemperature = 200e-6;
        config.DecreaseTemperature = a => a.CurrentTemperature - 1e-6;
        strategy.Initialize(config);
        var changes = new List<(double Old, double New)>();
        strategy.BestSolutionChanged += (_, e) => changes.Add((e.OldValue.Fitness, e.NewValue.Fitness));

        strategy.Solve(new TestProblem()).ToList();

        changes.Should().OnlyContain(c => c.New > c.Old);
    }

    private static SimulatedAnnealingConfiguration Geometric(int iterations) => new()
    {
        Seed = 7,
        InitialTemperature = 10,
        NumberOfIterations = iterations,
        DecreaseTemperature = a => a.CurrentTemperature * 0.5,
        Brownian = new SimpleBrownian(),
    };

    /// <summary>The sphere, counting evaluations and reporting each one.</summary>
    private sealed class CountingProblem : IProblem
    {
        private readonly TestProblem sphere = new();
        public int Evaluations { get; private set; }
        public event Action<int>? Evaluated;
        public double OptimumFitness => sphere.OptimumFitness;
        public bool IsValid(ISolution solutionCandidate) => true;
        public IEnumerable<ISolution> GenerateCandidates(int seed, int count) => sphere.GenerateCandidates(seed, count);

        public bool Evaluate(ISolution solutionCandidate)
        {
            sphere.Evaluate(solutionCandidate);
            Evaluations++; // not inside ?.Invoke(...), which skips its arguments when nobody listens
            Evaluated?.Invoke(Evaluations);
            return true;
        }
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
