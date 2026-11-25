using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>
    /// SIM-86 slice B: a <c>run_experiment</c> call as a run record keeps it, every default resolved, so
    /// repeating it needs nothing from the server that first ran it.
    /// </summary>
    public sealed class ExperimentCall
    {
        [JsonPropertyName("duration")] public double Duration { get; set; }
        [JsonPropertyName("replications")] public int Replications { get; set; }
        [JsonPropertyName("warmup")] public double Warmup { get; set; }
        [JsonPropertyName("seed")] public int Seed { get; set; }

        /// <summary>The metrics asked for; absent when all were.</summary>
        [JsonPropertyName("metrics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Metrics { get; set; }

        [JsonPropertyName("scenarios")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ScenarioDefinition>? Scenarios { get; set; }
    }

    /// <summary>SIM-86 slice B: where <c>run_experiment</c> wrote its run record, and the record's own hash.</summary>
    public sealed record RecordWritten(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("record_sha256")] string RecordSha256);

    /// <summary>
    /// SIM-86 slice B (UN-033): what <c>reproduce_run</c> found. <see cref="RecordIntact"/>: the record's
    /// content still hashes to the <c>record_sha256</c> written into it. <see cref="ModelIntact"/>: the
    /// model document in it still hashes to the <c>model_sha256</c> the answer named. <see cref="Identical"/>:
    /// running the recorded call on the recorded model gave every recorded figure again.
    /// </summary>
    public sealed class ReproductionReport
    {
        [JsonPropertyName("record_intact")] public bool RecordIntact { get; set; }
        [JsonPropertyName("model_intact")] public bool ModelIntact { get; set; }
        [JsonPropertyName("recorded_with")] public string RecordedWith { get; set; } = string.Empty;
        [JsonPropertyName("reproduced_with")] public string ReproducedWith { get; set; } = string.Empty;
        [JsonPropertyName("identical")] public bool Identical { get; set; }

        /// <summary>Each figure that came out differently, "scenario metric: recorded …, now …"; absent when none did.</summary>
        [JsonPropertyName("differences")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Differences { get; set; }

        [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    }
}
