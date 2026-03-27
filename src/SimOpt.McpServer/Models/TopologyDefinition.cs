using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>
    /// Describes a complete simulation topology to be instantiated.
    /// </summary>
    public sealed class TopologyDefinition
    {
        [Description("Human-readable name for the model instance")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Model";

        [Description("Random seed for reproducibility")]
        [JsonPropertyName("seed")]
        public int Seed { get; set; } = 42;

        [Description("List of simulation nodes (source, buffer, server, sink)")]
        [JsonPropertyName("nodes")]
        public List<NodeDefinition> Nodes { get; set; } = new();

        [Description("Directed connections between nodes (from -> to)")]
        [JsonPropertyName("connections")]
        public List<ConnectionDefinition> Connections { get; set; } = new();
    }

    /// <summary>
    /// Defines a single simulation node.
    /// </summary>
    public sealed class NodeDefinition
    {
        [Description("Unique identifier for this node within the model")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Description("Node type: source | buffer | server | sink")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [Description("Node-specific parameters (e.g. mean_interval, service_time, capacity)")]
        [JsonPropertyName("params")]
        public Dictionary<string, double> Params { get; set; } = new();
    }

    /// <summary>
    /// A directed connection between two nodes.
    /// </summary>
    public sealed class ConnectionDefinition
    {
        [Description("Source node id")]
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [Description("Destination node id")]
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }
}
