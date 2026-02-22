using Moq;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;
using SimOpt.Optimization.Strategies.SimulatedAnnealing;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// Factory for creating test configurations for optimization strategies.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Creates a minimal IConfiguration for RandomStrategy.
    /// </summary>
    public static IConfiguration CreateRandom(int seed = 42, int iterations = 50)
    {
        var config = new Mock<IConfiguration>();
        config.SetupProperty(c => c.Seed, seed);
        config.SetupProperty(c => c.NumberOfIterations, iterations);
        config.SetupProperty(c => c.NumberOfEvaluations, iterations);
        config.SetupProperty(c => c.Fitness, -double.MaxValue);
        config.SetupProperty(c => c.HasFitness, false);
        config.Setup(c => c.CompareTo(It.IsAny<ISolution>())).Returns(0);
        config.Setup(c => c.Clone()).Returns(() => config.Object);
        return config.Object;
    }

    /// <summary>
    /// Creates a minimal ISimulatedAnnealingConfiguration.
    /// </summary>
    public static ISimulatedAnnealingConfiguration CreateAnnealing(
        int seed = 42,
        int iterations = 100,
        double initialTemperature = 100.0)
    {
        var brownian = new Mock<IBrownianOperator>();
        brownian.Setup(b => b.Apply(It.IsAny<ISolution[]>()))
            .Returns((ISolution[] operands) =>
            {
                var clone = (TestSolution)operands[0].Clone();
                var rng = new Random();
                int idx = rng.Next(clone.Parameters.Length);
                clone.Parameters[idx] += (rng.NextDouble() - 0.5) * 2.0;
                clone.HasFitness = false;
                return clone;
            });

        var config = new Mock<ISimulatedAnnealingConfiguration>();
        config.SetupProperty(c => c.Seed, seed);
        config.SetupProperty(c => c.NumberOfIterations, iterations);
        config.SetupProperty(c => c.NumberOfEvaluations, iterations);
        config.SetupProperty(c => c.Fitness, -double.MaxValue);
        config.SetupProperty(c => c.HasFitness, false);
        config.SetupProperty(c => c.InitialTemperature, initialTemperature);
        config.Setup(c => c.Brownian).Returns(brownian.Object);
        config.Setup(c => c.DecreaseTemperature).Returns(
            (AnnealingAlgorithm alg) => alg.CurrentTemperature - 1.0);
        config.Setup(c => c.CompareTo(It.IsAny<ISolution>())).Returns(0);
        config.Setup(c => c.Clone()).Returns(() => config.Object);
        return config.Object;
    }

    /// <summary>
    /// Creates a minimal IEvolutionaryAlgorithmConfiguration.
    /// </summary>
    public static IEvolutionaryAlgorithmConfiguration CreateEvolutionary(
        int seed = 42,
        int generations = 10,
        int populationSize = 20,
        double mutationRate = 0.1)
    {
        var mutation = new Mock<IMutationOperator>();
        mutation.Setup(m => m.Apply(It.IsAny<ISolution[]>()))
            .Returns((ISolution[] operands) =>
            {
                var clone = (TestSolution)operands[0].Clone();
                clone.Tweak();
                return clone;
            });

        var crossover = new Mock<ICrossoverOperator>();
        crossover.Setup(c => c.Apply(It.IsAny<ISolution[]>()))
            .Returns((ISolution[] operands) =>
            {
                if (operands.Length < 2)
                    return new[] { (ISolution)(TestSolution)operands[0].Clone() };
                var p1 = (TestSolution)operands[0];
                var p2 = (TestSolution)operands[1];
                return new[] { p1.CombineWith(p2), p2.CombineWith(p1) };
            });
        crossover.SetupProperty(c => c.GrowthFactor, 2);

        var config = new Mock<IEvolutionaryAlgorithmConfiguration>();
        config.SetupProperty(c => c.Seed, seed);
        config.SetupProperty(c => c.NumberOfIterations, generations);
        config.SetupProperty(c => c.NumberOfEvaluations, generations * populationSize);
        config.SetupProperty(c => c.Fitness, -double.MaxValue);
        config.SetupProperty(c => c.HasFitness, false);
        config.SetupProperty(c => c.Generations, generations);
        config.SetupProperty(c => c.PopulationSize, populationSize);
        config.Setup(c => c.Mutation).Returns(mutation.Object);
        config.Setup(c => c.Crossover).Returns(crossover.Object);
        config.SetupProperty(c => c.MutationRate, mutationRate);
        config.SetupProperty(c => c.InitialElite, new List<ISolution>());

        // Selection delegates - simple implementations
        config.Setup(c => c.SelectAsParent).Returns(
            (List<ISolution> source) => source.OrderByDescending(s => s.Fitness).Take(source.Count / 2));
        config.Setup(c => c.SelectForMating).Returns(
            (List<ISolution> source) =>
            {
                var pairs = new List<Tuple<ISolution, ISolution>>();
                for (int i = 0; i < source.Count - 1; i += 2)
                    pairs.Add(Tuple.Create(source[i], source[i + 1]));
                return pairs.AsEnumerable();
            });
        config.Setup(c => c.SelectToSurvive).Returns(
            (List<ISolution> source) => source.OrderByDescending(s => s.Fitness).Take(source.Count / 2));
        config.Setup(c => c.SelectElite).Returns(
            (List<ISolution> gen, List<ISolution> elite) =>
                gen.OrderByDescending(s => s.Fitness).Take(2).AsEnumerable());
        config.Setup(c => c.SelectNewGeneration).Returns(
            (List<ISolution> children, List<ISolution> parents, List<ISolution> elite) =>
                children.Concat(parents).OrderByDescending(s => s.Fitness).Take(populationSize).AsEnumerable());

        config.Setup(c => c.CompareTo(It.IsAny<ISolution>())).Returns(0);
        config.Setup(c => c.Clone()).Returns(() => config.Object);
        return config.Object;
    }
}
