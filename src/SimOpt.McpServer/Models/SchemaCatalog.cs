using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using SimOpt.McpServer.Simulation;

namespace SimOpt.McpServer.Models
{
    /// <summary>A node parameter as the catalogue publishes it.</summary>
    /// <param name="Kind">"distribution", "number", "integer" or "string".</param>
    /// <param name="Alternative">An older parameter accepted instead, e.g. an exponential mean.</param>
    public sealed record ParameterSpec(string Name, string Kind, bool Required, string? Default, string Description,
                                       IReadOnlyList<string>? Choices = null, string? Alternative = null);

    /// <summary>A node type as the catalogue publishes it.</summary>
    public sealed record NodeTypeSpec(string Name, IReadOnlyList<string> Aliases, string Description,
                                      IReadOnlyList<ParameterSpec> Parameters, string FedBy, string Feeds);

    /// <summary>A metric kind as the catalogue publishes it.</summary>
    public sealed record MetricKindSpec(string Name, string MeasuredAt, string Description);

    /// <summary>SIM-126: a step type of a process node as the catalogue publishes it, with the fields it reads.</summary>
    public sealed record StepTypeSpec(string Name, string Description, IReadOnlyList<ParameterSpec> Fields);

    /// <summary>
    /// SIM-65 step 6 (D7): the one source of truth for the topology vocabulary of schema v1. The
    /// builder (<see cref="ModelRegistry"/>) resolves node types through it, and <c>get_schema</c> and
    /// <c>list_templates</c> render it, so what a client is told and what the builder accepts cannot
    /// drift apart; <c>SchemaCatalogTests</c> asserts they agree.
    /// </summary>
    public static class SchemaCatalog
    {
        public const string SchemaVersion = "1";

        /// <summary>SIM-126: the metric kind of a stock's time-average level.</summary>
        public const string LevelKind = "level";

        /// <summary>SIM-126: the metric kind of the share of time a stock stood empty.</summary>
        public const string EmptyShareKind = "empty_share";

        /// <summary>SIM-126: node types beside the item flow, which nothing is connected to; process steps use them.</summary>
        public static IReadOnlyList<string> OffFlowTypes { get; } = new[] { "resource", "stock" };

        private static readonly string[] None = System.Array.Empty<string>();

        public static IReadOnlyList<NodeTypeSpec> NodeTypes { get; } = new[]
        {
            new NodeTypeSpec("source", None,
                "Makes items (customers, jobs, parts) at random intervals and pushes each to the node it is connected to.",
                new[]
                {
                    new ParameterSpec("arrival", "distribution", false, "exponential, mean 1",
                        "Time between arrivals.", Alternative: "mean_interval"),
                },
                FedBy: "nothing", Feeds: "a queue, delay, router, sink or process"),
            new NodeTypeSpec("queue", new[] { "buffer" },
                "Holds items until a station takes them; a full queue refuses new items.",
                new[]
                {
                    new ParameterSpec("capacity", "integer", false, "unlimited", "Most items held at once."),
                    new ParameterSpec("discipline", "string", false, "fifo", "Which item a station takes next.",
                        Choices: new[] { "fifo", "lifo" }),
                },
                FedBy: "a source, station, delay, router or process", Feeds: "a station"),
            new NodeTypeSpec("station", new[] { "server" },
                "Serves items taken from its queue, each for its own service time, on one or more servers.",
                new[]
                {
                    new ParameterSpec("servers", "integer", false, "1",
                        "Servers working in parallel, 1 to 10,000; reported as <id>#1 .. <id>#c when more than one."),
                    new ParameterSpec("service", "distribution", false, "exponential, mean 1",
                        "Service time of one item.", Alternative: "service_time"),
                },
                FedBy: "a queue only", Feeds: "a queue, delay, router, sink or process"),
            new NodeTypeSpec("delay", None,
                "Holds every item for its own duration, any number at once, then passes it on (walking, cooling, transport).",
                new[]
                {
                    new ParameterSpec("duration", "distribution", true, null,
                        "How long each item is held.", Alternative: "delay_time"),
                },
                FedBy: "a source, station, delay, router or process", Feeds: "a queue, delay, router, sink or process"),
            new NodeTypeSpec("router", None,
                "Passes each item straight on to one of the nodes it is connected to.",
                new[]
                {
                    new ParameterSpec("policy", "string", true, null,
                        "probabilistic: in proportion to the weight on each connection leaving it (all or none; none splits " +
                        "equally). shortest_queue: to the queue with the fewest items waiting or in service behind it, " +
                        "a tie to the connection listed first; queues only, no weights.",
                        Choices: new[] { "probabilistic", "shortest_queue" }),
                },
                FedBy: "a source, station, delay, router or process", Feeds: "a queue, delay, router, sink or process"),
            new NodeTypeSpec("sink", None,
                "Absorbs and counts finished items.",
                System.Array.Empty<ParameterSpec>(),
                FedBy: "a source, station, delay, router or process", Feeds: "nothing"),
            new NodeTypeSpec("process", None,
                "Runs every item it receives through its steps, in order, any number of items at once, then passes it on " +
                "(an exam that needs a nurse, a wash that needs a bay and soap). An item leaving frees every unit it still " +
                "holds. Every random step value is drawn when the item enters. No loops or conditions: route between nodes.",
                System.Array.Empty<ParameterSpec>(),
                FedBy: "a source, station, delay, router or process (not a queue: it waits at its seize step)",
                Feeds: "a queue, delay, router, sink or process"),
            new NodeTypeSpec("resource", None,
                "Units an item holds while a process works on it (bays, nurses, forklifts), first come, first served. " +
                "Not on the item flow: nothing is connected to it; a process's seize step takes a unit and its release frees it.",
                new[]
                {
                    new ParameterSpec("capacity", "integer", false, "1", "Units, 1 to 10,000."),
                },
                FedBy: "nothing (a process seizes it)", Feeds: "nothing"),
            new NodeTypeSpec("stock", None,
                "A level of material (soap, parts, kilograms) that processes take from and put into. Not on the item flow: " +
                "nothing is connected to it; a take waits until the whole amount is there, a put while it would overfill.",
                new[]
                {
                    new ParameterSpec("initial", "number", false, "0", "Level at the start of every replication."),
                    new ParameterSpec("capacity", "number", false, "unlimited", "Most the stock holds, a positive number."),
                },
                FedBy: "nothing (a process takes from it and puts into it)", Feeds: "nothing"),
        };

        private static readonly ParameterSpec ResourceField =
            new ParameterSpec("resource", "string", true, null, "The id of a resource node.");

        private static readonly ParameterSpec StockField =
            new ParameterSpec("stock", "string", true, null, "The id of a stock node.");

        private static readonly ParameterSpec AmountField =
            new ParameterSpec("amount", "number or distribution", true, null,
                "A positive number, or a distribution drawn when the item enters that cannot go below 0 or beyond the " +
                "stock's capacity; a draw of 0 moves nothing.");

        private static readonly ParameterSpec PatienceField =
            new ParameterSpec("patience", "distribution", false, "none (waits for ever)",
                "How long an item waits at this step before it gives up, drawn when it enters; it cannot go below 0. Needs outlet.");

        private static readonly ParameterSpec OutletField =
            new ParameterSpec("outlet", "string", false, null,
                "With patience: the name of the exit an item that gave up leaves by, holding nothing; a connection " +
                "{\"from\": <process>, \"to\": <node>, \"outlet\": <name>} carries it.");

        /// <summary>SIM-126: the closed list of steps a process node's <c>steps</c> may use (Q-PROC-SCHEMA).</summary>
        public static IReadOnlyList<StepTypeSpec> StepTypes { get; } = new[]
        {
            new StepTypeSpec("delay", "Holds the item for its own duration (washing, an exam, transport).",
                new[] { new ParameterSpec("duration", "distribution", true, null, "How long; it cannot go below 0.") }),
            new StepTypeSpec("seize",
                "Waits for a unit of the resource, first come, first served, and holds it until a release step or until " +
                "the item leaves the process; with patience, for at most that long.",
                new[] { ResourceField, PatienceField, OutletField }),
            new StepTypeSpec("release", "Frees one unit of a resource the item holds, before it leaves the process.",
                new[] { ResourceField }),
            new StepTypeSpec("take",
                "Takes the amount out of the stock, waiting until the whole amount is there; with patience, for at most that long.",
                new[] { StockField, AmountField, PatienceField, OutletField }),
            new StepTypeSpec("put", "Puts the amount into the stock, waiting while it would overfill the stock.",
                new[] { StockField, AmountField }),
        };

        public static IReadOnlyList<MetricKindSpec> MetricKinds { get; } = new[]
        {
            new MetricKindSpec("wait_time", "a queue or a resource",
                "Mean time an item waits in the queue, or for a unit of the resource (over the items granted one)."),
            new MetricKindSpec("queue_length", "a queue or a resource", "Time-average number of items waiting."),
            new MetricKindSpec("utilization", "a station or a resource",
                "Time-average busy share of the station's servers or of the resource's units."),
            new MetricKindSpec("count", "a sink", "Items that reached the sink after the warm-up."),
            new MetricKindSpec("throughput", "a sink", "Items that reached the sink per unit of time after the warm-up."),
            new MetricKindSpec("cycle_time", "a sink, or every sink without a node",
                "Mean time from an item's creation at a source to its arrival at the sink."),
            new MetricKindSpec("share", "a sink",
                "Share of the items reaching any sink after the warm-up that reach this one (the share who gave up, a scrap rate)."),
            new MetricKindSpec(LevelKind, "a stock", "Time-average level of the stock."),
            new MetricKindSpec(EmptyShareKind, "a stock", "Share of time the stock stood empty."),
        };

        /// <summary>
        /// SIM-86 (UN-035): what a schema v1 model does not represent, published by <c>get_schema</c> and
        /// written into every run record, so that no figure is read as covering it.
        /// </summary>
        public static IReadOnlyList<string> Limits { get; } = new[]
        {
            "Items move between connected nodes in no time; travel, transport and handling take time only where a delay node or a delay step is placed.",
            "Stations never break down, and their servers work without shifts, breaks or set-ups.",
            "Arrivals keep one distribution for the whole run: no daily pattern and no peak hours.",
            "All items are alike: no item types, attributes or priorities; a queue serves first in first out or last in first out, and a router never looks at the item.",
            "A station's servers are identical and serve one item at a time each, and a station shares nothing with another; what activities share (staff, bays) is a resource that process steps seize, one unit per step.",
            "A process has no loops or conditions, and an item holds a unit only within one process node; it frees every unit it still holds when it leaves.",
            "An item waits in its queue until served and never leaves early, except at a seize or take step with a patience; an item a full queue refuses is lost, and counted.",
            "Every replication starts empty and idle; only the warm-up keeps that start out of the figures.",
        };

        /// <summary>The distribution families, in the order they are advertised.</summary>
        public static IReadOnlyList<string> DistributionTypes => DistributionFactory.KnownTypes;

        /// <summary>The v1 name of a node type or one of its aliases, ignoring case; null when unknown.</summary>
        public static string? Canonical(string? type)
        {
            string wanted = (type ?? string.Empty).Trim().ToLowerInvariant();
            foreach (NodeTypeSpec spec in NodeTypes)
                if (spec.Name == wanted || spec.Aliases.Contains(wanted)) return spec.Name;
            return null;
        }

        /// <summary>The node types for an error message: the v1 names, then the aliases.</summary>
        public static string Vocabulary()
        {
            string names = string.Join(", ", NodeTypes.Select(t => t.Name));
            string aliases = string.Join(" and ", NodeTypes.SelectMany(t => t.Aliases));
            string of = string.Join(" and ", NodeTypes.Where(t => t.Aliases.Count > 0).Select(t => t.Name));
            return $"{names} ({aliases} are accepted for {of})";
        }

        /// <summary>The topology document as JSON Schema (draft 2020-12).</summary>
        public static JsonObject JsonSchema()
        {
            JsonArray Strings(IEnumerable<string> values) => new JsonArray(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());
            JsonObject Number(string description) => new JsonObject { ["type"] = "number", ["description"] = description };
            JsonObject Value() => new JsonObject
            {
                ["anyOf"] = new JsonArray(
                    new JsonObject { ["type"] = "number" },
                    new JsonObject { ["type"] = "string" },
                    new JsonObject { ["$ref"] = "#/$defs/distribution" }),
            };

            var distribution = new JsonObject
            {
                ["type"] = "object",
                ["description"] = "A probability distribution; which fields a family needs is checked when the model is built.",
                ["required"] = Strings(new[] { "type" }),
                ["properties"] = new JsonObject
                {
                    ["type"] = new JsonObject { ["enum"] = Strings(DistributionTypes) },
                    ["mean"] = Number("exponential, lognormal, gamma"),
                    ["rate"] = Number("exponential, instead of mean"),
                    ["min"] = Number("triangular, uniform, empirical"),
                    ["mode"] = Number("triangular"),
                    ["max"] = Number("triangular, uniform, empirical"),
                    ["value"] = Number("constant"),
                    ["stddev"] = Number("lognormal, with mean"),
                    ["mu"] = Number("lognormal, mean of the underlying normal"),
                    ["sigma"] = Number("lognormal, standard deviation of the underlying normal"),
                    ["k"] = Number("gamma, shape"),
                    ["theta"] = Number("gamma, scale"),
                    ["shift"] = Number("added to every draw, any family"),
                    ["probabilities"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "number" },
                        ["description"] = "empirical: probabilities of equally spaced values from min to max, summing to 1",
                    },
                },
            };

            var node = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Strings(new[] { "id", "type" }),
                ["properties"] = new JsonObject
                {
                    ["id"] = new JsonObject { ["type"] = "string" },
                    ["type"] = new JsonObject { ["enum"] = Strings(NodeTypes.SelectMany(t => t.Aliases.Prepend(t.Name))) },
                    ["params"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["description"] = "Per node type, see node_types; a value is a number, a string or a distribution.",
                        ["additionalProperties"] = Value(),
                    },
                    ["steps"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["description"] = "A process node's steps, in order; see step_types.",
                        ["items"] = new JsonObject { ["$ref"] = "#/$defs/step" },
                    },
                },
            };

            var step = new JsonObject
            {
                ["type"] = "object",
                ["description"] = "A step of a process: its type, an optional id (else <type>#<n>), and the fields its type reads.",
                ["required"] = Strings(new[] { "type" }),
                ["properties"] = new JsonObject
                {
                    ["id"] = new JsonObject { ["type"] = "string" },
                    ["type"] = new JsonObject { ["enum"] = Strings(StepTypes.Select(t => t.Name)) },
                },
                ["additionalProperties"] = Value(),
            };

            var connection = new JsonObject
            {
                ["description"] = "{from, to, weight?} or [from, to] or [from, to, weight]; a weight is read only leaving a probabilistic router.",
                ["anyOf"] = new JsonArray(
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["required"] = Strings(new[] { "from", "to" }),
                        ["properties"] = new JsonObject
                        {
                            ["from"] = new JsonObject { ["type"] = "string" },
                            ["to"] = new JsonObject { ["type"] = "string" },
                            ["weight"] = new JsonObject { ["type"] = "number", ["exclusiveMinimum"] = 0 },
                            ["outlet"] = new JsonObject { ["type"] = "string", ["description"] = "The process outlet this connection leaves by." },
                        },
                    },
                    new JsonObject
                    {
                        ["type"] = "array",
                        ["prefixItems"] = new JsonArray(
                            new JsonObject { ["type"] = "string" },
                            new JsonObject { ["type"] = "string" },
                            new JsonObject { ["type"] = "number", ["exclusiveMinimum"] = 0 }),
                        ["minItems"] = 2,
                        ["maxItems"] = 3,
                    }),
            };

            var metric = new JsonObject
            {
                ["type"] = "object",
                ["required"] = Strings(new[] { "id", "kind" }),
                ["properties"] = new JsonObject
                {
                    ["id"] = new JsonObject { ["type"] = "string" },
                    ["kind"] = new JsonObject { ["enum"] = Strings(MetricKinds.Select(k => k.Name)) },
                    ["node"] = new JsonObject { ["type"] = "string" },
                },
            };

            return new JsonObject
            {
                ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
                ["title"] = "SimOpt topology, schema v" + SchemaVersion,
                ["type"] = "object",
                ["required"] = Strings(new[] { "nodes", "connections" }),
                ["properties"] = new JsonObject
                {
                    ["schema_version"] = new JsonObject { ["const"] = SchemaVersion },
                    ["name"] = new JsonObject { ["type"] = "string" },
                    ["seed"] = new JsonObject { ["type"] = "integer" },
                    ["nodes"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["$ref"] = "#/$defs/node" } },
                    ["connections"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["$ref"] = "#/$defs/connection" } },
                    ["metrics"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["$ref"] = "#/$defs/metric" } },
                },
                ["$defs"] = new JsonObject
                {
                    ["distribution"] = distribution,
                    ["node"] = node,
                    ["step"] = step,
                    ["connection"] = connection,
                    ["metric"] = metric,
                },
            };
        }
    }
}
