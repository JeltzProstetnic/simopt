using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Visualization.Models;
using Xunit;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-108 (SIM-110 slice V1): the topological layout must survive cycles. Before the fix, a rework
/// loop sent <c>AssignLayer</c> round the loop with an ever larger depth until the stack overflowed and
/// killed the process.
/// </summary>
public class AutoLayoutTests
{
    private static VizTopology Graph(string[] nodes, params (string From, string To)[] edges) => new()
    {
        Name = "layout",
        Nodes = nodes.Select(id => new VizNode { Id = id, Type = "server" }).ToList(),
        Connections = edges.Select(e => new VizConnection { From = e.From, To = e.To }).ToList(),
    };

    private static Dictionary<string, double> Columns(VizTopology topology) =>
        AutoLayout.Compute(topology, 2000, 1000).ToDictionary(kv => kv.Key, kv => kv.Value.X);

    [Fact]
    public void A_rework_loop_is_laid_out_in_flow_order()
    {
        // source -> queue -> station -> inspection, and inspection sends rework back to the queue
        var x = Columns(Graph(new[] { "source", "queue", "station", "inspection", "sink" },
            ("source", "queue"), ("queue", "station"), ("station", "inspection"),
            ("inspection", "queue"), ("inspection", "sink")));

        x.Keys.Should().BeEquivalentTo(new[] { "source", "queue", "station", "inspection", "sink" });
        x["source"].Should().BeLessThan(x["queue"]);
        x["queue"].Should().BeLessThan(x["station"]);
        x["station"].Should().BeLessThan(x["inspection"]);
        x["inspection"].Should().BeLessThan(x["sink"]);
    }

    [Fact]
    public void A_graph_that_is_one_cycle_starts_at_the_first_node_listed()
    {
        var x = Columns(Graph(new[] { "a", "b", "c" }, ("a", "b"), ("b", "c"), ("c", "a")));

        x["a"].Should().BeLessThan(x["b"]);
        x["b"].Should().BeLessThan(x["c"]);
    }

    [Fact]
    public void A_self_loop_is_ignored()
    {
        var x = Columns(Graph(new[] { "a", "b" }, ("a", "a"), ("a", "b")));

        x["a"].Should().BeLessThan(x["b"]);
    }

    [Fact]
    public void A_node_sits_one_column_after_its_longest_way_in()
    {
        // a -> b -> c -> d and a short cut a -> d: d goes after c, not next to b
        var x = Columns(Graph(new[] { "a", "b", "c", "d" }, ("a", "b"), ("b", "c"), ("c", "d"), ("a", "d")));

        x["d"].Should().BeGreaterThan(x["c"]);
        x["b"].Should().BeGreaterThan(x["a"]);
        x["c"].Should().BeGreaterThan(x["b"]);
    }

    [Fact]
    public void An_acyclic_graph_is_placed_exactly_as_before()
    {
        // the presets laid out by topology, and a diamond with crossing paths
        var graphs = new[]
        {
            VizTopology.Sqss(), VizTopology.ParallelServers(), VizTopology.ProductionLine(),
            Graph(new[] { "s", "q1", "q2", "m1", "m2", "m3", "out" },
                ("s", "q2"), ("s", "q1"), ("q1", "m1"), ("q2", "m2"), ("q2", "m3"), ("m1", "out"), ("m3", "out"), ("m2", "q1")),
        };

        foreach (VizTopology graph in graphs)
        {
            var now = AutoLayout.Compute(graph, 2000, 1000);
            var before = LayoutBeforeSim108(graph, 2000, 1000);
            now.Should().BeEquivalentTo(before, $"'{graph.Name}' is acyclic");
        }
    }

    [Fact]
    public void On_a_floor_plan_a_node_without_a_place_sits_between_the_nodes_it_joins()
    {
        // SIM-110 step 4: the queue the schema puts between two stations has no place on the plan; it used
        // to land in the middle of the canvas, far from both
        var plan = new VizTopology
        {
            Name = "plan",
            Nodes = new List<VizNode>
            {
                new() { Id = "qc", Type = "server", X = 0, Y = 0, Width = 4, Height = 2 },
                new() { Id = "qc_to_packing", Type = "buffer" },
                new() { Id = "packing", Type = "server", X = 20, Y = 10, Width = 4, Height = 2 },
                new() { Id = "loose", Type = "router" },
            },
            Connections = new List<VizConnection> { new() { From = "qc", To = "qc_to_packing" }, new() { From = "qc_to_packing", To = "packing" } },
        };

        var at = AutoLayout.Compute(plan, 2000, 1000);

        at["qc_to_packing"].X.Should().BeApproximately((at["qc"].X + at["packing"].X) / 2, 1e-9);
        at["qc_to_packing"].Y.Should().BeApproximately((at["qc"].Y + at["packing"].Y) / 2, 1e-9);
        at["loose"].Should().Be(new Avalonia.Point(1000, 500), "a node joined to nothing placed stays in the middle");
    }

    /// <summary>The layering of AutoLayout before SIM-108, verbatim; it never ends on a cycle.</summary>
    private static Dictionary<string, Avalonia.Point> LayoutBeforeSim108(VizTopology topology, double cw, double ch)
    {
        var positions = new Dictionary<string, Avalonia.Point>();
        var outEdges = topology.Nodes.ToDictionary(n => n.Id, _ => new List<string>());
        var inEdges = topology.Nodes.ToDictionary(n => n.Id, _ => new List<string>());
        foreach (var conn in topology.Connections)
        {
            outEdges[conn.From].Add(conn.To);
            inEdges[conn.To].Add(conn.From);
        }

        var layers = new Dictionary<string, int>();
        void AssignLayer(string nodeId, int depth)
        {
            if (layers.TryGetValue(nodeId, out int current) && current >= depth) return;
            layers[nodeId] = depth;
            foreach (var next in outEdges[nodeId])
                AssignLayer(next, depth + 1);
        }

        var sources = topology.Nodes.Where(n => inEdges[n.Id].Count == 0).ToList();
        if (sources.Count == 0) sources = new List<VizNode> { topology.Nodes[0] };
        foreach (var src in sources) AssignLayer(src.Id, 0);
        foreach (var node in topology.Nodes) layers.TryAdd(node.Id, 0);

        int maxLayer = layers.Values.DefaultIfEmpty(0).Max();
        var layerGroups = new List<List<string>>();
        for (int i = 0; i <= maxLayer; i++)
            layerGroups.Add(layers.Where(kv => kv.Value == i).Select(kv => kv.Key).ToList());

        int numLayers = layerGroups.Count;
        double totalW = numLayers * AutoLayout.NodeWidth + (numLayers - 1) * AutoLayout.HSpacing;
        double startX = (cw - totalW) / 2 + AutoLayout.NodeWidth / 2;
        for (int layer = 0; layer < numLayers; layer++)
        {
            var group = layerGroups[layer];
            double x = startX + layer * (AutoLayout.NodeWidth + AutoLayout.HSpacing);
            double totalH = group.Count * AutoLayout.NodeHeight + (group.Count - 1) * AutoLayout.VSpacing;
            double startY = (ch - totalH) / 2 + AutoLayout.NodeHeight / 2;
            for (int i = 0; i < group.Count; i++)
                positions[group[i]] = new Avalonia.Point(x, startY + i * (AutoLayout.NodeHeight + AutoLayout.VSpacing));
        }
        return positions;
    }
}
