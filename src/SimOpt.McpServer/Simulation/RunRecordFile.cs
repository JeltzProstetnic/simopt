using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Optimization;
using SimOpt.Engine.Api.Simulation;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-86 slice B (UN-033, UN-034): a run record, one JSON file holding everything needed to repeat a
    /// <c>run_experiment</c> or <c>optimize</c> call without the server that made it: the model's schema v1 document, the call
    /// with every default resolved, the answer, and <c>record_sha256</c> over all of that, which is the hook
    /// SIM-88 (UN-036) builds on. <see cref="Reproduce"/> repeats a record and compares every figure.
    /// </summary>
    /// <remarks>
    /// The hash is taken over the record as written without its <c>record_sha256</c> field: indented,
    /// line feeds only, through <see cref="JsonNode"/> on both sides, so a reader re-serialising the parsed
    /// file gets the same bytes (numbers keep their written text).
    /// </remarks>
    public static class RunRecordFile
    {
        public const string Format = "simopt-run/1";

        private static readonly JsonSerializerOptions Options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>The full path a record may be written to; checked before anything is run.</summary>
        /// <exception cref="ArgumentException">Not a .json path, the file exists, or its folder does not.</exception>
        public static string Target(string path)
        {
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"record must end in .json, not \"{path}\".");
            string full = Path.GetFullPath(path);
            if (File.Exists(full)) throw new ArgumentException($"{full} exists; a record is never overwritten, choose another path.");
            string? folder = Path.GetDirectoryName(full);
            if (folder == null || !Directory.Exists(folder)) throw new ArgumentException($"No folder {folder} for the record.");
            return full;
        }

        /// <summary>Writes the record of a finished run and returns its <c>record_sha256</c>.</summary>
        public static string Write(string fullPath, TopologyDefinition topology, IReadOnlyList<string>? metrics,
                                   IReadOnlyList<ScenarioDefinition>? scenarios, ExperimentResult result)
        {
            var call = new ExperimentCall
            {
                Duration = result.Duration,
                Replications = result.Replications,
                Warmup = result.Warmup,
                Seed = result.Seed,
                Metrics = metrics is { Count: > 0 } ? metrics.ToList() : null,
                Scenarios = scenarios is { Count: > 0 } ? scenarios.ToList() : null,
            };
            return WriteRecord(fullPath, "run_experiment", topology,
                JsonSerializer.SerializeToNode(call, Options), JsonSerializer.SerializeToNode(result, Options));
        }

        /// <summary>Writes the record of a finished search and returns its <c>record_sha256</c>.</summary>
        public static string Write(string fullPath, TopologyDefinition topology, IReadOnlyList<DecisionVariable> variables,
                                   ObjectiveDefinition objective, int maxEvaluations, OptimizationResult result)
        {
            var call = new OptimizationCall
            {
                Variables = variables.ToList(),
                Objective = objective,
                Duration = result.Duration,
                Warmup = result.Warmup,
                ReplicationsPerEval = result.ReplicationsPerEval,
                MaxEvaluations = maxEvaluations,
                Strategy = result.Strategy,
                Seed = result.Seed,
            };
            return WriteRecord(fullPath, "optimize", topology,
                JsonSerializer.SerializeToNode(call, Options), JsonSerializer.SerializeToNode(result, Options));
        }

        private static string WriteRecord(string fullPath, string tool, TopologyDefinition topology, JsonNode? call, JsonNode? result)
        {
            var record = new JsonObject
            {
                ["record"] = Format,
                ["tool_call"] = tool,
                ["model"] = JsonNode.Parse(ModelDocument.Write(topology)),
                ["limits"] = JsonSerializer.SerializeToNode(SchemaCatalog.Limits, Options),
                ["call"] = call,
                ["result"] = result,
            };
            string hash = Sha256(record);
            record["record_sha256"] = hash;
            using (var file = new FileStream(fullPath, FileMode.CreateNew))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(record.ToJsonString(Options));
                file.Write(bytes, 0, bytes.Length);
            }
            return hash;
        }

        /// <summary>
        /// SIM-86 slice C: a run record as read, its parts, and whether its two hashes still hold: the record's own
        /// (<c>record_sha256</c>) and its model's (the <c>model_sha256</c> the answer named).
        /// </summary>
        internal sealed record Opened(string FullPath, JsonObject Record, JsonObject Call, JsonObject Result,
                                      TopologyDefinition Topology, bool RecordIntact, bool ModelIntact)
        {
            public string? ToolCall => Text(Record["tool_call"]);
        }

        /// <summary>Reads a record and checks its hashes; runs nothing.</summary>
        /// <exception cref="ArgumentException">No such file, or the file is not a run record.</exception>
        internal static Opened Open(string path)
        {
            string full = Path.GetFullPath(path);
            if (!File.Exists(full)) throw new ArgumentException($"No file {full}.");
            JsonObject record;
            try
            {
                record = JsonNode.Parse(File.ReadAllText(full)) as JsonObject ?? throw new JsonException("not an object");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"{full} is not a run record ({Format}): {ex.Message}");
            }
            if (Text(record["record"]) != Format || record["model"] is not JsonObject model ||
                record["call"] is not JsonObject callNode || record["result"] is not JsonObject recorded)
                throw new ArgumentException($"{full} is not a run record: it needs \"record\": \"{Format}\", model, call and result.");

            string? stated = Text(record["record_sha256"]);
            var withoutHash = (JsonObject)record.DeepClone();
            withoutHash.Remove("record_sha256");
            TopologyDefinition topology = model.Deserialize<TopologyDefinition>(McpJsonUtilities.DefaultOptions)!;
            return new Opened(full, record, callNode, recorded, topology,
                RecordIntact: stated != null && stated == Sha256(withoutHash),
                ModelIntact: Text(recorded["model_sha256"]) == ModelDocument.Sha256(topology));
        }

        /// <summary>Checks a record's hashes, runs its call on its model, and compares every figure.</summary>
        /// <exception cref="ArgumentException">No such file, or the file is not a run record.</exception>
        public static ReproductionReport Reproduce(string path)
        {
            Opened opened = Open(path);
            TopologyDefinition topology = opened.Topology;
            JsonObject recorded = opened.Result;
            List<string> differences = opened.ToolCall switch
            {
                "run_experiment" => RepeatExperiment(topology, opened.Call.Deserialize<ExperimentCall>(Options)!, recorded),
                "optimize" => RepeatSearch(topology, opened.Call.Deserialize<OptimizationCall>(McpJsonUtilities.DefaultOptions)!, recorded),
                var other => throw new ArgumentException($"{opened.FullPath}: a record of \"{other}\" cannot be repeated; " +
                                                         "run_experiment and optimize can."),
            };

            var report = new ReproductionReport
            {
                RecordIntact = opened.RecordIntact,
                ModelIntact = opened.ModelIntact,
                RecordedWith = Text(recorded["tool"]) ?? "unknown",
                ReproducedWith = ExperimentRunner.ToolVersion,
                Identical = differences.Count == 0,
                Differences = differences.Count > 0 ? differences : null,
            };
            report.Summary = Summarise(report);
            return report;
        }

        private static List<string> RepeatExperiment(TopologyDefinition topology, ExperimentCall call, JsonObject recorded)
        {
            ExperimentResult now = ExperimentRunner.Run("record", 1, topology, call.Duration, call.Replications, call.Warmup,
                                                        call.Metrics, call.Scenarios, call.Seed);
            JsonArray then = recorded["scenarios"] as JsonArray ?? new JsonArray();
            JsonArray again = (JsonNode.Parse(JsonSerializer.Serialize(now.Scenarios, Options)) as JsonArray)!;
            return Differences(then, again);
        }

        /// <summary>The search's figures that came out differently: what it found, ranked and counted.</summary>
        private static List<string> RepeatSearch(TopologyDefinition topology, OptimizationCall call, JsonObject recorded)
        {
            OptimizationResult now = Optimizer.Run("record", 1, topology, call.Variables, call.Objective, call.Duration,
                call.Warmup, call.ReplicationsPerEval, call.MaxEvaluations, maxSeconds: null, call.Strategy, call.Seed);
            JsonObject again = (JsonNode.Parse(JsonSerializer.Serialize(now, Options)) as JsonObject)!;
            var found = new List<string>();
            foreach (string key in new[] { "best", "top", "evaluations", "space", "stopped", "strategy" })
                if (!Same(recorded[key], again[key]))
                    found.Add($"{key}: recorded {recorded[key]?.ToJsonString() ?? "nothing"}, now {again[key]?.ToJsonString() ?? "nothing"}");
            return found;
        }

        private static string Sha256(JsonObject withoutHash) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(withoutHash.ToJsonString(Options)))).ToLowerInvariant();

        private static string? Text(JsonNode? node) =>
            node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

        private static bool Same(JsonNode? a, JsonNode? b) =>
            (a?.ToJsonString(Options) ?? "null") == (b?.ToJsonString(Options) ?? "null");

        /// <summary>Every metric and paired difference that came out differently, and anything else in a scenario.</summary>
        private static List<string> Differences(JsonArray then, JsonArray again)
        {
            var found = new List<string>();
            if (then.Count != again.Count) found.Add($"{then.Count} scenarios recorded, {again.Count} now");
            for (int i = 0; i < Math.Min(then.Count, again.Count); i++)
            {
                JsonObject a = then[i] as JsonObject ?? new JsonObject(), b = again[i] as JsonObject ?? new JsonObject();
                if (Same(a, b)) continue;
                string name = Text(a["name"]) ?? $"scenario {i + 1}";
                int before = found.Count;
                Compare(found, name, a["metrics"] as JsonObject, b["metrics"] as JsonObject);
                Compare(found, $"{name} vs baseline", a["vs_baseline"] as JsonObject, b["vs_baseline"] as JsonObject);
                if (found.Count == before) found.Add($"{name}: its flags, errors or patch differ");
            }
            return found;
        }

        private static void Compare(List<string> found, string scenario, JsonObject? then, JsonObject? again)
        {
            IEnumerable<string> keys = (then?.Select(p => p.Key) ?? Enumerable.Empty<string>())
                .Union(again?.Select(p => p.Key) ?? Enumerable.Empty<string>());
            foreach (string key in keys)
            {
                JsonNode? a = then?[key], b = again?[key];
                if (!Same(a, b)) found.Add($"{scenario} {key}: recorded {Figure(a)}, now {Figure(b)}");
            }
        }

        /// <summary>"38.2 [37.06, 39.34]" from a metric or difference, or its JSON when it has no mean or diff.</summary>
        private static string Figure(JsonNode? node)
        {
            if (node is not JsonObject o) return "nothing";
            JsonNode? value = o["mean"] ?? o["diff"];
            string interval = o["ci95"] is JsonArray ci && ci.Count == 2 ? $" [{ci[0]?.ToJsonString()}, {ci[1]?.ToJsonString()}]" : "";
            return value != null ? value.ToJsonString() + interval : o.ToJsonString();
        }

        private static string Summarise(ReproductionReport r)
        {
            var parts = new List<string>
            {
                r.Identical ? "Every recorded figure came out the same" : $"{r.Differences?.Count ?? 0} recorded figures came out differently",
                r.RecordIntact ? "the record is as written" : "the record was changed after it was written (record_sha256 does not match)",
                r.ModelIntact ? "its model is the one the answer named" : "its model is not the one the answer named (model_sha256 does not match)",
            };
            if (r.RecordedWith != r.ReproducedWith)
                parts.Add($"recorded with {r.RecordedWith}, reproduced with {r.ReproducedWith}, so a difference may come from the tool");
            return string.Join("; ", parts) + ".";
        }
    }
}
