using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimOpt.Engine.Api.Models
{
    /// <summary>
    /// Describes a complete simulation topology to be instantiated.
    /// </summary>
    public sealed class TopologyDefinition
    {
        [Description("Schema version of this document; \"1\" when given")]
        [JsonPropertyName("schema_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SchemaVersion { get; set; }

        [Description("Human-readable name for the model instance")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Model";

        /// <summary>SIM-144: the seed a model runs at when neither it nor the call gives one.</summary>
        public const int DefaultSeed = 42;

        /// <summary>
        /// SIM-144: null when the document leaves the seed out, so that such a document stays apart from one
        /// giving 42 (in its saved bytes, its hash and the run's list of assumed values).
        /// </summary>
        [Description("Random seed for reproducibility; 42 when left out")]
        [JsonPropertyName("seed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Seed { get; set; }

        /// <summary>SIM-144: the seed the model runs at: its own, else <see cref="DefaultSeed"/>.</summary>
        [JsonIgnore]
        public int EffectiveSeed => Seed ?? DefaultSeed;

        [Description("List of simulation nodes (source, queue, station, delay, router, sink; resource and stock beside the item flow)")]
        [JsonPropertyName("nodes")]
        public List<NodeDefinition> Nodes { get; set; } = new();

        [Description("Directed connections between nodes: {from, to, weight?, outlet?}, or [from, to] or [from, to, weight]")]
        [JsonPropertyName("connections")]
        public List<ConnectionDefinition> Connections { get; set; } = new();

        [Description("Declared metrics: [{id, kind, node?}], kind one of wait_time, queue_length (at a queue or a resource), " +
                     "utilization (at a station or a resource), level, empty_share (at a stock), count, throughput (at a sink), " +
                     "cycle_time (at a sink, or all sinks without a node)")]
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

        [Description("wait_time | queue_length | utilization | count | throughput | cycle_time | level | empty_share")]
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

        [Description("Node type: source | queue | station | delay | router | sink | resource | stock (buffer and server are accepted for queue and station)")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [Description("Node-specific parameters, each a number, a string or a distribution object (e.g. mean_interval, service_time, capacity)")]
        [JsonPropertyName("params")]
        public Dictionary<string, ParamValue> Params { get; set; } = new();

        /// <summary>
        /// SIM-126: a process node's steps, in order. Null on every other node, so a document without a
        /// process is written, and hashed, as before.
        /// </summary>
        [Description("A process node's steps, in order: {type, id?, fields}, type one of delay, seize, release, take, put (see get_schema's step_types)")]
        [JsonPropertyName("steps")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<StepDefinition>? Steps { get; set; }
    }

    /// <summary>
    /// A directed connection between two nodes.
    /// </summary>
    /// <remarks>
    /// SIM-65 step 6: read from <c>{"from": "a", "to": "b", "weight": 0.7}</c> or from the shorter
    /// <c>["a", "b"]</c> / <c>["a", "b", 0.7]</c>, which the readiness review's clinic walkthrough uses
    /// and a client LLM writes as readily; written as the object.
    /// </remarks>
    [JsonConverter(typeof(ConnectionDefinitionConverter))]
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

        /// <summary>
        /// SIM-126 (rule 3): the outlet of a process this connection leaves by, the exit of the items that
        /// gave up at a step naming it; null for the process's normal exit and on every other node.
        /// </summary>
        [Description("Optional: the outlet of a process this connection leaves by (the items that gave up at a step naming it)")]
        [JsonPropertyName("outlet")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Outlet { get; set; }
    }

    /// <summary>Reads a connection as an object or an array, and writes it as an object.</summary>
    public sealed class ConnectionDefinitionConverter : JsonConverter<ConnectionDefinition>
    {
        public override ConnectionDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray) return ReadArray(ref reader);
            if (reader.TokenType == JsonTokenType.StartObject) return ReadObject(ref reader);
            throw new JsonException("A connection is {\"from\", \"to\", \"weight\"?} or [from, to] or [from, to, weight].");
        }

        private static ConnectionDefinition ReadArray(ref Utf8JsonReader reader)
        {
            var connection = new ConnectionDefinition();
            int index = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (index++)
                {
                    case 0: connection.From = Text(ref reader, "from"); break;
                    case 1: connection.To = Text(ref reader, "to"); break;
                    case 2:
                        if (reader.TokenType != JsonTokenType.Number)
                            throw new JsonException("The third entry of a connection array is its weight, a number.");
                        connection.Weight = reader.GetDouble();
                        break;
                    default:
                        throw new JsonException("A connection array is [from, to] or [from, to, weight].");
                }
            }
            if (index < 2) throw new JsonException("A connection array names its from and its to node: [from, to].");
            return connection;
        }

        private static ConnectionDefinition ReadObject(ref Utf8JsonReader reader)
        {
            var connection = new ConnectionDefinition();
            bool from = false, to = false;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string property = reader.GetString() ?? string.Empty;
                reader.Read();
                if (property.Equals("from", StringComparison.OrdinalIgnoreCase)) { connection.From = Text(ref reader, "from"); from = true; }
                else if (property.Equals("to", StringComparison.OrdinalIgnoreCase)) { connection.To = Text(ref reader, "to"); to = true; }
                else if (property.Equals("weight", StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.TokenType == JsonTokenType.Null) connection.Weight = null;
                    else if (reader.TokenType == JsonTokenType.Number) connection.Weight = reader.GetDouble();
                    else throw new JsonException("A connection's weight is a number.");
                }
                else if (property.Equals("outlet", StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.TokenType == JsonTokenType.Null) connection.Outlet = null;
                    else connection.Outlet = Text(ref reader, "outlet");
                }
                else reader.Skip();
            }
            if (!from || !to) throw new JsonException("A connection names its from and its to node.");
            return connection;
        }

        private static string Text(ref Utf8JsonReader reader, string what) =>
            reader.TokenType == JsonTokenType.String
                ? reader.GetString()!
                : throw new JsonException($"A connection's {what} is a node id, a string.");

        public override void Write(Utf8JsonWriter writer, ConnectionDefinition value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("from", value.From);
            writer.WriteString("to", value.To);
            if (value.Weight is double weight) writer.WriteNumber("weight", weight);
            if (value.Outlet != null) writer.WriteString("outlet", value.Outlet);
            writer.WriteEndObject();
        }
    }
}
