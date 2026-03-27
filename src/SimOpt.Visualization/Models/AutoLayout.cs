using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace SimOpt.Visualization.Models;

/// <summary>
/// Computes node positions for visualization from a topology description.
/// Uses topological sort to assign layers, then distributes vertically within layers.
/// </summary>
public static class AutoLayout
{
    public const double NodeWidth = 130;
    public const double NodeHeight = 65;
    public const double HSpacing = 110;
    public const double VSpacing = 85;

    /// <summary>
    /// Compute positions for all nodes in the topology.
    /// Returns a dictionary of node id → center position.
    /// </summary>
    public static Dictionary<string, Point> Compute(VizTopology topology, double canvasWidth, double canvasHeight)
    {
        var positions = new Dictionary<string, Point>();
        if (topology.Nodes.Count == 0) return positions;

        // Build adjacency for topological layering
        var outEdges = new Dictionary<string, List<string>>();
        var inEdges = new Dictionary<string, List<string>>();
        foreach (var node in topology.Nodes)
        {
            outEdges[node.Id] = new List<string>();
            inEdges[node.Id] = new List<string>();
        }
        foreach (var conn in topology.Connections)
        {
            outEdges[conn.From].Add(conn.To);
            inEdges[conn.To].Add(conn.From);
        }

        // Assign layers via longest-path from sources
        var layers = new Dictionary<string, int>();
        var visited = new HashSet<string>();

        void AssignLayer(string nodeId, int depth)
        {
            if (layers.TryGetValue(nodeId, out int current) && current >= depth)
                return;
            layers[nodeId] = depth;
            foreach (var next in outEdges[nodeId])
                AssignLayer(next, depth + 1);
        }

        // Start from sources (no incoming edges)
        var sources = topology.Nodes.Where(n => inEdges[n.Id].Count == 0).ToList();
        if (sources.Count == 0)
            sources = new List<VizNode> { topology.Nodes[0] }; // fallback

        foreach (var src in sources)
            AssignLayer(src.Id, 0);

        // Assign unvisited nodes (disconnected) to layer 0
        foreach (var node in topology.Nodes)
            layers.TryAdd(node.Id, 0);

        // Group by layer
        int maxLayer = layers.Values.DefaultIfEmpty(0).Max();
        var layerGroups = new List<List<string>>();
        for (int i = 0; i <= maxLayer; i++)
        {
            layerGroups.Add(layers.Where(kv => kv.Value == i)
                .Select(kv => kv.Key).ToList());
        }

        // Compute positions — center the whole layout
        int numLayers = layerGroups.Count;
        int maxNodesInLayer = layerGroups.Max(g => g.Count);

        double totalW = numLayers * NodeWidth + (numLayers - 1) * HSpacing;
        double startX = (canvasWidth - totalW) / 2 + NodeWidth / 2;

        for (int layer = 0; layer < numLayers; layer++)
        {
            var group = layerGroups[layer];
            double x = startX + layer * (NodeWidth + HSpacing);

            double totalH = group.Count * NodeHeight + (group.Count - 1) * VSpacing;
            double startY = (canvasHeight - totalH) / 2 + NodeHeight / 2;

            for (int i = 0; i < group.Count; i++)
            {
                double y = startY + i * (NodeHeight + VSpacing);
                positions[group[i]] = new Point(x, y);
            }
        }

        return positions;
    }
}
