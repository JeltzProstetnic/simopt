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
