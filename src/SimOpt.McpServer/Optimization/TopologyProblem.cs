using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Optimization.Interfaces;
using SimOpt.Simulation.Statistics;

namespace SimOpt.McpServer.Optimization
{
    /// <summary>One evaluated point of a <see cref="TopologyProblem"/>.</summary>
    /// <param name="Number">1 for the first point evaluated, and so on.</param>
    /// <param name="Point">The quantised point, one value per decision variable.</param>
    /// <param name="Violations">Each constraint the point breaks, in words; empty when feasible.</param>
    /// <param name="Error">Why the patched model could not be built, or null.</param>
    public sealed record CandidateEvaluation(int Number, IReadOnlyList<double> Point, double Fitness, bool Feasible,
                                             IReadOnlyDictionary<string, MetricSummary> Metrics,
                                             IReadOnlyList<string> Violations, string? Error);

    /// <summary>
    /// SIM-68: any MCP-built model as an <see cref="IProblem"/> for the strategies of SimOpt.Optimization.
    /// The genome is a vector of decision variables, each a patch path (D1); a candidate is quantised and
    /// applied as a patch (D2), replicated at one base seed so that candidates differ only where their
    /// patch differs (D3), scored for maximisation with constraint breaches ranked below every feasible
    /// point (D4), and cached by its quantised point (D5).
    /// </summary>
    public sealed class TopologyProblem : IProblem
    {
        /// <summary>Fitness of a point that breaks a constraint, before its violation is subtracted.</summary>
        public const double Infeasible = -1e12;

        /// <summary>Fitness of a point whose patched model does not build, or that the budget left unevaluated.</summary>
        public const double Unusable = -1e15;

        private static readonly string[] Ops = { "<", "<=", ">", ">=" };

        private readonly TopologyDefinition topology;
        private readonly IReadOnlyList<DecisionVariable> variables;
        private readonly ObjectiveDefinition objective;
        private readonly double duration, warmup;
        private readonly int replications, replicationSeed;
        private readonly int? budget;
        private readonly Dictionary<string, CandidateEvaluation> cache = new();
        private readonly List<CandidateEvaluation> evaluations = new();
        private bool exhausted;

        /// <param name="duration">Simulated time per replication, warm-up included.</param>
        /// <param name="budget">Most distinct points to evaluate; unlimited when null.</param>
        /// <exception cref="ArgumentException">The definition cannot be searched; the message lists every reason.</exception>
        public TopologyProblem(TopologyDefinition topology, IReadOnlyList<DecisionVariable> variables, ObjectiveDefinition objective,
                               double duration, double warmup, int replications, int replicationSeed, int? budget = null)
        {
            this.topology = topology;
            this.variables = variables.Select(Normalised).ToList();
            this.objective = objective;
            this.duration = duration;
            this.warmup = warmup;
            this.replications = replications;
            this.replicationSeed = replicationSeed;
            this.budget = budget;

            List<string> problems = Check();
            if (problems.Count > 0) throw new ArgumentException(string.Join(" ", problems));
        }

        /// <summary>Raised once, when the budget's last point has been evaluated.</summary>
        public event Action? BudgetExhausted;

        public IReadOnlyList<DecisionVariable> Variables => variables;

        /// <summary>Every point evaluated, in order; a cached answer adds none.</summary>
        public IReadOnlyList<CandidateEvaluation> Evaluations => evaluations;

        /// <summary>Unknown in general; a strategy that stops at it never does.</summary>
        public double OptimumFitness => double.PositiveInfinity;

        public bool IsValid(ISolution solutionCandidate) =>
            solutionCandidate is TopologySolution s && s.Parameters.Length == variables.Count;

        /// <summary>D2: the point as it is applied: integers rounded, steps snapped, bounds kept.</summary>
        public double[] Quantise(IReadOnlyList<double> point)
        {
            var q = new double[variables.Count];
            for (int i = 0; i < q.Length; i++)
            {
                DecisionVariable v = variables[i];
                double x = point[i];
                if (v.Step is double step) x = v.Min + Math.Round((x - v.Min) / step, MidpointRounding.AwayFromZero) * step;
                if (v.Type == "integer") x = Math.Round(x, MidpointRounding.AwayFromZero);
                q[i] = Math.Clamp(x, v.Min, v.Max);
            }
            return q;
        }

        /// <summary>
        /// SIM-68 step 2 (D6): how many distinct points the quantised space holds, or null when a real
        /// variable without a step makes it continuous.
        /// </summary>
        public long? SpaceSize
        {
            get
            {
                long size = 1;
                foreach (DecisionVariable v in variables)
                {
                    long? count = Count(v);
                    if (count == null) return null;
                    size = size > long.MaxValue / Math.Max(count.Value, 1) ? long.MaxValue : size * count.Value;
                }
                return size;
            }
        }

        /// <summary>D6: every point of a discrete space, the first variable changing slowest.</summary>
        public IEnumerable<double[]> Grid()
        {
            if (SpaceSize == null) throw new InvalidOperationException("A space with a continuous variable cannot be enumerated.");
            List<double[]> axes = variables.Select(v => Enumerable.Range(0, (int)Count(v)!.Value)
                                                                  .Select(k => Quantise(v, v.Min + k * (v.Step ?? 1d))).Distinct().ToArray())
                                           .ToList();
            var index = new int[axes.Count];
            while (true)
            {
                yield return axes.Select((axis, i) => axis[index[i]]).ToArray();
                int d = axes.Count - 1;
                while (d >= 0 && ++index[d] == axes[d].Length) index[d--] = 0;
                if (d < 0) yield break;
            }
        }

        private static long? Count(DecisionVariable v)
        {
            if (v.Step is double step) return (long)Math.Floor((v.Max - v.Min) / step + 1e-9) + 1;
            if (v.Type == "integer") return (long)(v.Max - v.Min) + 1;
            return v.Min == v.Max ? 1 : null;
        }

        private static double Quantise(DecisionVariable v, double x)
        {
            if (v.Type == "integer") x = Math.Round(x, MidpointRounding.AwayFromZero);
            return Math.Clamp(x, v.Min, v.Max);
        }

        public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
        {
            var draw = new TopologySolution(variables, Array.Empty<double>(), unchecked((ulong)seed));
            for (int c = 0; c < count; c++)
            {
                double[] point = variables.Select(v => v.Min + draw.Uniform() * (v.Max - v.Min)).ToArray();
                yield return new TopologySolution(variables, point, draw.Next());
            }
        }

        public bool Evaluate(ISolution solutionCandidate)
        {
            if (solutionCandidate is not TopologySolution solution || solution.Parameters.Length != variables.Count)
                throw new ArgumentException("This problem evaluates its own TopologySolution candidates only.");

            double[] point = Quantise(solution.Parameters);
            string key = string.Join("|", point.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
            if (!cache.TryGetValue(key, out CandidateEvaluation? evaluation))
            {
                if (budget is int most && evaluations.Count >= most)
                {
                    solution.Fitness = Unusable;
                    solution.HasFitness = true;
                    return false;
                }
                evaluation = Run(point);
                cache[key] = evaluation;
                evaluations.Add(evaluation);
                if (!exhausted && budget is int limit && evaluations.Count >= limit)
                {
                    exhausted = true;
                    BudgetExhausted?.Invoke();
                }
            }
            solution.Fitness = evaluation.Fitness;
            solution.HasFitness = true;
            return true;
        }

        // ── evaluation ──────────────────────────────────────────────────────

        private CandidateEvaluation Run(double[] point)
        {
            int number = evaluations.Count + 1;
            var report = new ValidationReport();
            TopologyDefinition? patched = TopologyPatcher.Apply(topology, Patch(point), report, new List<string>());
            if (patched != null && patched.Metrics.Count == 0) ExperimentRunner.AddDefaultMetrics(patched);
            ActiveModel? model = patched == null ? null : ModelRegistry.TryBuild(patched, report);
            if (model == null)
                return new CandidateEvaluation(number, point, Unusable, false, new Dictionary<string, MetricSummary>(),
                    Array.Empty<string>(), string.Join(" ", report.Errors.Select(e => e.Line)));

            ReplicationResult result = new ReplicationRunner(model.Model, duration, warmup).Run(replications, replicationSeed);
            var metrics = model.Metrics.ToDictionary(m => m.Key, m => result[m.Value]);

            var violations = new List<string>();
            double violation = 0d;
            // D4, measured: a bound is kept only when the metric's whole 95 % interval keeps it. On the mean,
            // five replications passed 6 exam rooms (wait 12.3 ± 8.0 against < 15) that twenty failed
            // (19.7 ± 6.6), so the answer flipped with the noise; the interval does not.
            foreach (ConstraintDefinition c in objective.Constraints)
            {
                MetricSummary m = metrics[c.Metric];
                if (double.IsNaN(m.Mean))
                {
                    violations.Add($"{c.Metric} has no data");
                    violation += 1d;
                    continue;
                }
                bool below = c.Op.StartsWith("<");
                double edge = m.HalfWidth is double h ? (below ? m.Mean + h : m.Mean - h) : m.Mean;
                double excess = below ? edge - c.Value : c.Value - edge;
                bool broken = c.Op.Length == 1 ? excess >= 0d : excess > 0d;
                if (!broken) continue;
                string interval = m.HalfWidth.HasValue ? $" (95 % {(below ? "up" : "down")} to {Fmt(edge)})" : "";
                violations.Add($"{c.Metric} {Fmt(m.Mean)}{interval} is not {c.Op} {Fmt(c.Value)}");
                violation += Math.Max(excess, 0d) / Math.Max(Math.Abs(c.Value), 1d) + 1e-9;
            }

            int variable = variables.ToList().FindIndex(v => v.Path == objective.Metric);
            double value = variable >= 0 ? point[variable] : metrics[objective.Metric].Mean;
            if (double.IsNaN(value))
                return new CandidateEvaluation(number, point, Unusable, false, metrics, violations.Append($"{objective.Metric} has no data").ToList(), null);
            double fitness = violations.Count > 0 ? Infeasible - violation : objective.Direction == "max" ? value : -value;
            return new CandidateEvaluation(number, point, fitness, violations.Count == 0, metrics, violations, null);
        }

        /// <summary>D1: the point as set operations, one per decision variable.</summary>
        private List<PatchOperation> Patch(IReadOnlyList<double> point) =>
            variables.Select((v, i) => new PatchOperation
            {
                Op = "set",
                Path = v.Path,
                Value = JsonDocument.Parse(v.Type == "integer"
                    ? ((long)point[i]).ToString(CultureInfo.InvariantCulture)
                    : point[i].ToString("R", CultureInfo.InvariantCulture)).RootElement,
            }).ToList();

        // ── checking the definition ─────────────────────────────────────────

        private static DecisionVariable Normalised(DecisionVariable v) => new DecisionVariable
        {
            Path = (v.Path ?? string.Empty).Trim(),
            Type = (v.Type ?? string.Empty).Trim().ToLowerInvariant(),
            Min = v.Min,
            Max = v.Max,
            Step = v.Step,
        };

        private List<string> Check()
        {
            var problems = new List<string>();
            var report = new ValidationReport();
            if (ModelRegistry.TryBuild(topology, report) == null)
                problems.Add($"The model does not build: {string.Join(" ", report.Errors.Select(e => e.Line))}");
            if (!(duration > 0d) || double.IsInfinity(duration)) problems.Add($"duration must be a positive number, not {duration}.");
            if (!(warmup >= 0d) || warmup >= duration) problems.Add($"warmup must be at least 0 and shorter than the duration, not {warmup}.");
            if (replications < 1 || replications > ExperimentRunner.MaxReplications)
                problems.Add($"replications per evaluation must be from 1 to {ExperimentRunner.MaxReplications}, not {replications}.");
            if (budget is int b && b < 1) problems.Add($"the evaluation budget must be at least 1, not {b}.");

            if (variables.Count == 0) problems.Add("Give at least one decision variable.");
            foreach (IGrouping<string, DecisionVariable> twice in variables.GroupBy(v => v.Path).Where(g => g.Count() > 1))
                problems.Add($"Variable '{twice.Key}' is given {twice.Count()} times.");
            foreach (DecisionVariable v in variables) problems.AddRange(CheckVariable(v));

            TopologyDefinition measured = TopologyPatcher.Copy(topology);
            if (measured.Metrics.Count == 0) ExperimentRunner.AddDefaultMetrics(measured);
            var metricIds = measured.Metrics.Select(m => m.Id).ToHashSet();
            string known = $"Its metrics are {string.Join(", ", metricIds.Select(m => $"'{m}'"))}.";
            if (!metricIds.Contains(objective.Metric ?? "") && variables.All(v => v.Path != objective.Metric))
                problems.Add($"The objective '{objective.Metric}' is neither a metric nor a decision variable. {known}");
            if (objective.Direction != "min" && objective.Direction != "max")
                problems.Add($"The objective's direction must be \"min\" or \"max\", not \"{objective.Direction}\".");
            foreach (ConstraintDefinition c in objective.Constraints)
            {
                if (!metricIds.Contains(c.Metric ?? "")) problems.Add($"The constraint on '{c.Metric}' names no metric. {known}");
                if (!Ops.Contains(c.Op)) problems.Add($"A constraint's op must be one of {string.Join(" ", Ops)}, not \"{c.Op}\".");
            }
            return problems;
        }

        private IEnumerable<string> CheckVariable(DecisionVariable v)
        {
            string name = $"Variable '{v.Path}'";
            if (v.Type != "integer" && v.Type != "real") yield return $"{name}: type must be \"integer\" or \"real\", not \"{v.Type}\".";
            if (!double.IsFinite(v.Min) || !double.IsFinite(v.Max) || v.Min > v.Max)
                yield return $"{name}: min {Fmt(v.Min)} and max {Fmt(v.Max)} must be numbers with min not above max.";
            if (v.Type == "integer" && (v.Min != Math.Floor(v.Min) || v.Max != Math.Floor(v.Max)))
                yield return $"{name}: an integer variable's min and max are whole numbers.";
            if (v.Step is double step && !(step > 0d)) yield return $"{name}: step must be positive, not {Fmt(step)}.";

            var report = new ValidationReport();
            TopologyPatcher.Apply(topology, new[]
            {
                new PatchOperation { Op = "set", Path = v.Path, Value = JsonDocument.Parse(v.Min.ToString("R", CultureInfo.InvariantCulture)).RootElement },
            }, report, new List<string>());
            foreach (ValidationIssue issue in report.Errors) yield return $"{name}: {issue.Line}";
        }

        private static string Fmt(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    }
}
