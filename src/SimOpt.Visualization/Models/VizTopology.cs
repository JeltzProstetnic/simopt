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
}

public class VizNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("params")]
    public Dictionary<string, double> Params { get; set; } = new();
}

public class VizConnection
{
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";
}
