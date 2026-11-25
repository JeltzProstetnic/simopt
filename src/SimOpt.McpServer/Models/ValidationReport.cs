using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>
    /// SIM-66 (D2): one problem found in a topology, machine-actionable: a stable code, the node it is
    /// at, what is wrong and how to fix it, and a number where one exists (a station's offered load).
    /// </summary>
    public sealed record ValidationIssue(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("node"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Node = null,
        [property: JsonPropertyName("suggestion"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Suggestion = null,
        [property: JsonPropertyName("value"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Value = null)
    {
        /// <summary>The diagnosis followed by the fix, as one line of text.</summary>
        [JsonIgnore]
        public string Line => Suggestion == null ? Message : Message + " " + Suggestion;
    }

    /// <summary>
    /// SIM-66 (D2): everything found in a topology at once (UN-008). Errors stop a model from being
    /// built; warnings and info never do.
    /// </summary>
    public sealed class ValidationReport
    {
        [JsonPropertyName("valid")]
        public bool Valid => Errors.Count == 0;

        [JsonPropertyName("errors")]
        public List<ValidationIssue> Errors { get; init; } = new();

        [JsonPropertyName("warnings")]
        public List<ValidationIssue> Warnings { get; init; } = new();

        [JsonPropertyName("info")]
        public List<ValidationIssue> Info { get; init; } = new();
    }
}
