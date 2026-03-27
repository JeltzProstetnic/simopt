using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace SimOpt.Visualization.Models;

/// <summary>
/// Computes node positions for visualization.
/// Uses physical coordinates when available, falls back to topological auto-layout.
/// </summary>
public static class AutoLayout
{
    public const double NodeWidth = 130;
    public const double NodeHeight = 65;
    public const double HSpacing = 110;
    public const double VSpacing = 85;

    /// <summary>
    /// Compute positions for all nodes. Physical positions (meters) are converted to pixels.
    /// Nodes without physical positions use topological auto-layout.
    /// Returns node id → center position in pixels.
    /// </summary>
    public static Dictionary<string, Point> Compute(VizTopology topology, double canvasWidth, double canvasHeight)
    {
        var positions = new Dictionary<string, Point>();
        if (topology.Nodes.Count == 0) return positions;

        bool hasPhysical = topology.Nodes.Any(n => n.HasPhysicalPosition);

        if (hasPhysical)
            ComputePhysical(topology, canvasWidth, canvasHeight, positions);
        else
            ComputeTopological(topology, canvasWidth, canvasHeight, positions);

        return positions;
    }

    /// <summary>
    /// Returns node dimensions in pixels. Uses physical dimensions if available.
    /// </summary>
    public static Size GetNodeSize(VizNode node, double scale)
    {
        if (node.Width.HasValue && node.Height.HasValue)
            return new Size(node.Width.Value * scale, node.Height.Value * scale);
        return new Size(NodeWidth, NodeHeight);
    }

    private static void ComputePhysical(VizTopology topology, double cw, double ch,
        Dictionary<string, Point> positions)
    {
        // Find bounds of all physical nodes
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        foreach (var n in topology.Nodes.Where(n => n.HasPhysicalPosition))
        {
            double nx = n.X!.Value;
            double ny = n.Y!.Value;
            double nw = n.Width ?? 4;
            double nh = n.Height ?? 3;
            minX = Math.Min(minX, nx);
            maxX = Math.Max(maxX, nx + nw);
            minY = Math.Min(minY, ny);
            maxY = Math.Max(maxY, ny + nh);
        }

        double worldW = maxX - minX;
        double worldH = maxY - minY;
        if (worldW < 1) worldW = 1;
        if (worldH < 1) worldH = 1;

        // Scale to fit canvas with margin
        double marginX = 60, marginY = 70;
        double availW = cw - 2 * marginX;
        double availH = ch - marginY - 60; // top margin + bottom bar
        double scale = Math.Min(availW / worldW, availH / worldH);

        // Center
        double offsetX = marginX + (availW - worldW * scale) / 2;
        double offsetY = marginY + (availH - worldH * scale) / 2;

        foreach (var n in topology.Nodes)
        {
            if (n.HasPhysicalPosition)
            {
                double nw = n.Width ?? 4;
                double nh = n.Height ?? 3;
                double cx = (n.X!.Value - minX + nw / 2) * scale + offsetX;
                double cy = (n.Y!.Value - minY + nh / 2) * scale + offsetY;
                positions[n.Id] = new Point(cx, cy);
            }
        }

        // Fallback: any node without physical position gets placed at origin
        foreach (var n in topology.Nodes.Where(n => !n.HasPhysicalPosition))
        {
            if (!positions.ContainsKey(n.Id))
                positions[n.Id] = new Point(cw / 2, ch / 2);
        }
    }

    private static void ComputeTopological(VizTopology topology, double cw, double ch,
        Dictionary<string, Point> positions)
    {
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
        double totalW = numLayers * NodeWidth + (numLayers - 1) * HSpacing;
        double startX = (cw - totalW) / 2 + NodeWidth / 2;

        for (int layer = 0; layer < numLayers; layer++)
        {
            var group = layerGroups[layer];
            double x = startX + layer * (NodeWidth + HSpacing);
            double totalH = group.Count * NodeHeight + (group.Count - 1) * VSpacing;
            double startY = (ch - totalH) / 2 + NodeHeight / 2;
            for (int i = 0; i < group.Count; i++)
                positions[group[i]] = new Point(x, startY + i * (NodeHeight + VSpacing));
        }
    }

    /// <summary>
    /// Compute the physical scale factor (pixels per meter) for the current layout.
    /// </summary>
    public static double ComputeScale(VizTopology topology, double canvasWidth, double canvasHeight)
    {
        if (!topology.Nodes.Any(n => n.HasPhysicalPosition)) return 1.0;

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        foreach (var n in topology.Nodes.Where(n => n.HasPhysicalPosition))
        {
            minX = Math.Min(minX, n.X!.Value);
            maxX = Math.Max(maxX, n.X!.Value + (n.Width ?? 4));
            minY = Math.Min(minY, n.Y!.Value);
            maxY = Math.Max(maxY, n.Y!.Value + (n.Height ?? 3));
        }

        double worldW = Math.Max(1, maxX - minX);
        double worldH = Math.Max(1, maxY - minY);
        double marginX = 60, marginY = 70;
        return Math.Min((canvasWidth - 2 * marginX) / worldW, (canvasHeight - marginY - 60) / worldH);
    }
}
