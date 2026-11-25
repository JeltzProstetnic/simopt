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

        [Description("List of simulation nodes (source, queue, station, delay, router, sink)")]
        [JsonPropertyName("nodes")]
        public List<NodeDefinition> Nodes { get; set; } = new();

        [Description("Directed connections between nodes (from -> to)")]
        [JsonPropertyName("connections")]
        public List<ConnectionDefinition> Connections { get; set; } = new();

        [Description("Declared metrics: [{id, kind, node?}], kind one of wait_time, queue_length (at a queue), " +
                     "utilization (at a station), count, throughput (at a sink), cycle_time (at a sink, or all sinks without a node)")]
        [JsonPropertyName("metrics")]
        public List<MetricDefinition> Metrics { get; set; } = new();
    }

    /// <summary>
    /// SIM-65 step 5 (D6): a metric the model measures, addressable by its id. Attaching the
    /// collector is the builder's job; reporting it is SIM-66's.
    /// </summary>
    public sealed class MetricDefinition
    {
        [Description("Unique identifier of this metric within the model")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Description("wait_time | queue_length | utilization | count | throughput | cycle_time")]
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [Description("The node it is measured at; optional for cycle_time only")]
        [JsonPropertyName("node")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Node { get; set; }
    }

    /// <summary>
    /// Defines a single simulation node.
    /// </summary>
    public sealed class NodeDefinition
    {
        [Description("Unique identifier for this node within the model")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Description("Node type: source | queue | station | delay | router | sink (buffer and server are accepted for queue and station)")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [Description("Node-specific parameters, each a number, a string or a distribution object (e.g. mean_interval, service_time, capacity)")]
        [JsonPropertyName("params")]
        public Dictionary<string, ParamValue> Params { get; set; } = new();
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

        /// <summary>
        /// SIM-65: the edge's share of a router's traffic. The weight belongs to the edge, not to the
        /// router node, which keeps <see cref="ParamValue"/> free of maps. Only a router reads it.
        /// </summary>
        [Description("Optional routing weight of this connection, read only on connections leaving a probabilistic router (all or none)")]
        [JsonPropertyName("weight")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Weight { get; set; }
    }
}
