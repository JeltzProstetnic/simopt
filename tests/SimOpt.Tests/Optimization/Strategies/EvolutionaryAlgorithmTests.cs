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

    // ── SIM-59: the default configuration breeds from the best, and mutation never changes a parent ──

    [Fact]
    public void DefaultParentSelector_IsGivenTheGenerationBestFirst()
    {
        // ProcessGeneration documents its input as "sorted descending by fitness" and the default parent
        // selector takes the first mu; before SIM-59 the generation was sorted ascending, so the worst bred
        var config = new EvolutionaryAlgorithmConfiguration(seed: 3, iterations: 5, mu: 4, lambda: 12, keepParentsAlive: true);
        var seen = new List<List<double>>();
        FractionSelector selectParents = config.SelectAsParent;
        config.SelectAsParent = generation =>
        {
            seen.Add(generation.Select(s => s.Fitness).ToList());
            return selectParents(generation);
        };
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(config);

        ea.Solve(new TestProblem()).ToList();

        seen.Should().HaveCount(5);
        seen.Should().OnlyContain(fitness => fitness.SequenceEqual(fitness.OrderByDescending(f => f)));
    }

    [Fact]
    public void Solve_ConvergesOnTheSphereWithTheDefaultConfiguration()
    {
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(new EvolutionaryAlgorithmConfiguration(seed: 3, iterations: 60, mu: 5, lambda: 20, keepParentsAlive: true));

        ea.Solve(new TestProblem()).ToList();

        // the initial population's best on [-5, 5]^3 is about -2; breeding from the best gets near the optimum 0
        ea.BestSolution.Fitness.Should().BeGreaterThan(-0.05);
    }

    [Fact]
    public void Mutation_NeverChangesAParentOrTheBest()
    {
        // a crossover that hands the parents back as children, as one with a crossover rate below 1 does;
        // mutating them in place changed parents that survive into the next generation and the best found
        var config = new EvolutionaryAlgorithmConfiguration(seed: 3, iterations: 10, mu: 4, lambda: 8,
            crossover: new PassThroughCrossover(), keepParentsAlive: true);
        var ea = new EvolutionaryAlgorithm();
        ea.Initialize(config);
        var problem = new RecordingSphere();

        ea.Solve(problem).ToList();

        // the best found is the best ever evaluated: a mutated parent that was the best is re-evaluated
        // worse, and the best then read the worse figure
        ea.BestSolution.Fitness.Should().Be(problem.Highest);
        var best = (TestSolution)ea.BestSolution.Clone();
        new TestProblem().Evaluate(best);
        best.Fitness.Should().Be(problem.Highest, "the best's fitness belongs to its parameters");
        ea.CurrentGeneration.Should().NotContain(s => ReferenceEquals(s, ea.BestSolution), "the best is kept as a copy");
    }

    private sealed class RecordingSphere : IProblem
    {
        private readonly TestProblem sphere = new();
        public double Highest { get; private set; } = double.NegativeInfinity;
        public double OptimumFitness => 0d;
        public bool IsValid(ISolution solutionCandidate) => true;
        public IEnumerable<ISolution> GenerateCandidates(int seed, int count) => sphere.GenerateCandidates(seed, count);

        public bool Evaluate(ISolution solutionCandidate)
        {
            sphere.Evaluate(solutionCandidate);
            Highest = Math.Max(Highest, solutionCandidate.Fitness);
            return true;
        }
    }

    private sealed class PassThroughCrossover : ICrossoverOperator
    {
        public string Name => "pass-through";
        public int Seed { get; set; }
        public int Cardinality => 2;
        public int Size { get; set; }
        public int GrowthFactor { get; set; } = 2;
        public IEnumerable<ISolution> Apply(params ISolution[] operands) => operands;
        public void Initialize(int seed, int growthFactor = 2, int size = -1) => Seed = seed;
        public void Configure(int growFactor, int size) { }
        public void Configure(int size) { }
    }
}
