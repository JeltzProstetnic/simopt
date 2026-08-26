using System;
using System.Text.Json;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-20 / SIM-63 — the first tests ever written against the MCP head.
///
/// <para>
/// The MCP endpoint is the surface the product's v0.9 ships on and the one an agent drives, yet
/// until now nothing exercised it. That mattered more than it looked: <see cref="SimulationTools"/>
/// wraps every call in a catch-all that serialises the exception into an <c>error</c> field, so a
/// totally broken build path returns a well-formed JSON document and reads, to a caller, like a
/// working tool reporting a bad model. A test that only asserts "returns a string" would pass
/// against a server that cannot construct a single model.
/// </para>
/// <para>
/// These tests therefore assert on the <em>absence of an error field</em> and on real post-run
/// numbers, not on the call completing.
/// </para>
/// </summary>
public class ModelRegistrySmokeTests
{
    /// <summary>The canonical single-queue single-server topology: source → buffer → server → sink.</summary>
    private static TopologyDefinition Sqss() => new()
    {
        Name = "SQSS",
        Seed = 42,
        Nodes =
        {
            new NodeDefinition { Id = "arrivals", Type = "source", Params = { ["mean_interval"] = 2.0 } },
            new NodeDefinition { Id = "queue",    Type = "buffer", Params = { ["capacity"] = 15 } },
            new NodeDefinition { Id = "server",   Type = "server", Params = { ["service_time"] = 1.5 } },
            new NodeDefinition { Id = "done",     Type = "sink" },
        },
        Connections =
        {
            new ConnectionDefinition { From = "arrivals", To = "queue" },
            new ConnectionDefinition { From = "queue",    To = "server" },
            new ConnectionDefinition { From = "server",   To = "done" },
        },
    };

    [Fact]
    public void Create_BuildsTheCanonicalTopology_WithoutThrowing()
    {
        var registry = new ModelRegistry();

        string modelId = registry.Create(Sqss());

        modelId.Should().NotBeNullOrWhiteSpace();
        var active = registry.Get(modelId);
        active.Sources.Should().ContainKey("arrivals");
        active.Buffers.Should().ContainKey("queue");
        active.Servers.Should().ContainKey("server");
        active.Sinks.Should().ContainKey("done");
    }

    [Fact]
    public void CreateModelTool_DoesNotReturnAnError()
    {
        var tools = new SimulationTools(new ModelRegistry());

        string json = tools.CreateModel(Sqss());

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("error", out JsonElement err)
            .Should().BeFalse($"create_model must build the canonical topology, but reported: {(err.ValueKind == JsonValueKind.Undefined ? "" : err.GetString())}");
    }

    [Fact]
    public void RunSimulation_ProducesThroughput_NotJustAWellFormedErrorDocument()
    {
        var registry = new ModelRegistry();
        var tools = new SimulationTools(registry);
        string modelId = registry.Create(Sqss());

        string json = tools.RunSimulation(modelId, 1000.0);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("error", out JsonElement err)
            .Should().BeFalse($"run_simulation must run the model, but reported: {(err.ValueKind == JsonValueKind.Undefined ? "" : err.GetString())}");

        // Arrivals every ~2.0, service ~1.5 ⇒ rho ≈ 0.75, stable, so entities must reach the sink.
        // The assertion is deliberately loose on the count and strict on it being non-zero: a build
        // that silently produces a model with no flow is the failure mode being gated here.
        doc.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done")
            .GetInt32().Should().BeGreaterThan(0, "a stable M/M/1 must deliver entities to its sink");
        doc.RootElement.GetProperty("events_processed").GetInt64().Should().BeGreaterThan(0);
    }

    // ── reproducibility (UN-009, UN-033) ──────────────────────────────────────

    /// <summary>Runs the SQSS topology once and returns how many entities reached the sink.</summary>
    private static int ThroughputOf(TopologyDefinition topology, double duration = 1000.0)
    {
        var registry = new ModelRegistry();
        var tools = new SimulationTools(registry);
        string modelId = registry.Create(topology);

        using var doc = JsonDocument.Parse(tools.RunSimulation(modelId, duration));
        doc.RootElement.TryGetProperty("error", out JsonElement err)
            .Should().BeFalse($"the run must succeed, but reported: {(err.ValueKind == JsonValueKind.Undefined ? "" : err.GetString())}");
        return doc.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done").GetInt32();
    }

    [Fact]
    public void SameTopologyAndSeed_ProduceIdenticalResults()
    {
        ThroughputOf(Sqss()).Should().Be(ThroughputOf(Sqss()));
    }

    [Fact]
    public void RepeatedRunsOfTheSameModel_ProduceIdenticalResults()
    {
        var registry = new ModelRegistry();
        var tools = new SimulationTools(registry);
        string modelId = registry.Create(Sqss());

        static (int Sink, int Queue, long Events) StatsOf(string json)
        {
            using var d = JsonDocument.Parse(json);
            JsonElement stats = d.RootElement.GetProperty("stats");
            return (stats.GetProperty("sinks").GetProperty("done").GetInt32(),
                    stats.GetProperty("buffers").GetProperty("queue").GetInt32(),
                    d.RootElement.GetProperty("events_processed").GetInt64());
        }

        var first = StatsOf(tools.RunSimulation(modelId, 1000.0));
        var second = StatsOf(tools.RunSimulation(modelId, 1000.0));
        var third = StatsOf(tools.RunSimulation(modelId, 1000.0));

        // run_simulation resets before every run and advertises that results are "independent and
        // reproducible". The whole optimizer loop depends on it: IProblem.Evaluate resets and
        // re-runs, so an evaluation that silently diverges from its neighbours corrupts every
        // fitness comparison built on top of it. This is the MCP-surface analogue of SIM-58.
        second.Should().Be(third);

        // SIM-91, closed. The first run of a freshly built model used to disagree with every later
        // one (476/2862 against a stable 475/2859) because Source.Reset raised its t=0 arrival
        // synchronously, mid-reset, against downstream entities that had not been reset yet.
        first.Should().Be(second, "the first run of a model must not differ from the ones after it");
    }

    [Fact]
    public void NodeOrderInTheTopology_DoesNotChangeResults()
    {
        var ordered = Sqss();
        var shuffled = Sqss();
        shuffled.Nodes.Reverse();   // same nodes, same connections, declared in the opposite order

        // Each node's stream is keyed to its stable ID rather than to its construction position,
        // so how the caller happened to order the JSON cannot move the answer. An LLM emits node
        // lists in whatever order it likes; without this property the same described system would
        // give different numbers on different days for no stated reason (SIM-89).
        ThroughputOf(shuffled).Should().Be(ThroughputOf(ordered));
    }

    [Fact]
    public void ADifferentSeed_ProducesADifferentSampleePath()
    {
        var a = Sqss();
        var b = Sqss();
        b.Seed = 4242;

        // Guards the opposite failure: seeds that are plumbed but ignored would make every run
        // identical and reproducibility would be vacuously "true".
        ThroughputOf(b).Should().NotBe(ThroughputOf(a));
    }
}
