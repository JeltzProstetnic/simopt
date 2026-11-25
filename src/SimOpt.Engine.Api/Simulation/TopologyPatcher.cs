using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SimOpt.Engine.Api.Models;

namespace SimOpt.Engine.Api.Simulation
{
    /// <summary>
    /// SIM-66 step 5 (D9): applies a patch to a copy of a topology. Every operation is tried and every
    /// faulty one is reported (code <c>PATCH_*</c>, the operation's number in <c>value</c>); the copy is
    /// returned only when none was faulty, so a patch applies whole or not at all. Operations apply in
    /// order, so a later one sees what an earlier one added.
    /// </summary>
    public static class TopologyPatcher
    {
        public static readonly IReadOnlyList<string> Operations = new[]
        {
            "set", "add_node", "remove_node", "set_connection", "remove_connection", "set_metric", "remove_metric",
        };

        private static readonly JsonSerializerOptions Json = new();

        /// <summary>The JSON names of a distribution object's fields, which a set path may end in.</summary>
        private static readonly IReadOnlyList<string> DistributionFields = typeof(DistributionSpec)
            .GetProperties().Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
            .Where(n => n != null).Select(n => n!).ToList();

        /// <summary>A deep copy of a topology, through its JSON form.</summary>
        public static TopologyDefinition Copy(TopologyDefinition topology) =>
            JsonSerializer.Deserialize<TopologyDefinition>(JsonSerializer.Serialize(topology, Json), Json)!;

        /// <returns>The patched copy, or null when an operation was faulty (all of them are in <paramref name="report"/>).</returns>
        public static TopologyDefinition? Apply(TopologyDefinition topology, IReadOnlyList<PatchOperation> patch,
                                                ValidationReport report, List<string> changes)
        {
            TopologyDefinition copy = Copy(topology);
            bool faulty = false;
            for (int i = 0; i < patch.Count; i++)
            {
                PatchOperation op = patch[i] ?? new PatchOperation();
                try
                {
                    changes.AddRange(ApplyOne(copy, op));
                }
                catch (TopologyException e)
                {
                    faulty = true;
                    report.Errors.Add(e.Issue with { Message = $"Operation {i + 1} ({op.Op}): {e.Issue.Message}", Value = i + 1 });
                }
            }
            return faulty ? null : copy;
        }

        private static IEnumerable<string> ApplyOne(TopologyDefinition t, PatchOperation op)
        {
            switch ((op.Op ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "set": return new[] { Set(t, op) };
                case "add_node": return new[] { AddNode(t, op) };
                case "remove_node": return RemoveNode(t, op);
                case "set_connection": return new[] { SetConnection(t, op) };
                case "remove_connection": return new[] { RemoveConnection(t, op) };
                case "set_metric": return new[] { SetMetric(t, op) };
                case "remove_metric": return new[] { RemoveMetric(t, op) };
                default:
                    throw Fault("PATCH_UNKNOWN_OP", null, $"\"{op.Op}\" is not an operation.",
                        $"Use one of: {string.Join(", ", Operations)}.");
            }
        }

        // ── set ─────────────────────────────────────────────────────────────

        private static string Set(TopologyDefinition t, PatchOperation op)
        {
            string path = (op.Path ?? string.Empty).Trim();
            string[] segments = path.Split('.');
            if (path.Length == 0 || segments.Any(s => s.Length == 0))
                throw Fault("PATCH_BAD_PATH", null, $"path \"{op.Path}\" is not a path.",
                    "Write \"name\", \"seed\", \"<node>.<param>\" or \"<node>.<param>.<field>\".");

            if (segments.Length == 1) return SetTopLevel(t, path, op.Value);

            // a node id may itself contain dots: take the longest prefix that names a node
            NodeDefinition? node = null;
            int taken = 0;
            for (int k = segments.Length - 1; k >= 1 && node == null; k--)
            {
                string id = string.Join(".", segments.Take(k));
                node = t.Nodes.FirstOrDefault(n => n.Id == id);
                taken = k;
            }
            if (node == null)
                throw Fault("PATCH_BAD_PATH", null, $"path \"{path}\" names no node of this model.",
                    $"Its nodes are {string.Join(", ", t.Nodes.Select(n => $"'{n.Id}'"))}.");

            string[] rest = segments.Skip(taken).ToArray();
            if (rest[0] == "steps" && node.Steps != null) return SetStep(node, rest.Skip(1).ToArray(), path, op.Value);   // SIM-126
            if (rest.Length == 1) return SetParameter(node, rest[0], op.Value);
            if (rest.Length == 2) return SetField(node, rest[0], rest[1], op.Value);
            throw Fault("PATCH_BAD_PATH", node.Id, $"path \"{path}\" goes deeper than a field of a distribution.",
                "Write \"<node>.<param>\" or \"<node>.<param>.<field>\".");
        }

        private static string SetTopLevel(TopologyDefinition t, string path, JsonElement? value)
        {
            switch (path)
            {
                case "name" when value?.ValueKind == JsonValueKind.String:
                    string oldName = t.Name;
                    t.Name = value.Value.GetString()!;
                    return $"name: \"{oldName}\" → \"{t.Name}\"";
                case "seed" when value?.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out int seed):
                    int oldSeed = t.Seed;
                    t.Seed = seed;
                    return $"seed: {oldSeed} → {seed}";
                case "name":
                case "seed":
                    throw Fault("PATCH_BAD_VALUE", null, $"{path} needs {(path == "name" ? "a string" : "a whole number")}, not {Show(value)}.");
                default:
                    throw Fault("PATCH_BAD_PATH", null, $"path \"{path}\" names neither name, seed nor a node parameter.",
                        "Write \"name\", \"seed\", \"<node>.<param>\" or \"<node>.<param>.<field>\".");
            }
        }

        private static string SetParameter(NodeDefinition node, string param, JsonElement? value) =>
            SetValue(node.Params, node.Id, $"{node.Id}.{param}", param, value);

        private static string SetField(NodeDefinition node, string param, string field, JsonElement? value) =>
            SetDistributionField(node.Params, node.Id, $"'{node.Id}'", $"{node.Id}.{param}", param, field, value);

        /// <summary>
        /// SIM-126 schema, slice 5: <c>&lt;node&gt;.steps.&lt;step id&gt;.&lt;field&gt;[.&lt;distribution field&gt;]</c>,
        /// a step named as the builder names it (its id, or <c>&lt;type&gt;#&lt;n&gt;</c>); an id may contain dots.
        /// </summary>
        private static string SetStep(NodeDefinition node, string[] rest, string path, JsonElement? value)
        {
            IReadOnlyList<string> ids = SchemaCatalog.StepIds(node.Steps!);
            for (int k = rest.Length - 1; k >= 1; k--)
            {
                int at = ids.ToList().IndexOf(string.Join(".", rest.Take(k)));
                if (at < 0) continue;
                StepDefinition step = node.Steps![at];
                string[] field = rest.Skip(k).ToArray();
                string label = $"{node.Id}.steps.{ids[at]}.{field[0]}";
                if (field.Length == 1) return SetValue(step.Fields, node.Id, label, field[0], value);
                if (field.Length == 2)
                    return SetDistributionField(step.Fields, node.Id, $"step '{ids[at]}' of '{node.Id}'", label, field[0], field[1], value);
                break;
            }
            throw Fault("PATCH_BAD_PATH", node.Id, $"path \"{path}\" names no field of a step of process '{node.Id}'.",
                $"Write \"{node.Id}.steps.<step>.<field>\" or \"{node.Id}.steps.<step>.<field>.<distribution field>\"; " +
                $"its steps are {string.Join(", ", ids.Select(i => $"'{i}'"))}.");
        }

        private static string SetValue(Dictionary<string, ParamValue> values, string node, string label, string param, JsonElement? value)
        {
            values.TryGetValue(param, out ParamValue? old);
            if (value == null || value.Value.ValueKind == JsonValueKind.Null)
            {
                values.Remove(param);
                return old == null ? $"{label}: not set, nothing removed" : $"{label}: {old} → removed";
            }
            ParamValue parameter = value.Value.ValueKind switch
            {
                JsonValueKind.Number => value.Value.GetDouble(),
                JsonValueKind.String => value.Value.GetString()!,
                JsonValueKind.Object => Read<DistributionSpec>(value.Value, node, "a distribution object"),
                _ => throw Fault("PATCH_BAD_VALUE", node,
                    $"{label} takes a number, a string or a distribution object, not {Show(value)}."),
            };
            values[param] = parameter;
            return $"{label}: {old?.ToString() ?? "not set"} → {parameter}";
        }

        private static string SetDistributionField(Dictionary<string, ParamValue> values, string node, string owner, string label,
                                                   string param, string field, JsonElement? value)
        {
            if (!values.TryGetValue(param, out ParamValue? old) || old?.Distribution == null)
                throw Fault("PATCH_BAD_PATH", node, $"{owner} has no distribution '{param}' whose field could be set" +
                                                   (old == null ? "." : $"; it is {old}."),
                    $"Set the whole parameter with the path \"{label}\" and a distribution object.");
            if (!DistributionFields.Contains(field))
                throw Fault("PATCH_BAD_PATH", node, $"a distribution has no field '{field}'.",
                    $"Its fields are {string.Join(", ", DistributionFields)}.");

            JsonObject spec = JsonSerializer.SerializeToNode(old.Distribution, Json)!.AsObject();
            if (value == null || value.Value.ValueKind == JsonValueKind.Null) spec.Remove(field);
            else spec[field] = JsonNode.Parse(value.Value.GetRawText());
            DistributionSpec changed = Read<DistributionSpec>(spec, node, $"a value for the field '{field}'");
            values[param] = changed;
            return $"{label}.{field}: {old} → {(ParamValue)changed}";
        }

        // ── nodes ───────────────────────────────────────────────────────────

        private static string AddNode(TopologyDefinition t, PatchOperation op)
        {
            if (op.Value?.ValueKind != JsonValueKind.Object)
                throw Fault("PATCH_BAD_VALUE", null, "add_node needs the node as its value: {\"id\", \"type\", \"params\"}.");
            NodeDefinition node = Read<NodeDefinition>(op.Value.Value, null, "a node {\"id\", \"type\", \"params\"}");
            if (string.IsNullOrWhiteSpace(node.Id))
                throw Fault("PATCH_BAD_VALUE", null, "the node to add has no id.");
            if (t.Nodes.Any(n => n.Id == node.Id))
                throw Fault("PATCH_NODE_EXISTS", node.Id, $"a node '{node.Id}' exists already.",
                    $"Choose another id, or change '{node.Id}' with set.");
            t.Nodes.Add(node);
            return $"added {node.Type} '{node.Id}'";
        }

        private static IEnumerable<string> RemoveNode(TopologyDefinition t, PatchOperation op)
        {
            NodeDefinition node = NodeNamed(t, op.Node, "node");
            t.Nodes.Remove(node);
            var changes = new List<string> { $"removed {node.Type} '{node.Id}'" };
            foreach (ConnectionDefinition c in t.Connections.Where(c => c.From == node.Id || c.To == node.Id).ToList())
            {
                t.Connections.Remove(c);
                changes.Add($"removed connection '{c.From}' → '{c.To}' with it");
            }
            foreach (MetricDefinition m in t.Metrics.Where(m => m.Node == node.Id).ToList())
            {
                t.Metrics.Remove(m);
                changes.Add($"removed metric '{m.Id}' with it");
            }
            return changes;
        }

        // ── connections ─────────────────────────────────────────────────────

        private static string SetConnection(TopologyDefinition t, PatchOperation op)
        {
            NodeDefinition from = NodeNamed(t, op.From, "from");
            NodeDefinition to = NodeNamed(t, op.To, "to");
            // SIM-126: a process's outlet connection is another connection than its normal exit to the same node
            ConnectionDefinition? existing = t.Connections.FirstOrDefault(c => c.From == from.Id && c.To == to.Id && c.Outlet == op.Outlet);
            string weight = op.Weight is double w ? $" (weight {w.ToString(CultureInfo.InvariantCulture)})" : "";
            string outlet = op.Outlet == null ? "" : $" by outlet '{op.Outlet}'";
            if (existing != null)
            {
                existing.Weight = op.Weight;
                return $"connection '{from.Id}' → '{to.Id}'{outlet}{weight} kept, weight set";
            }
            t.Connections.Add(new ConnectionDefinition { From = from.Id, To = to.Id, Weight = op.Weight, Outlet = op.Outlet });
            return $"added connection '{from.Id}' → '{to.Id}'{outlet}{weight}";
        }

        private static string RemoveConnection(TopologyDefinition t, PatchOperation op)
        {
            ConnectionDefinition? existing = t.Connections.FirstOrDefault(c => c.From == op.From && c.To == op.To && c.Outlet == op.Outlet);
            string outlet = op.Outlet == null ? "" : $" by outlet '{op.Outlet}'";
            if (existing == null)
                throw Fault("PATCH_NO_SUCH_CONNECTION", op.From, $"there is no connection '{op.From}' → '{op.To}'{outlet}.");
            t.Connections.Remove(existing);
            return $"removed connection '{op.From}' → '{op.To}'{outlet}";
        }

        // ── metrics ─────────────────────────────────────────────────────────

        private static string SetMetric(TopologyDefinition t, PatchOperation op)
        {
            if (op.Value?.ValueKind != JsonValueKind.Object)
                throw Fault("PATCH_BAD_VALUE", null, "set_metric needs the metric as its value: {\"id\", \"kind\", \"node\"?}.");
            MetricDefinition metric = Read<MetricDefinition>(op.Value.Value, null, "a metric {\"id\", \"kind\", \"node\"?}");
            if (string.IsNullOrWhiteSpace(metric.Id))
                throw Fault("PATCH_BAD_VALUE", null, "the metric has no id.");
            int at = t.Metrics.FindIndex(m => m.Id == metric.Id);
            if (at >= 0)
            {
                t.Metrics[at] = metric;
                return $"metric '{metric.Id}' replaced: {metric.Kind}{(metric.Node == null ? "" : $" at '{metric.Node}'")}";
            }
            t.Metrics.Add(metric);
            return $"added metric '{metric.Id}': {metric.Kind}{(metric.Node == null ? "" : $" at '{metric.Node}'")}";
        }

        private static string RemoveMetric(TopologyDefinition t, PatchOperation op)
        {
            int at = t.Metrics.FindIndex(m => m.Id == op.Id);
            if (at < 0)
                throw Fault("PATCH_NO_SUCH_METRIC", null, $"there is no metric '{op.Id}'.",
                    t.Metrics.Count == 0 ? null : $"Its metrics are {string.Join(", ", t.Metrics.Select(m => $"'{m.Id}'"))}.");
            t.Metrics.RemoveAt(at);
            return $"removed metric '{op.Id}'";
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static NodeDefinition NodeNamed(TopologyDefinition t, string? id, string field) =>
            t.Nodes.FirstOrDefault(n => n.Id == id)
            ?? throw Fault("PATCH_UNKNOWN_NODE", null,
                id == null ? $"{field} is missing." : $"{field} '{id}' is not a node of this model.",
                $"Its nodes are {string.Join(", ", t.Nodes.Select(n => $"'{n.Id}'"))}.");

        private static T Read<T>(JsonElement value, string? node, string what) => Read<T>(JsonNode.Parse(value.GetRawText())!, node, what);

        private static T Read<T>(JsonNode value, string? node, string what)
        {
            try
            {
                return value.Deserialize<T>(Json) ?? throw new JsonException("null");
            }
            catch (JsonException e)
            {
                throw Fault("PATCH_BAD_VALUE", node, $"the value is not {what}: {e.Message}");
            }
        }

        private static string Show(JsonElement? value) => value == null ? "nothing" : value.Value.GetRawText();

        private static TopologyException Fault(string code, string? node, string message, string? suggestion = null) =>
            new TopologyException(code, node, message, suggestion);
    }
}
