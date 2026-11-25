using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.Engine.Api.Models;

namespace SimOpt.Visualization.Models;

/// <summary>Where and how the viewer draws one node; no part of the model.</summary>
public sealed record NodeLayout(double? X, double? Y, double? Width, double? Height, string? Label, string? Color);

/// <summary>A model as the schema builder reads it, and how the viewer draws it, by node id.</summary>
public sealed record VizDocument(TopologyDefinition Topology, IReadOnlyDictionary<string, NodeLayout> Layout);

/// <summary>
/// SIM-110 step 3 (Q-VIEW-PATH, <c>docs/plans/2026-11-25-sim-110-one-model-path.md</c>): a
/// <see cref="VizTopology"/> as a schema v1 document plus a layout overlay, so every model the viewer shows
/// is built by the one schema builder (UN-004) and follows the model's seed (UN-009).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>source</c>: arrivals <c>normal</c> (mean <c>mean_interval</c>, default 2; stddev <c>stddev</c>,
/// default 15 % of the mean), or <c>constant</c> without a spread.</item>
/// <item><c>buffer</c>: a queue of the same capacity.</item>
/// <item><c>server</c>: a station with a <c>constant</c> service (<c>service_time</c>, default 1); with a
/// <c>reject_rate</c> r it is followed by a probabilistic router sending r to the sink marked
/// <c>is_reject_target</c> and the rest, split equally, to its other targets.</item>
/// <item><c>roland</c>: a station serving <c>batch_size</c> items (default 15) together for batch_size ×
/// <c>per_piece_time</c>.</item>
/// <item>A source or station pushing into a station gets a queue between them (<c>&lt;from&gt;_to_&lt;to&gt;</c>),
/// and a station pushing into several nodes a router splitting equally (<c>&lt;id&gt;_split</c>), as the
/// schema requires; the old builder pushed straight into the server, which lost the items it was offered
/// while busy (SIM-135), and duplicated an item into every target.</item>
/// </list>
/// Each queue is measured by its wait and length, each station by its busy share, each sink by its count.
/// </remarks>
public static class VizSchema
{
    public static VizDocument ToSchema(VizTopology viz)
    {
        if (viz == null) throw new ArgumentNullException(nameof(viz));
        var t = new TopologyDefinition { SchemaVersion = SchemaCatalog.SchemaVersion, Name = viz.Name, Seed = viz.Seed };
        var layout = new Dictionary<string, NodeLayout>();
        var type = new Dictionary<string, string>();
        foreach (VizNode node in viz.Nodes)
        {
            NodeDefinition converted = Convert(node);
            type[node.Id] = converted.Type;
            t.Nodes.Add(converted);
            layout[node.Id] = new NodeLayout(node.X, node.Y, node.Width, node.Height, node.Label, node.Color);
        }

        VizNode? Viz(string id) => viz.Nodes.FirstOrDefault(n => n.Id == id);
        foreach (IGrouping<string, VizConnection> leaving in viz.Connections.GroupBy(c => c.From))
        {
            string from = leaving.Key;
            List<string> targets = leaving.Select(c => c.To).Distinct().ToList();
            double reject = Viz(from)?.Params.GetValueOrDefault("reject_rate", 0d) ?? 0d;
            if (type.GetValueOrDefault(from) == "station" && reject > 0d)
            {
                string router = Add(t, type, new NodeDefinition { Id = $"{from}_reject", Type = "router", Params = { ["policy"] = "probabilistic" } });
                t.Connections.Add(new ConnectionDefinition { From = from, To = router });
                List<string> rejects = targets.Where(id => Viz(id)?.Params.ContainsKey("is_reject_target") == true).ToList();
                List<string> passes = targets.Except(rejects).ToList();
                foreach (string target in rejects) Connect(t, type, router, target, reject / rejects.Count);
                foreach (string target in passes) Connect(t, type, router, target, (1d - reject) / passes.Count);
            }
            else if (type.GetValueOrDefault(from) is "station" or "source" && targets.Count > 1)
            {
                string router = Add(t, type, new NodeDefinition { Id = $"{from}_split", Type = "router", Params = { ["policy"] = "probabilistic" } });
                t.Connections.Add(new ConnectionDefinition { From = from, To = router });
                foreach (string target in targets) Connect(t, type, router, target, null);
            }
            else
            {
                foreach (string target in targets) Connect(t, type, from, target, null);
            }
        }

        foreach (NodeDefinition node in t.Nodes.ToList())
            switch (node.Type)
            {
                case "queue":
                    t.Metrics.Add(new MetricDefinition { Id = $"wait_{node.Id}", Kind = "wait_time", Node = node.Id });
                    t.Metrics.Add(new MetricDefinition { Id = $"length_{node.Id}", Kind = "queue_length", Node = node.Id });
                    break;
                case "station":
                    t.Metrics.Add(new MetricDefinition { Id = $"util_{node.Id}", Kind = "utilization", Node = node.Id });
                    break;
                case "sink":
                    t.Metrics.Add(new MetricDefinition { Id = $"count_{node.Id}", Kind = "count", Node = node.Id });
                    break;
            }
        return new VizDocument(t, layout);
    }

    /// <summary>
    /// SIM-110 step 4: what the viewer draws for a document: each of its nodes once, under the viewer's name
    /// for its kind (a queue is drawn as a buffer, a station as a server, an unknown kind as given), where its
    /// layout puts it, and each connection between two drawn nodes. A node the layout leaves out, such as a
    /// queue or router the conversion inserted, is placed by <see cref="AutoLayout"/>; on a floor plan it is
    /// drawn small, so it sits on the belt between the machines it joins rather than covering them.
    /// </summary>
    public static VizTopology Drawing(VizDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        TopologyDefinition t = document.Topology;
        bool plan = document.Layout.Values.Any(l => l.X.HasValue && l.Y.HasValue);
        var drawing = new VizTopology { Name = t.Name, Seed = t.EffectiveSeed };
        var drawn = new HashSet<string>();
        foreach (NodeDefinition node in t.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || !drawn.Add(node.Id)) continue;
            string kind = SchemaCatalog.Canonical(node.Type) switch
            {
                "queue" => "buffer",
                "station" => "server",
                string canonical => canonical,
                null => node.Type?.Trim() ?? string.Empty,
            };
            var shown = new VizNode { Id = node.Id, Type = kind };
            if (document.Layout.TryGetValue(node.Id, out NodeLayout? layout))
            {
                (shown.X, shown.Y, shown.Width, shown.Height) = (layout.X, layout.Y, layout.Width, layout.Height);
                (shown.Label, shown.Color) = (layout.Label, layout.Color);
            }
            else if (plan)
            {
                (shown.Width, shown.Height) = kind switch { "buffer" => (2d, 1.5d), "router" => (1.2d, 1.2d), _ => (3d, 2d) };
                shown.Label = string.Empty;   // too small for its id; the stats name it by id
            }
            drawing.Nodes.Add(shown);
        }
        foreach (ConnectionDefinition c in t.Connections)
            if (c.From != null && c.To != null && drawn.Contains(c.From) && drawn.Contains(c.To))
                drawing.Connections.Add(new VizConnection { From = c.From, To = c.To });
        return drawing;
    }

    private static NodeDefinition Convert(VizNode node)
    {
        var p = node.Params;
        switch (node.Type.Trim().ToLowerInvariant())
        {
            case "source":
            {
                double mean = p.GetValueOrDefault("mean_interval", 2.0);
                double stddev = p.GetValueOrDefault("stddev", mean * 0.15);
                DistributionSpec arrival = stddev > 0d
                    ? new DistributionSpec { Type = "normal", Mean = mean, Stddev = stddev }
                    : new DistributionSpec { Type = "constant", Value = mean };
                return new NodeDefinition { Id = node.Id, Type = "source", Params = { ["arrival"] = arrival } };
            }
            case "buffer":
            {
                var queue = new NodeDefinition { Id = node.Id, Type = "queue" };
                if (p.TryGetValue("capacity", out double capacity) && capacity < int.MaxValue) queue.Params["capacity"] = capacity;
                return queue;
            }
            case "server":
                return new NodeDefinition
                {
                    Id = node.Id, Type = "station",
                    Params = { ["service"] = new DistributionSpec { Type = "constant", Value = p.GetValueOrDefault("service_time", 1.0) } },
                };
            case "roland":
            {
                int batch = (int)p.GetValueOrDefault("batch_size", 15);
                double perPiece = p.GetValueOrDefault("per_piece_time", p.GetValueOrDefault("service_time", 1.0));
                return new NodeDefinition
                {
                    Id = node.Id, Type = "station",
                    Params = { ["batch"] = batch, ["service"] = new DistributionSpec { Type = "constant", Value = batch * perPiece } },
                };
            }
            case "sink":
                return new NodeDefinition { Id = node.Id, Type = "sink" };
            default:
                throw new ArgumentException($"Node '{node.Id}': the viewer has no node type '{node.Type}'.");
        }
    }

    /// <summary>A connection, with a queue between when a push would reach a station directly.</summary>
    private static void Connect(TopologyDefinition t, Dictionary<string, string> type, string from, string to, double? weight)
    {
        if (type.GetValueOrDefault(to) == "station" && type.GetValueOrDefault(from) != "queue")
        {
            string queue = Add(t, type, new NodeDefinition { Id = $"{from}_to_{to}", Type = "queue" });
            t.Connections.Add(new ConnectionDefinition { From = from, To = queue, Weight = weight });
            t.Connections.Add(new ConnectionDefinition { From = queue, To = to });
            return;
        }
        t.Connections.Add(new ConnectionDefinition { From = from, To = to, Weight = weight });
    }

    private static string Add(TopologyDefinition t, Dictionary<string, string> type, NodeDefinition node)
    {
        t.Nodes.Add(node);
        type[node.Id] = node.Type;
        return node.Id;
    }
}
