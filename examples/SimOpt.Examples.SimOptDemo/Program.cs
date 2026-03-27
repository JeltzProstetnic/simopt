using System;
using SimOpt.Logging;
using SimOpt.Logging.Loggers;
using SimOpt.Optimization.Strategies.Evolutionary;
using SimOpt.Examples.SimOptDemo.Optimizer;

namespace SimOpt.Examples.SimOptDemo
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Logger.Add(new ConsoleLogger());
            Logger.Log("SimOpt.Examples.SimOptDemo", "Starting SimOpt Demo...");
            Logger.Dispatch();

            int generations = 10;
            int queueSize = 50;

            Console.WriteLine($"Running evolutionary optimization: {generations} generations, queue size {queueSize}");

            var sim = new Model.Simulation(logEvents: false, seed: 123, queueSize: queueSize);
            var problem = new Problem(sim);

            var eac = new EvolutionaryAlgorithmConfiguration(123, generations, 5, 10);
            var ea = new EvolutionaryAlgorithm();
            ea.Initialize(eac);

            ea.BestSolutionChanged += (s, e) =>
            {
                Console.WriteLine($"New best solution: fitness={ea.BestSolution.Fitness:F4}, config={e.NewValue}");
            };

            ea.Solve(problem);

            Console.WriteLine($"Optimization complete after {ea.ProcessedGenerations} generations.");
            if (ea.BestSolution != null)
                Console.WriteLine($"Best fitness: {ea.BestSolution.Fitness:F4}");

            Logger.Log("Shutting down.");
            Logger.Shutdown(false);
        }
    }
}
