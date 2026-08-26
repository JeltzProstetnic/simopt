using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>
    /// A probability distribution as it appears in a schema-v1 topology document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIM-65. Every duration and interval in a model is one of these. The fields are deliberately
    /// flat and optional rather than a nested per-type payload: an LLM emits <c>{"type":"triangular",
    /// "min":20,"mode":30,"max":40}</c> far more reliably than it emits a discriminated union, the
    /// shape survives being published as JSON Schema, and a wrong field for a given type is caught
    /// by <see cref="SimOpt.McpServer.Simulation.DistributionFactory"/> with the field named.
    /// </para>
    /// <para>
    /// Which fields a type requires is documented in
    /// <see cref="SimOpt.McpServer.Models.SchemaCatalog"/>, which is also what <c>get_schema</c> and
    /// <c>list_templates</c> render — so the vocabulary a client is told about and the vocabulary
    /// the builder accepts cannot drift apart.
    /// </para>
    /// </remarks>
    public sealed class DistributionSpec
    {
        [Description("Distribution family: exponential | triangular | uniform | lognormal | gamma | constant | empirical")]
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [Description("Mean value (exponential, lognormal, gamma)")]
        [JsonPropertyName("mean")]
        public double? Mean { get; set; }

        [Description("Rate lambda, the reciprocal of the mean (exponential; alternative to mean)")]
        [JsonPropertyName("rate")]
        public double? Rate { get; set; }

        [Description("Lower bound (triangular, uniform, empirical)")]
        [JsonPropertyName("min")]
        public double? Min { get; set; }

        [Description("Most likely value (triangular)")]
        [JsonPropertyName("mode")]
        public double? Mode { get; set; }

        [Description("Upper bound (triangular, uniform, empirical)")]
        [JsonPropertyName("max")]
        public double? Max { get; set; }

        [Description("The single value produced (constant)")]
        [JsonPropertyName("value")]
        public double? Value { get; set; }

        [Description("Standard deviation of the distribution itself (lognormal, with mean)")]
        [JsonPropertyName("stddev")]
        public double? Stddev { get; set; }

        [Description("Mean of the underlying normal (lognormal; alternative to mean/stddev)")]
        [JsonPropertyName("mu")]
        public double? Mu { get; set; }

        [Description("Standard deviation of the underlying normal (lognormal; alternative to mean/stddev)")]
        [JsonPropertyName("sigma")]
        public double? Sigma { get; set; }

        [Description("Shape parameter k (gamma)")]
        [JsonPropertyName("k")]
        public double? K { get; set; }

        [Description("Scale parameter theta (gamma; alternative to mean)")]
        [JsonPropertyName("theta")]
        public double? Theta { get; set; }

        [Description("Constant offset added to every draw, e.g. a fixed handling time before a random remainder")]
        [JsonPropertyName("shift")]
        public double? Shift { get; set; }

        [Description("Probabilities of equally spaced values from min to max, summing to 1 (empirical)")]
        [JsonPropertyName("probabilities")]
        public List<double>? Probabilities { get; set; }
    }
}
