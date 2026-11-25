using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.Evolutionary;
using SimOpt.Optimization.Strategies.ParticleSwarm;
using SimOpt.Optimization.Strategies.RandomSearch;
using SimOpt.Optimization.Strategies.SimulatedAnnealing;
using SimOpt.Simulation.Statistics;

namespace SimOpt.McpServer.Optimization
{
    /// <summary>
    /// SIM-69 (D6, D9): runs a search over a model version's decision variables and reports the best
    /// point. "grid" evaluates every point of a discrete space; "random" draws points from the seeded
    /// space; "sa", "ea" and "pso" run simulated annealing, the evolutionary algorithm and the particle
    /// swarm of SimOpt.Optimization on the same problem, each until the budget is spent (SIM-68 step 4).
    /// "auto" enumerates when the space fits the evaluation budget and runs the EA otherwise: at the
    /// default budget it found a 170-point space's optimum 12 times in 15 against random search's 2, and
    /// had the best worst case on a continuous space; SA, planned as the default in D6, came last.
    /// </summary>
    public static class Optimizer
    {
        public const int DefaultEvaluations = 50;
        public const int DefaultReplications = 5;
        public static readonly IReadOnlyList<string> Strategies = new[] { "auto", "grid", "random", "sa", "ea", "pso" };

        /// <summary>
        /// Moves a search may propose per evaluation it is allowed. A point proposed again is answered from
        /// the cache and costs nothing, so a search that keeps returning to known points still ends.
        /// </summary>
        private const int ProposalsPerEvaluation = 20;

        /// <param name="seed">The replications' base seed, and the search's unless <paramref name="searchSeed"/> is given.</param>
        /// <param name="searchSeed">
        /// The search's own seed (start points, moves), so that searches can be compared under the same
        /// replications; the optimize tool passes only <paramref name="seed"/>.
        /// </param>
        /// <exception cref="ArgumentException">The request cannot be searched; the message says why.</exception>
        public static OptimizationResult Run(string modelId, int version, TopologyDefinition topology,
            IReadOnlyList<DecisionVariable> variables, ObjectiveDefinition objective, double duration, double? warmup = null,
            int? replicationsPerEval = null, int maxEvaluations = DefaultEvaluations, double? maxSeconds = null,
            string strategy = "auto", int? seed = null, int? searchSeed = null)
        {
            int replications = replicationsPerEval ?? DefaultReplications;
            string chosen = (strategy ?? "auto").Trim().ToLowerInvariant();
            if (!Strategies.Contains(chosen))
                throw new ArgumentException($"strategy must be one of {string.Join(", ", Strategies)}, not \"{strategy}\".");
            if (maxEvaluations < 1) throw new ArgumentException($"max_evaluations must be at least 1, not {maxEvaluations}.");
            if (maxSeconds is double s && !(s > 0d)) throw new ArgumentException($"max_seconds must be positive, not {s}.");

            double warm = warmup ?? duration / 10d;
            int baseSeed = seed ?? topology.Seed;
            var problem = new TopologyProblem(topology, variables, objective, duration, warm, replications, baseSeed, maxEvaluations);
            long? space = problem.SpaceSize;
            if (chosen == "auto") chosen = space is long n && n <= maxEvaluations ? "grid" : "ea";
            if (chosen == "grid" && space == null)
                throw new ArgumentException("grid needs a discrete space: give every real variable a step, or use random.");
            if (chosen == "grid" && space > maxEvaluations)
                throw new ArgumentException($"The space has {space} points, more than max_evaluations {maxEvaluations}; " +
                                            "raise max_evaluations or use random.");

            if (maxSeconds is double seconds) problem.Deadline = DateTime.UtcNow.AddSeconds(seconds);
            int proposals = maxEvaluations * ProposalsPerEvaluation;
            int search = searchSeed ?? baseSeed;
            switch (chosen)
            {
                case "grid": SearchGrid(problem); break;
                case "sa": Search(new AnnealingAlgorithm(), Annealing(proposals, search), problem); break;
                case "ea": Search(new EvolutionaryAlgorithm(), Evolution(proposals, search), problem); break;
                case "pso": Search(new ParticleSwarmOptimization(), Swarm(proposals, search), problem); break;
                default: SearchRandom(problem, maxEvaluations, search); break;
            }

            OptimizationResult result = Report(modelId, version, chosen, problem, topology, objective, duration, warm, replications, baseSeed);
            result.Assumed = ExperimentRunner.Assumptions(topology,
                ExperimentRunner.NotGiven(warmup, seed, ("replications_per_eval", replicationsPerEval, replications)),
                problem.Variables.Select(v => v.Path).ToList());
            return result;
        }

        private static void SearchGrid(TopologyProblem problem)
        {
            foreach (double[] point in problem.Grid())
            {
                var candidate = (TopologySolution)problem.GenerateCandidates(0, 1).Single();
                candidate.Parameters = point;
                problem.Evaluate(candidate);
                if (problem.StopReason != null) break;
            }
        }

        private static void SearchRandom(TopologyProblem problem, int maxEvaluations, int seed)
        {
            var search = new RandomStrategy();
            // a point drawn twice is answered from the cache and costs nothing, so allow many draws per evaluation
            search.Initialize(new SearchConfiguration { Seed = seed, NumberOfIterations = maxEvaluations * 20 });
            problem.BudgetExhausted += search.Stop;
            search.Solve(problem).ToList();
        }

        private static void Search(IStrategy search, IConfiguration configuration, TopologyProblem problem)
        {
            search.Initialize(configuration);
            problem.BudgetExhausted += search.Stop;
            search.Solve(problem).ToList();
        }

        /// <summary>
        /// Simulated annealing over <paramref name="proposals"/> moves (one Tweak each). The temperature is
        /// relative to the current point: a feasible point's fitness is the objective in its own unit, so a
        /// move worse by a tenth of it is accepted at odds of 1 in e at the start, falling geometrically to
        /// a thousandth of that by the end. An infeasible point's fitness differs from its neighbours' by
        /// relative violations, which need no scale; a move into infeasibility (about -1e12) is never taken.
        /// </summary>
        private static SimulatedAnnealingConfiguration Annealing(int proposals, int seed)
        {
            const double start = 0.1, end = start / 1000d;
            double cooling = Math.Pow(end / start, 1d / proposals);
            return new SimulatedAnnealingConfiguration
            {
                Seed = seed,
                NumberOfIterations = proposals,
                InitialTemperature = start,
                DecreaseTemperature = a => start * Math.Pow(cooling, a.Steps + 1) * Scale(a.CurrentCandidate.Fitness),
            };
        }

        private static double Scale(double fitness) =>
            fitness > TopologyProblem.Infeasible / 2d ? Math.Max(Math.Abs(fitness), 1e-9) : 1d;

        /// <summary>
        /// A (5 + 10) evolutionary algorithm: the five best breed ten children by uniform crossover, each
        /// mutated by one Tweak, and the next generation is the fifteen of parents and children.
        /// </summary>
        private static EvolutionaryAlgorithmConfiguration Evolution(int proposals, int seed)
        {
            const int mu = 5, lambda = 10;
            return new EvolutionaryAlgorithmConfiguration(seed, Math.Max(1, proposals / lambda), mu, lambda, keepParentsAlive: true);
        }

        /// <summary>A swarm of ten with the constriction defaults of <see cref="ParticleSwarmConfiguration"/>.</summary>
        private static ParticleSwarmConfiguration Swarm(int proposals, int seed)
        {
            var configuration = new ParticleSwarmConfiguration { Seed = seed };
            configuration.NumberOfIterations = Math.Max(1, proposals / configuration.SwarmSize);
            return configuration;
        }

        private static OptimizationResult Report(string modelId, int version, string strategy, TopologyProblem problem,
            TopologyDefinition topology, ObjectiveDefinition objective, double duration, double warmup, int replications, int seed)
        {
            List<CandidateEvaluation> ranked = problem.Evaluations.OrderByDescending(e => e.Fitness).ThenBy(e => e.Number).ToList();
            string bounds = string.Join(", ", objective.Constraints.Select(c => $"{c.Subject} {c.Op} {Fmt(c.Value)}"));
            if (ranked.Count == 0)
                throw new ArgumentException($"No point the search proposed keeps {bounds}; {problem.Skipped} were refused without a run. " +
                                            "Widen the variables' bounds or the constraints on them.");
            CandidateEvaluation best = ranked[0];
            var result = new OptimizationResult
            {
                ModelId = modelId,
                Version = version,
                ModelSha256 = ModelDocument.Sha256(topology),
                Strategy = strategy,
                Evaluations = problem.Evaluations.Count,
                Space = problem.SpaceSize,
                Stopped = problem.StopReason ?? "done",
                Skipped = problem.Skipped > 0 ? problem.Skipped : null,
                Best = Point(problem, objective, best, detailed: true),
                Top = ranked.Take(5).Select(e => Point(problem, objective, e, detailed: false)).ToList(),
                Duration = duration,
                Warmup = warmup,
                ReplicationsPerEval = replications,
                Seed = seed,
                Tool = ExperimentRunner.ToolVersion,
            };
            result.Best.Saturated = Saturated(problem, topology, best);

            string at = string.Join(", ", result.Best.Point.Select(p => $"{p.Key} = {Fmt(p.Value)}"));
            result.Summary = best.Feasible
                ? $"{at} is the best of {result.Evaluations} points evaluated ({strategy}" +
                  $"{(result.Stopped == "done" ? "" : $", stopped by {result.Stopped}")})" +
                  $"{(bounds.Length > 0 ? $", keeping {bounds} on its whole 95% interval" : "")}. " +
                  "Confirm it with run_experiment at more replications."
                : $"No point of the {result.Evaluations} evaluated keeps {bounds}; the nearest is {at}.";
            if (problem.Skipped > 0)
                result.Summary += $" {problem.Skipped} points broke a constraint on the variables and were refused without a run.";
            if (result.Best.Saturated != null)
                result.Summary += $" At this point {string.Join(", ", result.Best.Saturated)} is offered a load of 1 or more, " +
                                  "so its figures grow with the run length.";
            return result;
        }

        private static OptimizedPoint Point(TopologyProblem problem, ObjectiveDefinition objective, CandidateEvaluation e, bool detailed)
        {
            var point = new OptimizedPoint
            {
                Point = problem.Variables.Select((v, i) => (v.Path, e.Point[i])).ToDictionary(p => p.Path, p => p.Item2),
                Feasible = e.Feasible,
                Error = e.Error,
            };
            int variable = problem.Variables.ToList().FindIndex(v => v.Path == objective.Metric);
            point.Objective = variable >= 0 ? e.Point[variable]
                : e.Metrics.TryGetValue(objective.Metric, out MetricSummary? m) ? ExperimentRunner.Round(m.Mean) : null;
            if (!detailed) return point;
            point.Metrics = e.Metrics.ToDictionary(m => m.Key, m => Estimate(m.Value));
            point.Violations = e.Violations.Count > 0 ? e.Violations.ToList() : null;
            return point;
        }

        private static MetricEstimate Estimate(MetricSummary m) =>
            !m.HasData || double.IsNaN(m.Mean)
                ? new MetricEstimate(null, null)
                : new MetricEstimate(ExperimentRunner.Round(m.Mean), m.HalfWidth is double h
                    ? new[] { ExperimentRunner.Round(m.Mean - h)!.Value, ExperimentRunner.Round(m.Mean + h)!.Value }
                    : null);

        /// <summary>The stations offered a load of 1 or more at the point, from the patched topology.</summary>
        private static List<string>? Saturated(TopologyProblem problem, TopologyDefinition topology, CandidateEvaluation e)
        {
            if (e.Error != null) return null;
            var patch = problem.Variables.Select((v, i) => new PatchOperation
            {
                Op = "set",
                Path = v.Path,
                Value = System.Text.Json.JsonDocument.Parse(e.Point[i].ToString("R", CultureInfo.InvariantCulture)).RootElement,
            }).ToList();
            TopologyDefinition? patched = TopologyPatcher.Apply(topology, patch, new ValidationReport(), new List<string>());
            if (patched == null) return null;
            List<string> saturated = OfferedLoad.Compute(patched).Where(l => l.Load >= 1d).Select(l => l.Station).ToList();
            return saturated.Count > 0 ? saturated : null;
        }

        private static string Fmt(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    }

    /// <summary>The settings <see cref="RandomStrategy"/> reads: a seed and how many points to draw.</summary>
    public sealed class SearchConfiguration : IConfiguration
    {
        public int Seed { get; set; }
        public int NumberOfIterations { get; set; }
        public int NumberOfEvaluations { get; set; }
        public double Fitness { get; set; }
        public bool HasFitness { get; set; }
        public int CompareTo(ISolution? other) => Fitness.CompareTo(other?.Fitness ?? double.NegativeInfinity);
        public object Clone() => MemberwiseClone();
    }
}
