using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>SIM-66 (D10): a named variant of the model, made by a patch of its version.</summary>
    public sealed class ScenarioDefinition
    {
        [Description("The scenario's name, unique in the call")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Description("Operations as for patch_model, applied to the version being run")]
        [JsonPropertyName("patch")]
        public List<PatchOperation> Patch { get; set; } = new();
    }

    /// <summary>A metric's mean over replications with its 95 % interval; null fields when no replication measured it.</summary>
    public sealed record MetricEstimate(
        [property: JsonPropertyName("mean")] double? Mean,
        [property: JsonPropertyName("ci95"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double[]? Ci95);

    /// <summary>
    /// SIM-66 (D11): a scenario's difference from the baseline, replication by replication, with its
    /// 95 % interval; significant when the interval excludes 0.
    /// </summary>
    public sealed record Difference(
        [property: JsonPropertyName("diff")] double Diff,
        [property: JsonPropertyName("ci95"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double[]? Ci95,
        [property: JsonPropertyName("significant")] bool Significant);

    /// <summary>SIM-66 (D12): what went wrong in a scenario's runs; absent when nothing did.</summary>
    public sealed class ScenarioFlags
    {
        /// <summary>Stations with an offered load of 1 or more (OfferedLoad).</summary>
        [JsonPropertyName("saturated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Saturated { get; set; }

        /// <summary>Items refused, by node, as a mean per replication (warm-up included).</summary>
        [JsonPropertyName("refused")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, double>? Refused { get; set; }

        [JsonPropertyName("nothing_finished")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? NothingFinished { get; set; }

        /// <summary>Metrics no replication produced a value for.</summary>
        [JsonPropertyName("no_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? NoData { get; set; }

        [JsonIgnore]
        public bool Any => Saturated != null || Refused != null || NothingFinished != null || NoData != null;
    }

    public sealed class ScenarioResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>SIM-86: the patch that made the scenario from the version run; absent for the baseline.</summary>
        [JsonPropertyName("patch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PatchOperation>? Patch { get; set; }

        /// <summary>Why the scenario was not run: its patch, or the patched model, has errors.</summary>
        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("metrics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, MetricEstimate>? Metrics { get; set; }

        [JsonPropertyName("vs_baseline")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, Difference>? VsBaseline { get; set; }

        [JsonPropertyName("flags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScenarioFlags? Flags { get; set; }

        /// <summary>Every replication's figure per metric, kept for the comparison and not sent.</summary>
        [JsonIgnore]
        public Dictionary<string, double[]> Replicates { get; } = new();
    }

    /// <summary>
    /// SIM-66 (D10-D12): the outcome of <c>run_experiment</c>, with what a third party needs to
    /// reproduce it (UN-034): model version and the SHA-256 of its document (SIM-86), seed, run length,
    /// warm-up, replications, confidence, the tool's version, and what the run assumed rather than was
    /// told (UN-035). Replication r runs at <c>ReplicationRunner.SeedFor(seed, r)</c>.
    /// </summary>
    public sealed class ExperimentResult
    {
        [JsonPropertyName("model_id")] public string ModelId { get; set; } = string.Empty;
        [JsonPropertyName("version")] public int Version { get; set; }

        /// <summary>SIM-86: the SHA-256 of the version's schema v1 document, the bytes save_model writes.</summary>
        [JsonPropertyName("model_sha256")] public string ModelSha256 { get; set; } = string.Empty;

        [JsonPropertyName("duration")] public double Duration { get; set; }
        [JsonPropertyName("warmup")] public double Warmup { get; set; }
        [JsonPropertyName("replications")] public int Replications { get; set; }
        [JsonPropertyName("seed")] public int Seed { get; set; }
        [JsonPropertyName("confidence")] public double Confidence { get; set; }
        [JsonPropertyName("tool")] public string Tool { get; set; } = string.Empty;

        /// <summary>
        /// SIM-86 (UN-035): every value the run used without being given it, call arguments by name and node
        /// parameters by patch path, with the value used; absent when everything was given.
        /// </summary>
        [JsonPropertyName("assumed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Assumed { get; set; }

        [JsonPropertyName("scenarios")] public List<ScenarioResult> Scenarios { get; set; } = new();
        [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    }
}
