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

        // SIM-110 step 4: a node without a place on the plan (a queue the schema puts between two
        // machines) sits at the mean of the placed nodes it is connected to, found in rounds so a chain of
        // such nodes is placed from its ends inwards; one connected to nothing placed sits in the middle
        var unplaced = topology.Nodes.Where(n => !positions.ContainsKey(n.Id)).Select(n => n.Id).ToHashSet();
        while (unplaced.Count > 0)
        {
            var round = new Dictionary<string, Point>();
            foreach (string id in unplaced)
            {
                var placed = topology.Connections
                    .Select(c => c.From == id ? c.To : c.To == id ? c.From : null)
                    .Where(other => other != null && positions.ContainsKey(other))
                    .Select(other => positions[other!])
                    .ToList();
                if (placed.Count > 0)
                    round[id] = new Point(placed.Average(p => p.X), placed.Average(p => p.Y));
            }
            if (round.Count == 0) break;
            foreach (var (id, at) in round)
            {
                positions[id] = at;
                unplaced.Remove(id);
            }
        }
        foreach (string id in unplaced)
            positions[id] = new Point(cw / 2, ch / 2);
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

        // SIM-108: a node's layer is the longest way to it from the nodes nothing feeds, over the graph
        // without its back edges (an edge into a node still being explored: a rework loop). The old
        // recursion followed a loop with an ever larger depth until the stack overflowed. Nodes are found
        // depth first, in the order the old recursion first reached them, so an acyclic graph lands where
        // it did before.
        var found = new List<string>();      // in discovery order: the order within a layer
        var finished = new List<string>();   // post-order; reversed, a topological order without back edges
        var seen = new HashSet<string>();
        var onPath = new HashSet<string>();
        var backEdges = new HashSet<(string, string)>();
        void Explore(string root)
        {
            if (!seen.Add(root)) return;
            found.Add(root);
            onPath.Add(root);
            var stack = new Stack<(string Node, int Next)>();
            stack.Push((root, 0));
            while (stack.Count > 0)
            {
                var (node, next) = stack.Pop();
                List<string> outs = outEdges[node];
                if (next == outs.Count)
                {
                    onPath.Remove(node);
                    finished.Add(node);
                    continue;
                }
                stack.Push((node, next + 1));
                string child = outs[next];
                if (onPath.Contains(child)) backEdges.Add((node, child));
                else if (seen.Add(child))
                {
                    found.Add(child);
                    onPath.Add(child);
                    stack.Push((child, 0));
                }
            }
        }

        var sources = topology.Nodes.Where(n => inEdges[n.Id].Count == 0).ToList();
        if (sources.Count == 0) sources = new List<VizNode> { topology.Nodes[0] };
        foreach (var src in sources) Explore(src.Id);
        foreach (var node in topology.Nodes) Explore(node.Id);   // a loop nothing outside it feeds

        var layers = found.ToDictionary(id => id, _ => 0);
        for (int k = finished.Count - 1; k >= 0; k--)
        {
            string from = finished[k];
            foreach (string to in outEdges[from])
                if (!backEdges.Contains((from, to)))
                    layers[to] = Math.Max(layers[to], layers[from] + 1);
        }

        int maxLayer = layers.Values.DefaultIfEmpty(0).Max();
        var layerGroups = new List<List<string>>();
        for (int i = 0; i <= maxLayer; i++)
            layerGroups.Add(found.Where(id => layers[id] == i).ToList());

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
