using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>
    /// SIM-66 (D9): one change to a model's topology. Which fields an operation reads depends on its
    /// <see cref="Op"/>; see <c>TopologyPatcher</c>.
    /// </summary>
    public sealed class PatchOperation
    {
        [Description("set | add_node | remove_node | set_connection | remove_connection | set_metric | remove_metric")]
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        [Description("set: \"name\", \"seed\", \"<node>.<param>\" or \"<node>.<param>.<field>\" (a field of a distribution, e.g. \"arrivals.arrival.mean\")")]
        [JsonPropertyName("path")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Path { get; set; }

        [Description("set: the new value, a number, a string or a distribution object; null removes the parameter. " +
                     "add_node: the node {id, type, params}. set_metric: the metric {id, kind, node?}")]
        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Value { get; set; }

        [Description("remove_node: the node's id")]
        [JsonPropertyName("node")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Node { get; set; }

        [Description("set_connection, remove_connection: the node the connection leaves")]
        [JsonPropertyName("from")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? From { get; set; }

        [Description("set_connection, remove_connection: the node the connection enters")]
        [JsonPropertyName("to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? To { get; set; }

        [Description("set_connection: the connection's routing weight, read only when it leaves a probabilistic router")]
        [JsonPropertyName("weight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Weight { get; set; }

        [Description("set_connection, remove_connection: the process outlet the connection leaves by; none for the normal exit")]
        [JsonPropertyName("outlet")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Outlet { get; set; }

        [Description("remove_metric: the metric's id")]
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }
    }
}
