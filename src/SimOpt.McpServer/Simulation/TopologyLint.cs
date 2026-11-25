using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-66 step 2 (D4): structural faults of a topology, found from its graph alone before anything
    /// is run (UN-008). Errors where the model cannot be what was meant; warnings where it runs but
    /// loses or strands items. One fault gives one issue, at the node where it is: a node without
    /// connections is reported as unconnected only, and a dead branch at the node where items stop.
    /// </summary>
    /// <remarks>
    /// Nodes without an id, with a duplicated id or with an unknown type, and connections to nodes
    /// that do not exist, are left to the builder, which reports them.
    /// </remarks>
    public static class TopologyLint
    {
        /// <summary>Node types that push each item they release into every node connected after them.</summary>
        private static readonly HashSet<string> PushTypes = new() { "source", "station", "delay" };

        public static void Check(TopologyDefinition topology, ValidationReport report)
        {
            var types = KnownNodes(topology);
            var order = types.Keys.ToList();
            var outs = order.ToDictionary(id => id, _ => new List<string>());
            var ins = order.ToDictionary(id => id, _ => new List<string>());

            var wired = new HashSet<(string, string)>();
            var twice = new HashSet<(string, string)>();
            foreach (ConnectionDefinition c in topology.Connections)
            {
                if (c.From == null || c.To == null || !types.ContainsKey(c.From) || !types.ContainsKey(c.To)) continue;
                if (!wired.Add((c.From, c.To)))
                {
                    if (twice.Add((c.From, c.To)))
                        report.Errors.Add(new ValidationIssue("DUPLICATE_CONNECTION",
                            $"Connection '{c.From}' → '{c.To}' is listed more than once; every item would be passed on once per listing.",
                            c.From, "Remove the repeated connection."));
                    continue;
                }
                outs[c.From].Add(c.To);
                ins[c.To].Add(c.From);
            }

            List<string> sources = order.Where(id => types[id] == "source").ToList();
            List<string> sinks = order.Where(id => types[id] == "sink").ToList();

            if (sources.Count == 0)
                report.Errors.Add(new ValidationIssue("NO_SOURCE", "The model has no source, so no item ever enters it.",
                    Suggestion: "Add a source and connect it to the first queue."));

            foreach (string id in order.Where(id => PushTypes.Contains(types[id]) && outs[id].Count > 1))
            {
                string targets = string.Join(", ", outs[id].Select(t => $"'{t}'"));
                report.Errors.Add(new ValidationIssue("FAN_OUT_WITHOUT_ROUTER",
                    $"{Capital(types[id])} '{id}' has {outs[id].Count} connections leaving it ({targets}); every item " +
                    "would be put into each of them, duplicated.",
                    id, $"Connect '{id}' to a router (policy probabilistic or shortest_queue) and the router to {targets}."));
            }

            var unconnected = order.Where(id => outs[id].Count == 0 && ins[id].Count == 0).ToHashSet();
            foreach (string id in order.Where(unconnected.Contains))
                report.Warnings.Add(new ValidationIssue("UNCONNECTED", $"{Capital(types[id])} '{id}' has no connections.",
                    id, "Connect it, or remove it."));

            if (sinks.Count == 0)
                report.Warnings.Add(new ValidationIssue("NO_SINK",
                    "The model has no sink, so every item stays in the system and nothing is counted as finished.",
                    Suggestion: "Add a sink and connect the last station to it."));
            if (sources.Count == 0) return;   // reachability means nothing without a way in

            HashSet<string> reached = Walk(sources, outs);
            foreach (string id in order.Where(id => !reached.Contains(id) && !unconnected.Contains(id)))
                report.Warnings.Add(new ValidationIssue("UNREACHABLE",
                    $"{Capital(types[id])} '{id}' cannot be reached from any source, so no item ever gets there.",
                    id, $"Connect a node that items pass through to '{id}', or remove it."));
            if (sinks.Count == 0) return;     // NO_SINK already says where every item ends up

            var stops = new List<string>();
            foreach (string id in order.Where(id => reached.Contains(id) && types[id] != "sink" && outs[id].Count == 0
                                                    && !unconnected.Contains(id)))
            {
                stops.Add(id);
                report.Warnings.Add(types[id] == "queue"
                    ? new ValidationIssue("QUEUE_NOT_TAKEN",
                        $"Queue '{id}' feeds no station, so it only fills.", id, $"Connect '{id}' to the station that serves it.")
                    : new ValidationIssue("DEAD_END",
                        $"{Capital(types[id])} '{id}' has no connection leaving it, so the items reaching it stop there.",
                        id, $"Connect '{id}' onward, toward a sink."));
            }

            HashSet<string> leadOut = Walk(sinks.Concat(stops), ins);
            List<string> loop = order.Where(id => reached.Contains(id) && !leadOut.Contains(id)
                                                  && OnACycle(id, outs, n => reached.Contains(n) && !leadOut.Contains(n))).ToList();
            if (loop.Count > 0)
                report.Warnings.Add(new ValidationIssue("LOOP_WITHOUT_EXIT",
                    $"Items reaching {string.Join(", ", loop.Select(id => $"'{id}'"))} go round a loop with no way out " +
                    "and never reach a sink.",
                    loop[0], "Give the loop a way out: a router in it with a connection leading toward a sink."));
        }

        /// <summary>The nodes the lint reasons about, by id in declaration order, with their v1 type.</summary>
        private static Dictionary<string, string> KnownNodes(TopologyDefinition topology)
        {
            var twins = topology.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).GroupBy(n => n.Id)
                                .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var types = new Dictionary<string, string>();
            foreach (NodeDefinition node in topology.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id) || twins.Contains(node.Id)) continue;
                string? type = SchemaCatalog.Canonical(node.Type);
                if (type != null) types[node.Id] = type;
            }
            return types;
        }

        /// <summary>Every node reachable from <paramref name="start"/> along <paramref name="next"/>, the start included.</summary>
        private static HashSet<string> Walk(IEnumerable<string> start, IReadOnlyDictionary<string, List<string>> next)
        {
            var seen = new HashSet<string>(start);
            var open = new Queue<string>(seen);
            while (open.Count > 0)
                foreach (string n in next[open.Dequeue()])
                    if (seen.Add(n)) open.Enqueue(n);
            return seen;
        }

        /// <summary>Whether <paramref name="id"/> can reach itself through nodes <paramref name="within"/> accepts.</summary>
        private static bool OnACycle(string id, IReadOnlyDictionary<string, List<string>> outs, Func<string, bool> within)
        {
            var seen = new HashSet<string>();
            var open = new Queue<string>(outs[id].Where(within));
            while (open.Count > 0)
            {
                string n = open.Dequeue();
                if (n == id) return true;
                if (!seen.Add(n)) continue;
                foreach (string m in outs[n].Where(within)) open.Enqueue(m);
            }
            return false;
        }

        private static string Capital(string type) => char.ToUpperInvariant(type[0]) + type.Substring(1);
    }
}
