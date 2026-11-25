using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.RandomSearch;
using SimOpt.Simulation.Statistics;

namespace SimOpt.McpServer.Optimization
{
    /// <summary>
    /// SIM-69 (D6, D9): runs a search over a model version's decision variables and reports the best
    /// point. "grid" evaluates every point of a discrete space; "random" draws points from the seeded
    /// space; "auto" enumerates when the space fits the evaluation budget and draws at random otherwise.
    /// </summary>
    public static class Optimizer
    {
        public const int DefaultEvaluations = 50;
        public const int DefaultReplications = 5;
        public static readonly IReadOnlyList<string> Strategies = new[] { "auto", "grid", "random" };

        /// <exception cref="ArgumentException">The request cannot be searched; the message says why.</exception>
        public static OptimizationResult Run(string modelId, int version, TopologyDefinition topology,
            IReadOnlyList<DecisionVariable> variables, ObjectiveDefinition objective, double duration, double? warmup = null,
            int replicationsPerEval = DefaultReplications, int maxEvaluations = DefaultEvaluations, double? maxSeconds = null,
            string strategy = "auto", int? seed = null)
        {
            string chosen = (strategy ?? "auto").Trim().ToLowerInvariant();
            if (!Strategies.Contains(chosen))
                throw new ArgumentException($"strategy must be one of {string.Join(", ", Strategies)}, not \"{strategy}\"; " +
                                            "sa, ea and pso follow (SIM-68 step 4).");
            if (maxEvaluations < 1) throw new ArgumentException($"max_evaluations must be at least 1, not {maxEvaluations}.");
            if (maxSeconds is double s && !(s > 0d)) throw new ArgumentException($"max_seconds must be positive, not {s}.");

            double warm = warmup ?? duration / 10d;
            int baseSeed = seed ?? topology.Seed;
            var problem = new TopologyProblem(topology, variables, objective, duration, warm, replicationsPerEval, baseSeed, maxEvaluations);
            long? space = problem.SpaceSize;
            if (chosen == "auto") chosen = space is long n && n <= maxEvaluations ? "grid" : "random";
            if (chosen == "grid" && space == null)
                throw new ArgumentException("grid needs a discrete space: give every real variable a step, or use random.");
            if (chosen == "grid" && space > maxEvaluations)
                throw new ArgumentException($"The space has {space} points, more than max_evaluations {maxEvaluations}; " +
                                            "raise max_evaluations or use random.");

            if (maxSeconds is double seconds) problem.Deadline = DateTime.UtcNow.AddSeconds(seconds);
            if (chosen == "grid") SearchGrid(problem);
            else SearchRandom(problem, maxEvaluations, baseSeed);

            return Report(modelId, version, chosen, problem, topology, objective, duration, warm, replicationsPerEval, baseSeed);
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

        private static OptimizationResult Report(string modelId, int version, string strategy, TopologyProblem problem,
            TopologyDefinition topology, ObjectiveDefinition objective, double duration, double warmup, int replications, int seed)
        {
            List<CandidateEvaluation> ranked = problem.Evaluations.OrderByDescending(e => e.Fitness).ThenBy(e => e.Number).ToList();
            CandidateEvaluation best = ranked[0];
            var result = new OptimizationResult
            {
                ModelId = modelId,
                Version = version,
                Strategy = strategy,
                Evaluations = problem.Evaluations.Count,
                Space = problem.SpaceSize,
                Stopped = problem.StopReason ?? "done",
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
            string bounds = string.Join(", ", objective.Constraints.Select(c => $"{c.Metric} {c.Op} {Fmt(c.Value)}"));
            result.Summary = best.Feasible
                ? $"{at} is the best of {result.Evaluations} points evaluated ({strategy}" +
                  $"{(result.Stopped == "done" ? "" : $", stopped by {result.Stopped}")})" +
                  $"{(bounds.Length > 0 ? $", keeping {bounds} on its whole 95% interval" : "")}. " +
                  "Confirm it with run_experiment at more replications."
                : $"No point of the {result.Evaluations} evaluated keeps {bounds}; the nearest is {at}.";
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
