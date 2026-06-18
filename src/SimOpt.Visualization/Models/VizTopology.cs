using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimOpt.Visualization.Models;

/// <summary>
/// Describes a simulation network for visualization. Same JSON format as the MCP server.
/// Claude generates this, the visualization auto-renders it.
/// </summary>
public class VizTopology
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Model";

    [JsonPropertyName("seed")]
    public int Seed { get; set; } = 42;

    [JsonPropertyName("nodes")]
    public List<VizNode> Nodes { get; set; } = new();

    [JsonPropertyName("connections")]
    public List<VizConnection> Connections { get; set; } = new();

    /// <summary>
    /// Creates the classic SQSS (Source-Queue-Server-Sink) topology.
    /// </summary>
    public static VizTopology Sqss(int seed = 42) => new()
    {
        Name = "SQSS",
        Seed = seed,
        Nodes = new List<VizNode>
        {
            new() { Id = "source", Type = "source", Params = new() { ["mean_interval"] = 2.0 } },
            new() { Id = "queue", Type = "buffer", Params = new() { ["capacity"] = 15 } },
            new() { Id = "server", Type = "server", Params = new() { ["service_time"] = 1.8 } },
            new() { Id = "sink", Type = "sink" }
        },
        Connections = new List<VizConnection>
        {
            new() { From = "source", To = "queue" },
            new() { From = "queue", To = "server" },
            new() { From = "server", To = "sink" }
        }
    };

    /// <summary>
    /// Two parallel servers sharing one queue.
    /// </summary>
    public static VizTopology ParallelServers(int seed = 42) => new()
    {
        Name = "Parallel Servers",
        Seed = seed,
        Nodes = new List<VizNode>
        {
            new() { Id = "source", Type = "source", Params = new() { ["mean_interval"] = 1.0 } },
            new() { Id = "queue", Type = "buffer", Params = new() { ["capacity"] = 20 } },
            new() { Id = "server1", Type = "server", Params = new() { ["service_time"] = 1.8 } },
            new() { Id = "server2", Type = "server", Params = new() { ["service_time"] = 2.0 } },
            new() { Id = "sink", Type = "sink" }
        },
        Connections = new List<VizConnection>
        {
            new() { From = "source", To = "queue" },
            new() { From = "queue", To = "server1" },
            new() { From = "queue", To = "server2" },
            new() { From = "server1", To = "sink" },
            new() { From = "server2", To = "sink" }
        }
    };

    /// <summary>
    /// Two-stage production line with intermediate buffer.
    /// </summary>
    public static VizTopology ProductionLine(int seed = 42) => new()
    {
        Name = "Production Line",
        Seed = seed,
        Nodes = new List<VizNode>
        {
            new() { Id = "raw", Type = "source", Params = new() { ["mean_interval"] = 1.5 } },
            new() { Id = "buf1", Type = "buffer", Params = new() { ["capacity"] = 10 } },
            new() { Id = "stage1", Type = "server", Params = new() { ["service_time"] = 1.2 } },
            new() { Id = "buf2", Type = "buffer", Params = new() { ["capacity"] = 8 } },
            new() { Id = "stage2", Type = "server", Params = new() { ["service_time"] = 1.4 } },
            new() { Id = "finished", Type = "sink" }
        },
        Connections = new List<VizConnection>
        {
            new() { From = "raw", To = "buf1" },
            new() { From = "buf1", To = "stage1" },
            new() { From = "stage1", To = "buf2" },
            new() { From = "buf2", To = "stage2" },
            new() { From = "stage2", To = "finished" }
        }
    };

    /// <summary>
    /// Electronics assembly factory with physical layout (50m × 30m).
    /// Two docks → inspection → 3 parallel assembly lines → QC → packing → shipping.
    /// </summary>
    public static VizTopology FactoryFloor(int seed = 42) => new()
    {
        Name = "Electronics Assembly Factory",
        Seed = seed,
        Nodes = new List<VizNode>
        {
            // Receiving docks (top)
            new() { Id = "dock_a", Type = "source", Label = "Dock A\n(PCBs)",
                X = 8, Y = 2, Width = 4, Height = 3,
                Params = new() { ["mean_interval"] = 0.75 }, Color = "#4CAF50" },
            new() { Id = "dock_b", Type = "source", Label = "Dock B\n(Casings)",
                X = 20, Y = 2, Width = 4, Height = 3,
                Params = new() { ["mean_interval"] = 0.85 }, Color = "#2196F3" },

            // Incoming buffer
            new() { Id = "incoming", Type = "buffer", Label = "Incoming\nStaging",
                X = 14, Y = 6, Width = 5, Height = 3,
                Params = new() { ["capacity"] = 40 } },

            // Inspection — 5% reject rate
            new() { Id = "inspect", Type = "server", Label = "Inspection\n(5% reject)",
                X = 14, Y = 11, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 0.33, ["reject_rate"] = 0.05 } },

            // Pre-assembly buffer
            new() { Id = "staging", Type = "buffer", Label = "Assembly\nStaging",
                X = 14, Y = 15.5, Width = 5, Height = 3,
                Params = new() { ["capacity"] = 30 } },

            // 3 parallel assembly lines
            new() { Id = "asm1", Type = "server", Label = "Assembly\nLine 1",
                X = 5, Y = 20, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 1.5 }, Color = "#FF9800" },
            new() { Id = "asm2", Type = "server", Label = "Assembly\nLine 2",
                X = 14, Y = 20, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 1.5 }, Color = "#FF9800" },
            new() { Id = "asm3", Type = "server", Label = "Assembly\nLine 3",
                X = 23, Y = 20, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 1.5 }, Color = "#FF9800" },

            // QC buffer
            new() { Id = "qc_buf", Type = "buffer", Label = "QC Queue",
                X = 14, Y = 24.5, Width = 5, Height = 2,
                Params = new() { ["capacity"] = 20 } },

            // Quality control
            new() { Id = "qc", Type = "server", Label = "Quality\nControl",
                X = 14, Y = 28, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 0.5 } },

            // Packing
            new() { Id = "packing", Type = "server", Label = "Packing",
                X = 14, Y = 33, Width = 5, Height = 3,
                Params = new() { ["service_time"] = 0.25 } },

            // Shipping dock
            new() { Id = "shipping", Type = "sink", Label = "Shipping\nDock",
                X = 14, Y = 38, Width = 6, Height = 3,
                Color = "#F44336" },

            // Waste bin (reject target)
            new() { Id = "waste", Type = "sink", Label = "Waste",
                X = 28, Y = 11, Width = 3, Height = 2,
                Color = "#9E9E9E", Params = new() { ["is_reject_target"] = 1 } },
        },
        Connections = new List<VizConnection>
        {
            new() { From = "dock_a", To = "incoming" },
            new() { From = "dock_b", To = "incoming" },
            new() { From = "incoming", To = "inspect" },
            new() { From = "inspect", To = "waste" },
            new() { From = "inspect", To = "staging" },
            // Parallel assembly
            new() { From = "staging", To = "asm1" },
            new() { From = "staging", To = "asm2" },
            new() { From = "staging", To = "asm3" },
            new() { From = "asm1", To = "qc_buf" },
            new() { From = "asm2", To = "qc_buf" },
            new() { From = "asm3", To = "qc_buf" },
            // QC → Packing → Shipping
            new() { From = "qc_buf", To = "qc" },
            new() { From = "qc", To = "packing" },
            new() { From = "packing", To = "shipping" },
        }
    };

    /// <summary>
    /// Ivoclar Ivotion denture packing line (real production cell, 50m × 14m).
    /// 5-step manual+automated process: inspection/cleaning → Roland LEF UV print
    /// → packaging → labeling → SSB placement. Roland printer is the bottleneck
    /// (60s/pc, batch of 15 = 15min cycle). All other steps operator-bound.
    /// </summary>
    /// <summary>
    /// Parametric Ivotion packing line. Builds the topology from an optimized
    /// <see cref="SimOpt.Ivotion.IvotionSolution"/>: variable Roland count
    /// (1 or 2, stacked vertically per plan), per-solution batch size,
    /// operator-count labels, and effective-service-time splits
    /// (base_time / operator_count) per locked-in v1 model.
    /// </summary>
    public static VizTopology IvotionPacking(SimOpt.Ivotion.IvotionSolution sol, int seed = 42)
    {
        int rolands = sol.RolandCount;
        int batchSize = sol.RolandBatchSize;

        // Effective service times: base_time / operator_count (v1 parallelism model).
        double inspectTime = 1.0 / sol.OperatorsInspect;
        double packTime = 2.0 / sol.OperatorsPack;
        double labelTime = 1.2 / sol.OperatorsLabel;
        double ssbTime = 0.3 / sol.OperatorsSsb;

        var nodes = new List<VizNode>
        {
            new() { Id = "incoming", Type = "source", Label = "Incoming\nDentures",
                X = 1, Y = 5, Width = 4, Height = 4,
                Params = new() { ["mean_interval"] = 5.5 }, Color = "#E1BEE7" },

            new() { Id = "buf1", Type = "buffer", Label = "Queue 1",
                X = 6.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 20 } },
            new() { Id = "inspect", Type = "server",
                Label = $"1. Inspect\n& Clean\n({sol.OperatorsInspect} op)",
                X = 10.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = inspectTime }, Color = "#4FC3F7" },

            new() { Id = "buf2", Type = "buffer", Label = "Print\nQueue",
                X = 17, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 30 } },
        };

        // Variable Roland count (1 or 2). Stacked vertically when 2.
        if (rolands == 2)
        {
            nodes.Add(new VizNode
            {
                Id = "roland_a", Type = "roland",
                Label = $"2a. Roland LEF\nUV Print\n({batchSize}×60s)",
                X = 21, Y = 2.5, Width = 6, Height = 4,
                Params = new() { ["per_piece_time"] = 0.4, ["batch_size"] = batchSize },
                Color = "#FF6F00",
            });
            nodes.Add(new VizNode
            {
                Id = "roland_b", Type = "roland",
                Label = $"2b. Roland LEF\nUV Print\n({batchSize}×60s)",
                X = 21, Y = 7.5, Width = 6, Height = 4,
                Params = new() { ["per_piece_time"] = 0.4, ["batch_size"] = batchSize },
                Color = "#FF6F00",
            });
        }
        else
        {
            nodes.Add(new VizNode
            {
                Id = "roland", Type = "roland",
                Label = $"2. Roland LEF\nUV Print\n({batchSize}×60s)",
                X = 21, Y = 4.5, Width = 6, Height = 5,
                Params = new() { ["per_piece_time"] = 0.4, ["batch_size"] = batchSize },
                Color = "#FF6F00",
            });
        }

        nodes.AddRange(new[]
        {
            new VizNode { Id = "buf3", Type = "buffer", Label = "Pack\nQueue",
                X = 28.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 20 } },
            new VizNode { Id = "pack", Type = "server",
                Label = $"3. Manual\nPackaging\n({sol.OperatorsPack} op)",
                X = 32.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = packTime }, Color = "#66BB6A" },

            new VizNode { Id = "buf4", Type = "buffer", Label = "Label\nQueue",
                X = 39, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 15 } },
            new VizNode { Id = "label", Type = "server",
                Label = $"4. Labeling\n({sol.OperatorsLabel} op)",
                X = 43, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = labelTime }, Color = "#FFCA28" },

            new VizNode { Id = "buf5", Type = "buffer", Label = "SSB\nQueue",
                X = 49.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 10 } },
            new VizNode { Id = "ssb", Type = "server",
                Label = $"5. SSB\nPlacement\n({sol.OperatorsSsb} op)",
                X = 53.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = ssbTime }, Color = "#AB47BC" },

            new VizNode { Id = "shipped", Type = "sink", Label = "Shipped",
                X = 60, Y = 5, Width = 4, Height = 4,
                Color = "#43A047" },
        });

        var connections = new List<VizConnection>
        {
            new() { From = "incoming", To = "buf1" },
            new() { From = "buf1", To = "inspect" },
            new() { From = "inspect", To = "buf2" },
        };

        if (rolands == 2)
        {
            connections.Add(new VizConnection { From = "buf2", To = "roland_a" });
            connections.Add(new VizConnection { From = "buf2", To = "roland_b" });
            connections.Add(new VizConnection { From = "roland_a", To = "buf3" });
            connections.Add(new VizConnection { From = "roland_b", To = "buf3" });
        }
        else
        {
            connections.Add(new VizConnection { From = "buf2", To = "roland" });
            connections.Add(new VizConnection { From = "roland", To = "buf3" });
        }

        connections.AddRange(new[]
        {
            new VizConnection { From = "buf3", To = "pack" },
            new VizConnection { From = "pack", To = "buf4" },
            new VizConnection { From = "buf4", To = "label" },
            new VizConnection { From = "label", To = "buf5" },
            new VizConnection { From = "buf5", To = "ssb" },
            new VizConnection { From = "ssb", To = "shipped" },
        });

        return new VizTopology
        {
            Name = $"Ivoclar Ivotion ({rolands}× Roland, batch {batchSize})",
            Seed = seed,
            Nodes = nodes,
            Connections = connections,
        };
    }

    public static VizTopology IvotionPacking(int seed = 42) => new()
    {
        Name = "Ivoclar Ivotion Packing Line",
        Seed = seed,
        Nodes = new List<VizNode>
        {
            // Incoming dentures from production
            new() { Id = "incoming", Type = "source", Label = "Incoming\nDentures",
                X = 1, Y = 5, Width = 4, Height = 4,
                Params = new() { ["mean_interval"] = 5.5 }, Color = "#E1BEE7" },

            // Step 1: Inspection & cleaning (10s manual)
            new() { Id = "buf1", Type = "buffer", Label = "Queue 1",
                X = 6.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 20 } },
            new() { Id = "inspect", Type = "server", Label = "1. Inspect\n& Clean\n(10s)",
                X = 10.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = 1.0 }, Color = "#4FC3F7" },

            // Step 2: Roland LEF UV printer (60s/pc, batch of 15 — bottleneck)
            new() { Id = "buf2", Type = "buffer", Label = "Print\nQueue",
                X = 17, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 30 } },
            new() { Id = "roland", Type = "roland", Label = "2. Roland LEF\nUV Print\n(15×60s)",
                X = 21, Y = 4.5, Width = 6, Height = 5,
                Params = new() { ["per_piece_time"] = 0.4, ["batch_size"] = 15 }, Color = "#FF6F00" },

            // Step 3: Manual packaging (20s)
            new() { Id = "buf3", Type = "buffer", Label = "Pack\nQueue",
                X = 28.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 20 } },
            new() { Id = "pack", Type = "server", Label = "3. Manual\nPackaging\n(20s)",
                X = 32.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = 2.0 }, Color = "#66BB6A" },

            // Step 4: Labeling (4s manual + 8s auto)
            new() { Id = "buf4", Type = "buffer", Label = "Label\nQueue",
                X = 39, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 15 } },
            new() { Id = "label", Type = "server", Label = "4. Labeling\n(4s+8s)",
                X = 43, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = 1.2 }, Color = "#FFCA28" },

            // Step 5: SSB placement (3s manual)
            new() { Id = "buf5", Type = "buffer", Label = "SSB\nQueue",
                X = 49.5, Y = 5.5, Width = 2.5, Height = 3,
                Params = new() { ["capacity"] = 10 } },
            new() { Id = "ssb", Type = "server", Label = "5. SSB\nPlacement\n(3s)",
                X = 53.5, Y = 5, Width = 5, Height = 4,
                Params = new() { ["service_time"] = 0.3 }, Color = "#AB47BC" },

            // Shipping
            new() { Id = "shipped", Type = "sink", Label = "Shipped",
                X = 60, Y = 5, Width = 4, Height = 4,
                Color = "#43A047" },
        },
        Connections = new List<VizConnection>
        {
            new() { From = "incoming", To = "buf1" },
            new() { From = "buf1", To = "inspect" },
            new() { From = "inspect", To = "buf2" },
            new() { From = "buf2", To = "roland" },
            new() { From = "roland", To = "buf3" },
            new() { From = "buf3", To = "pack" },
            new() { From = "pack", To = "buf4" },
            new() { From = "buf4", To = "label" },
            new() { From = "label", To = "buf5" },
            new() { From = "buf5", To = "ssb" },
            new() { From = "ssb", To = "shipped" },
        }
    };
}

public class VizNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("params")]
    public Dictionary<string, double> Params { get; set; } = new();

    // Physical layout (meters). If set, auto-layout is skipped for this node.
    [JsonPropertyName("x")]
    public double? X { get; set; }

    [JsonPropertyName("y")]
    public double? Y { get; set; }

    [JsonPropertyName("width")]
    public double? Width { get; set; }

    [JsonPropertyName("height")]
    public double? Height { get; set; }

    /// <summary>
    /// Display label override. If null, uses Id.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Color hint for rendering (hex, e.g. "#4CAF50"). Null = default per type.
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    public bool HasPhysicalPosition => X.HasValue && Y.HasValue;
}

public class VizConnection
{
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";
}
