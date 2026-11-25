using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SimOpt.McpServer.Models;
using SimOpt.Simulation.Statistics;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-66 step 6 (D10-D12): runs a model version and its scenarios with replications and reports
    /// each metric with a confidence interval, each scenario's paired difference from the baseline,
    /// and flags for what went wrong, compactly enough for an agent's context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every scenario is built fresh from the version's topology plus its patch, so nothing a tool did
    /// to the registered model leaks in. All scenarios run at the same base seed. Node streams are
    /// keyed by node id (SIM-62/89), so a node a patch leaves alone draws the same numbers in every
    /// scenario: common random numbers, which is what makes the replication-by-replication difference
    /// a sharp comparison rather than the difference of two noisy means (UN-013).
    /// </para>
    /// <para>
    /// A model that declares no metric is measured by a default set: cycle time over all sinks,
    /// throughput per sink, utilization per station and wait per queue (UN-011).
    /// </para>
    /// </remarks>
    public static class ExperimentRunner
    {
        public const double Confidence = 0.95;
        public const int MaxReplications = 1000;
        public const int DefaultReplications = 10;
        public const string Baseline = "baseline";

        /// <param name="duration">Simulated time per replication, warm-up included.</param>
        /// <param name="replications">Per scenario; <see cref="DefaultReplications"/> when not given.</param>
        /// <param name="warmup">Excluded from every statistic; a tenth of the duration when not given.</param>
        /// <param name="seed">The base seed of every scenario; the model's seed when not given.</param>
        /// <exception cref="ArgumentException">The request cannot be run; the message says why.</exception>
        public static ExperimentResult Run(string modelId, int version, TopologyDefinition topology, double duration,
            int? replications = null, double? warmup = null, IReadOnlyList<string>? metrics = null,
            IReadOnlyList<ScenarioDefinition>? scenarios = null, int? seed = null)
        {
            double warm = warmup ?? duration / 10d;
            int reps = replications ?? DefaultReplications;
            if (!(duration > 0d) || double.IsInfinity(duration))
                throw new ArgumentException($"duration must be a positive number of time units, not {duration}.");
            if (!(warm >= 0d) || warm >= duration)
                throw new ArgumentException($"warmup must be at least 0 and shorter than the duration ({Fmt(duration)}), not {warm}.");
            if (reps < 1 || reps > MaxReplications)
                throw new ArgumentException($"replications must be from 1 to {MaxReplications}, not {reps}.");
            scenarios ??= Array.Empty<ScenarioDefinition>();
            var names = scenarios.Select((s, i) => string.IsNullOrWhiteSpace(s?.Name) ? $"scenario {i + 1}" : s.Name.Trim()).ToList();
            string? twice = names.Prepend(Baseline).GroupBy(n => n).FirstOrDefault(g => g.Count() > 1)?.Key;
            if (twice != null)
                throw new ArgumentException($"scenario name '{twice}' is used twice; \"{Baseline}\" is the unpatched version.");

            bool defaults = topology.Metrics.Count == 0;
            if (metrics is { Count: > 0 })
            {
                TopologyDefinition measured = TopologyPatcher.Copy(topology);
                if (defaults) AddDefaultMetrics(measured);
                List<string> known = measured.Metrics.Select(m => m.Id).ToList();
                List<string> unknown = metrics.Where(id => !known.Contains(id)).ToList();
                if (unknown.Count > 0)
                    throw new ArgumentException($"no metric {string.Join(", ", unknown.Select(u => $"'{u}'"))} in this model; " +
                                                $"its metrics are {string.Join(", ", known.Select(k => $"'{k}'"))}.");
            }

            var result = new ExperimentResult
            {
                ModelId = modelId,
                Version = version,
                ModelSha256 = ModelDocument.Sha256(topology),
                Duration = duration,
                Warmup = warm,
                Replications = reps,
                Seed = seed ?? topology.Seed,
                Confidence = Confidence,
                Tool = ToolVersion,
                Assumed = Assumptions(topology, NotGiven(warmup, seed, ("replications", replications, reps))),
            };
            result.Scenarios.Add(RunScenario(Baseline, topology, Array.Empty<PatchOperation>(), defaults, result));
            for (int i = 0; i < scenarios.Count; i++)
            {
                List<PatchOperation> patch = scenarios[i]?.Patch ?? new();
                ScenarioResult scenario = RunScenario(names[i], topology, patch, defaults, result);
                scenario.Patch = patch;
                result.Scenarios.Add(scenario);
            }

            if (metrics is { Count: > 0 }) Choose(result, metrics);
            Compare(result);
            result.Summary = Summarise(result);
            return result;
        }

        private static ScenarioResult RunScenario(string name, TopologyDefinition topology, IReadOnlyList<PatchOperation> patch,
                                                  bool defaults, ExperimentResult run)
        {
            var scenario = new ScenarioResult { Name = name };
            var faults = new ValidationReport();
            TopologyDefinition? t = patch.Count == 0
                ? TopologyPatcher.Copy(topology)
                : TopologyPatcher.Apply(topology, patch, faults, new List<string>());
            if (t != null && defaults && t.Metrics.Count == 0) AddDefaultMetrics(t);
            ActiveModel? model = t == null ? null : ModelRegistry.TryBuild(t, faults);
            if (model == null)
            {
                scenario.Errors = faults.Errors.Select(e => e.Line).ToList();
                return scenario;
            }

            // what no collector holds, read at the end of each replication (a reset wipes it)
            var refusedNow = new Dictionary<string, int>();
            foreach (var (id, queue) in model.Buffers)
                queue.NotifyItemNotAccepted = _ => refusedNow[id] = refusedNow.GetValueOrDefault(id) + 1;
            var refused = new Dictionary<string, double>();
            int finished = 0;
            void AfterReplication(int _)
            {
                foreach (string id in refusedNow.Keys.ToList())
                {
                    refused[id] = refused.GetValueOrDefault(id) + refusedNow[id];
                    refusedNow[id] = 0;
                }
                foreach (var (id, delay) in model.Delays) refused[id] = refused.GetValueOrDefault(id) + delay.RefusedCount;
                foreach (var (id, router) in model.Routers) refused[id] = refused.GetValueOrDefault(id) + router.RefusedCount;
                finished += model.Sinks.Values.Sum(s => s.Count);
            }

            ReplicationResult replicated = new ReplicationRunner(model.Model, run.Duration, run.Warmup)
                .Run(run.Replications, run.Seed, Confidence, AfterReplication);

            scenario.Metrics = new Dictionary<string, MetricEstimate>();
            var noData = new List<string>();
            foreach (var (id, key) in model.Metrics)
            {
                MetricSummary summary = replicated[key];
                scenario.Replicates[id] = summary.ReplicateEstimates;
                if (!summary.HasData || double.IsNaN(summary.Mean))
                {
                    scenario.Metrics[id] = new MetricEstimate(null, null);
                    noData.Add(id);
                    continue;
                }
                scenario.Metrics[id] = new MetricEstimate(Round(summary.Mean),
                    summary.HalfWidth is double h ? new[] { Round(summary.Mean - h)!.Value, Round(summary.Mean + h)!.Value } : null);
            }

            var flags = new ScenarioFlags
            {
                Saturated = Nonempty(faults.Warnings.Where(w => w.Code == "SATURATED" && w.Node != null).Select(w => w.Node!).ToList()),
                Refused = refused.Any(r => r.Value > 0)
                    ? refused.Where(r => r.Value > 0).ToDictionary(r => r.Key, r => Round(r.Value / run.Replications)!.Value)
                    : null,
                NothingFinished = model.Sinks.Count > 0 && finished == 0 ? true : null,
                NoData = Nonempty(noData),
            };
            scenario.Flags = flags.Any ? flags : null;
            return scenario;
        }

        /// <summary>Keeps only the metrics asked for, in the order asked.</summary>
        private static void Choose(ExperimentResult result, IReadOnlyList<string> metrics)
        {
            foreach (ScenarioResult s in result.Scenarios.Where(s => s.Metrics != null))
            {
                s.Metrics = metrics.Where(s.Metrics!.ContainsKey).ToDictionary(id => id, id => s.Metrics![id]);
                foreach (string id in s.Replicates.Keys.Except(metrics).ToList()) s.Replicates.Remove(id);
                if (s.Flags?.NoData != null)
                {
                    s.Flags.NoData = Nonempty(s.Flags.NoData.Where(metrics.Contains).ToList());
                    if (!s.Flags.Any) s.Flags = null;
                }
            }
        }

        /// <summary>D11: each scenario against the baseline, replication by replication.</summary>
        private static void Compare(ExperimentResult result)
        {
            ScenarioResult baseline = result.Scenarios[0];
            foreach (ScenarioResult s in result.Scenarios.Skip(1).Where(s => s.Metrics != null))
            {
                s.VsBaseline = new Dictionary<string, Difference>();
                foreach (string id in s.Metrics!.Keys.Where(baseline.Replicates.ContainsKey))
                {
                    double[] a = baseline.Replicates[id], b = s.Replicates[id];
                    List<double> d = Enumerable.Range(0, Math.Min(a.Length, b.Length))
                        .Where(i => !double.IsNaN(a[i]) && !double.IsNaN(b[i])).Select(i => b[i] - a[i]).ToList();
                    if (d.Count == 0) continue;
                    double mean = d.Average();
                    double? h = ReplicationRunner.ConfidenceHalfWidth(d, Confidence);
                    s.VsBaseline[id] = new Difference(Round(mean)!.Value,
                        h is double hw ? new[] { Round(mean - hw)!.Value, Round(mean + hw)!.Value } : null,
                        h.HasValue && Math.Abs(mean) > h.Value);
                }
            }
        }

        private static string Summarise(ExperimentResult result)
        {
            var parts = new List<string>
            {
                $"{result.Replications} replications of {Fmt(result.Duration)} time units ({Fmt(result.Warmup)} warm-up) per scenario",
            };
            foreach (ScenarioResult s in result.Scenarios)
            {
                if (s.Errors != null) { parts.Add($"{s.Name} not run ({s.Errors.Count} errors)"); continue; }
                if (s.VsBaseline != null)
                {
                    var significant = s.VsBaseline.Where(d => d.Value.Significant).ToList();
                    parts.Add(significant.Count == 0
                        ? $"{s.Name}: no significant difference from baseline"
                        : $"{s.Name} differs significantly in " +
                          string.Join(", ", significant.Select(d => $"{d.Key} ({(d.Value.Diff > 0 ? "+" : "")}{Fmt(d.Value.Diff)})")));
                }
                if (s.Flags?.Saturated != null) parts.Add($"{s.Name}: saturated {string.Join(", ", s.Flags.Saturated)}");
                if (s.Flags?.Refused != null) parts.Add($"{s.Name}: items refused at {string.Join(", ", s.Flags.Refused.Keys)}");
                if (s.Flags?.NothingFinished == true) parts.Add($"{s.Name}: nothing finished");
            }
            return string.Join("; ", parts) + ".";
        }

        /// <summary>D10: what a model that declares no metric is measured by.</summary>
        public static void AddDefaultMetrics(TopologyDefinition t)
        {
            List<string> Of(string type) => t.Nodes.Where(n => SchemaCatalog.Canonical(n.Type) == type).Select(n => n.Id).ToList();
            List<string> sinks = Of("sink");
            if (sinks.Count > 0) t.Metrics.Add(new MetricDefinition { Id = Instrumentation.CycleTime, Kind = Instrumentation.CycleTime });
            foreach (string sink in sinks)
                t.Metrics.Add(new MetricDefinition { Id = $"throughput_{sink}", Kind = Instrumentation.Throughput, Node = sink });
            foreach (string station in Of("station"))
                t.Metrics.Add(new MetricDefinition { Id = $"util_{station}", Kind = Instrumentation.Utilization, Node = station });
            foreach (string queue in Of("queue"))
                t.Metrics.Add(new MetricDefinition { Id = $"wait_{queue}", Kind = Instrumentation.WaitTime, Node = queue });
            // SIM-126: a resource is measured as a station and its queue are, a stock by its level and its empty time
            foreach (string resource in Of("resource"))
            {
                t.Metrics.Add(new MetricDefinition { Id = $"util_{resource}", Kind = Instrumentation.Utilization, Node = resource });
                t.Metrics.Add(new MetricDefinition { Id = $"wait_{resource}", Kind = Instrumentation.WaitTime, Node = resource });
            }
            foreach (string stock in Of("stock"))
            {
                t.Metrics.Add(new MetricDefinition { Id = $"level_{stock}", Kind = SchemaCatalog.LevelKind, Node = stock });
                t.Metrics.Add(new MetricDefinition { Id = $"empty_{stock}", Kind = SchemaCatalog.EmptyShareKind, Node = stock });
            }
        }

        /// <summary>
        /// SIM-86 (UN-035): the call arguments a run used without being given them, with the value used:
        /// the warm-up, the seed and the replication count named in <paramref name="replications"/>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> NotGiven(double? warmup, int? seed,
                                                                          (string Name, int? Given, int Used) replications)
        {
            if (warmup == null) yield return new("warmup", "a tenth of the duration");
            if (seed == null) yield return new("seed", "the model's");
            if (replications.Given == null)
                yield return new(replications.Name, replications.Used.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// SIM-86 (UN-035): what a run assumed rather than was told: the call arguments not given, the default
        /// metric set when the model declares none, and every node parameter left to the catalogue, by patch
        /// path. Paths in <paramref name="searched"/> are decision variables, varied rather than assumed. Null
        /// when nothing was assumed.
        /// </summary>
        public static Dictionary<string, string>? Assumptions(TopologyDefinition topology,
            IEnumerable<KeyValuePair<string, string>> call, IReadOnlyCollection<string>? searched = null)
        {
            var assumed = new Dictionary<string, string>();
            foreach (var (name, value) in call) assumed[name] = value;
            if (topology.Metrics.Count == 0) assumed["metrics"] = "the default set; the model declares none";
            foreach (var (path, value) in ModelDocument.Defaults(topology))
                if (searched == null || !searched.Contains(path)) assumed[path] = value;
            return assumed.Count == 0 ? null : assumed;
        }

        /// <summary>The tool's name and version, with the commit it was built from shortened (UN-034).</summary>
        public static string ToolVersion { get; } = ReadToolVersion();

        private static string ReadToolVersion()
        {
            Assembly assembly = typeof(ExperimentRunner).Assembly;
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                             ?? assembly.GetName().Version?.ToString() ?? "unknown";
            int plus = version.IndexOf('+');
            if (plus >= 0 && version.Length > plus + 8) version = version.Substring(0, plus + 8);
            return $"{assembly.GetName().Name} {version}";
        }

        private static List<string>? Nonempty(List<string> list) => list.Count == 0 ? null : list;

        /// <summary>Four significant digits: enough to read an interval, and a third of the tokens of a full double.</summary>
        internal static double? Round(double value) =>
            double.IsNaN(value) || double.IsInfinity(value)
                ? null
                : double.Parse(value.ToString("G4", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        private static string Fmt(double value) => value.ToString("G4", CultureInfo.InvariantCulture);
    }
}
