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

    // ── SIM-59 / SIM-68 D7: the swarm reads the vector through IVectorSolution ──

    [Fact]
    public void Solve_SearchesAVectorSolutionWhoseVectorIsNoPublicProperty()
    {
        // reflection on a property named "Parameters" finds nothing here, which is the shape of every
        // real problem's solution; the swarm must still move it and reach the sphere's optimum
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(seed: 42, iterations: 100, swarmSize: 20));

        var best = pso.Solve(new HiddenSphere()).Last();

        best.Fitness.Should().BeGreaterThan(-0.01);
        ((IVectorSolution)best).Parameters.Should().OnlyContain(x => Math.Abs(x) < 0.1);
    }

    [Fact]
    public void Solve_RejectsASolutionWithoutAVector()
    {
        // before SIM-59 the swarm silently took the fitness as a one-dimensional position and searched nothing
        var pso = new ParticleSwarmOptimization();
        pso.Initialize(TestConfiguration.CreatePso(iterations: 5, swarmSize: 3));

        var act = () => pso.Solve(new NoVectorProblem()).ToList();

        act.Should().Throw<ArgumentException>().WithMessage("*IVectorSolution*");
    }

    private sealed class HiddenVector : IVectorSolution
    {
        private double[] point;
        public HiddenVector(double[] point) => this.point = point;
        double[] IVectorSolution.Parameters { get => point; set => point = value; }
        public double Fitness { get; set; } = double.NegativeInfinity;
        public bool HasFitness { get; set; }
        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);
        public object Clone() => new HiddenVector((double[])point.Clone()) { Fitness = Fitness, HasFitness = HasFitness };
    }

    private sealed class HiddenSphere : IProblem
    {
        public double OptimumFitness => 0d;
        public bool IsValid(ISolution solutionCandidate) => solutionCandidate is HiddenVector;

        public bool Evaluate(ISolution solutionCandidate)
        {
            double[] x = ((IVectorSolution)solutionCandidate).Parameters;
            solutionCandidate.Fitness = -x.Sum(v => v * v);
            solutionCandidate.HasFitness = true;
            return true;
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
        {
            var rng = new Random(seed);
            for (int i = 0; i < count; i++)
                yield return new HiddenVector(new[] { rng.NextDouble() * 10 - 5, rng.NextDouble() * 10 - 5, rng.NextDouble() * 10 - 5 });
        }
    }

    private sealed class NoVector : ISolution
    {
        public double Fitness { get; set; }
        public bool HasFitness { get; set; }
        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);
        public object Clone() => new NoVector { Fitness = Fitness, HasFitness = HasFitness };
    }

    private sealed class NoVectorProblem : IProblem
    {
        public double OptimumFitness => 0d;
        public bool IsValid(ISolution solutionCandidate) => true;

        public bool Evaluate(ISolution solutionCandidate)
        {
            solutionCandidate.Fitness = -1d;
            solutionCandidate.HasFitness = true;
            return true;
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count) =>
            Enumerable.Range(0, count).Select(_ => (ISolution)new NoVector());
    }
}
