using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;

namespace SimOpt.McpServer.Models
{
    /// <summary>SIM-68 (D1): a decision variable, a patch path the search may set within bounds.</summary>
    public sealed class DecisionVariable
    {
        [Description("A set path as for patch_model: \"<node>.<param>\" or \"<node>.<param>.<field>\", e.g. \"exam.servers\"")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [Description("\"integer\" or \"real\"")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "real";

        [Description("Lowest value")]
        [JsonPropertyName("min")]
        public double Min { get; set; }

        [Description("Highest value")]
        [JsonPropertyName("max")]
        public double Max { get; set; }

        [Description("Optional grid: values are min + k * step")]
        [JsonPropertyName("step")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Step { get; set; }
    }

    /// <summary>SIM-68 (D4): a bound a metric's mean must keep.</summary>
    public sealed class ConstraintDefinition
    {
        [Description("A metric id")]
        [JsonPropertyName("metric")]
        public string Metric { get; set; } = string.Empty;

        [Description("\"<\", \"<=\", \">\" or \">=\"")]
        [JsonPropertyName("op")]
        public string Op { get; set; } = "<";

        [Description("The bound")]
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    /// <summary>SIM-68 (D4): what the search optimises.</summary>
    public sealed class ObjectiveDefinition
    {
        [Description("A metric id, or a decision variable's path (e.g. \"exam.servers\" to use as few as possible)")]
        [JsonPropertyName("metric")]
        public string Metric { get; set; } = string.Empty;

        [Description("\"min\" or \"max\"")]
        [JsonPropertyName("direction")]
        public string Direction { get; set; } = "min";

        [Description("Bounds on metric means a solution must keep: [{metric, op, value}]")]
        [JsonPropertyName("constraints")]
        public List<ConstraintDefinition> Constraints { get; set; } = new();
    }
}
