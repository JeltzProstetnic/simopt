using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>SIM-69: one evaluated point as <c>optimize</c> reports it.</summary>
    public sealed class OptimizedPoint
    {
        /// <summary>The value of each decision variable, by path.</summary>
        [JsonPropertyName("point")] public Dictionary<string, double> Point { get; set; } = new();

        /// <summary>The objective's value at this point (a metric's mean, or the variable itself).</summary>
        [JsonPropertyName("objective")] public double? Objective { get; set; }

        [JsonPropertyName("feasible")] public bool Feasible { get; set; }

        [JsonPropertyName("metrics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, MetricEstimate>? Metrics { get; set; }

        [JsonPropertyName("violations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Violations { get; set; }

        /// <summary>Stations whose offered load at this point is 1 or more: their figures depend on the run length.</summary>
        [JsonPropertyName("saturated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Saturated { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Error { get; set; }
    }

    /// <summary>
    /// SIM-69 (D9): the outcome of <c>optimize</c>, with what a third party needs to repeat the search
    /// (UN-034): version, strategy, seeds, run length, warm-up, replications per point, tool version.
    /// </summary>
    public sealed class OptimizationResult
    {
        [JsonPropertyName("model_id")] public string ModelId { get; set; } = string.Empty;
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("strategy")] public string Strategy { get; set; } = string.Empty;
        [JsonPropertyName("evaluations")] public int Evaluations { get; set; }

        /// <summary>Distinct points of the quantised space, or null when it is continuous.</summary>
        [JsonPropertyName("space")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Space { get; set; }

        /// <summary>"done" (the strategy finished), "budget" or "time".</summary>
        [JsonPropertyName("stopped")] public string Stopped { get; set; } = "done";

        [JsonPropertyName("best")] public OptimizedPoint Best { get; set; } = new();

        /// <summary>The five best points, best first, without their metrics.</summary>
        [JsonPropertyName("top")] public List<OptimizedPoint> Top { get; set; } = new();

        [JsonPropertyName("duration")] public double Duration { get; set; }
        [JsonPropertyName("warmup")] public double Warmup { get; set; }
        [JsonPropertyName("replications_per_eval")] public int ReplicationsPerEval { get; set; }
        [JsonPropertyName("seed")] public int Seed { get; set; }
        [JsonPropertyName("tool")] public string Tool { get; set; } = string.Empty;
        [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    }
}
