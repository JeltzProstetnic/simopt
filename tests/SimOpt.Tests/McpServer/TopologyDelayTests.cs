using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 2, second part: the <c>delay</c> node, <c>{duration: Dist}</c> (or the older
/// <c>delay_time</c>, an exponential mean). Every item gets its own timer, so a delay holds as many
/// items as arrive (SIM-132's unlimited capacity); a delay with one place would have dropped most of
/// them. It is a push node: a source, station or delay feeds it, and it feeds a queue, station, delay
/// or sink. A queue cannot feed it, because a delay would take every item at once and the queue
/// would stay empty. Connections carry an optional <c>weight</c> for the router of step 4; until a
/// router exists nothing reads it, so a weight is refused rather than silently ignored.
/// </summary>
public class TopologyDelayTests
{
    private static DistributionSpec Constant(double v) => new DistributionSpec { Type = "constant", Value = v };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static TopologyDefinition Topology(IEnumerable<NodeDefinition> nodes, params (string From, string To)[] connections) =>
        new TopologyDefinition
        {
            Name = "delays",
            Seed = 7,
            Nodes = nodes.ToList(),
            Connections = connections.Select(c => new ConnectionDefinition { From = c.From, To = c.To }).ToList(),
        };

    private static JsonElement Stats(ModelRegistry registry, string id, double duration)
    {
        using JsonDocument doc = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, duration));
        doc.RootElement.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        return doc.RootElement.GetProperty("stats").Clone();
    }

    private static TopologyDefinition SourceDelaySink(params (string Key, ParamValue Value)[] delayParams) =>
        Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("d", "delay", delayParams),
                Node("done", "sink"),
            },
            ("arrivals", "d"), ("d", "done"));

    [Fact]
    public void EveryItemGetsItsOwnTimer_SoADelayHoldsManyItemsAtOnce()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(SourceDelaySink(("duration", Constant(5))));
        var created = 0;
        registry.Get(id).Sources["arrivals"].EntityCreatedEvent.AddHandler((_, _) => created++);

        JsonElement stats = Stats(registry, id, 100.5);

        // An item every time unit from t = 0, each held 5: those of t = 0..95 are out, 96..100 inside.
        // With one place (SIM-132's default) the delay would have refused four items in five.
        int delivered = stats.GetProperty("sinks").GetProperty("done").GetInt32();
        JsonElement delay = stats.GetProperty("delays").GetProperty("d");
        created.Should().Be(101);
        delivered.Should().Be(96);
        delay.GetProperty("in_flight").GetInt32().Should().Be(5);
        delay.GetProperty("refused").GetInt32().Should().Be(0);
    }

    [Fact]
    public void ADelayFeedsAQueuedStation_AndAStationFeedsADelay()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Topology(new[]
            {
                Node("arrivals", "source", ("arrival", new DistributionSpec { Type = "exponential", Mean = 2 })),
                Node("walk", "delay", ("duration", new DistributionSpec { Type = "uniform", Min = 1, Max = 3 })),
                Node("q", "queue"),
                Node("s", "station", ("service", Constant(1))),
                Node("cool", "delay", ("duration", Constant(0.5))),
                Node("done", "sink"),
            },
            ("arrivals", "walk"), ("walk", "q"), ("q", "s"), ("s", "cool"), ("cool", "done")));

        JsonElement stats = Stats(registry, id, 2000);

        stats.GetProperty("sinks").GetProperty("done").GetInt32().Should().BeInRange(900, 1100,
            "about 1000 arrivals in 2000 time units, rho = 0.5");
        stats.GetProperty("delays").GetProperty("walk").GetProperty("refused").GetInt32().Should().Be(0);
        stats.GetProperty("delays").GetProperty("cool").GetProperty("refused").GetInt32().Should().Be(0);
    }

    [Fact]
    public void TheOldDelayTimeIsAnExponentialMean()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(SourceDelaySink(("delay_time", 3.0)));
        var created = 0;
        registry.Get(id).Sources["arrivals"].EntityCreatedEvent.AddHandler((_, _) => created++);

        JsonElement stats = Stats(registry, id, 1000.5);

        int delivered = stats.GetProperty("sinks").GetProperty("done").GetInt32();
        int inFlight = stats.GetProperty("delays").GetProperty("d").GetProperty("in_flight").GetInt32();
        created.Should().Be(1001);
        (delivered + inFlight).Should().Be(created);
        inFlight.Should().BeInRange(1, 20, "about 3 items are inside at any time");
    }

    [Fact]
    public void GivingDurationAndDelayTimeTogether_IsRefused()
    {
        Action build = () => new ModelRegistry().Create(SourceDelaySink(("duration", Constant(5)), ("delay_time", 5.0)));

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'d'") && e.Message.Contains("duration") && e.Message.Contains("delay_time"));
    }

    [Fact]
    public void ADelayWithoutADuration_IsRefused_NamingWhatToGive()
    {
        Action build = () => new ModelRegistry().Create(SourceDelaySink());

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'d'") && e.Message.Contains("duration"));
    }

    [Fact]
    public void AQueueCannotFeedADelay_AndTheRefusalNamesTheFix()
    {
        TopologyDefinition t = Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("q", "queue"),
                Node("d", "delay", ("duration", Constant(5))),
                Node("done", "sink"),
            },
            ("arrivals", "q"), ("q", "d"), ("d", "done"));

        Action build = () => new ModelRegistry().Create(t);

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'q'") && e.Message.Contains("'d'") && e.Message.Contains("station"));
    }

    [Fact]
    public void TheDelaysRandomStreamIsKeyedToItsId_SoTheNodeOrderDoesNotMatter()
    {
        NodeDefinition[] Nodes() => new[]
        {
            Node("arrivals", "source", ("arrival", new DistributionSpec { Type = "exponential", Mean = 1 })),
            Node("d", "delay", ("duration", new DistributionSpec { Type = "exponential", Mean = 4 })),
            Node("done", "sink"),
        };
        (string, string)[] wires = { ("arrivals", "d"), ("d", "done") };
        var registry = new ModelRegistry();
        string forward = registry.Create(Topology(Nodes(), wires));
        string backward = registry.Create(Topology(Nodes().Reverse(), wires));

        JsonElement a = Stats(registry, forward, 500);
        JsonElement b = Stats(registry, backward, 500);

        b.GetProperty("sinks").GetProperty("done").GetInt32().Should().Be(a.GetProperty("sinks").GetProperty("done").GetInt32());
        b.GetProperty("delays").GetProperty("d").GetProperty("in_flight").GetInt32()
         .Should().Be(a.GetProperty("delays").GetProperty("d").GetProperty("in_flight").GetInt32());
    }

    [Fact]
    public void GetStatus_ListsADelayWithItsItemsInside()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(SourceDelaySink(("duration", Constant(5))));
        var tools = new SimulationTools(registry);
        tools.RunSimulation(id, 100.5);

        using JsonDocument doc = JsonDocument.Parse(tools.GetStatus(id));

        JsonElement node = doc.RootElement.GetProperty("nodes").EnumerateArray()
                              .Single(n => n.GetProperty("id").GetString() == "d");
        node.GetProperty("type").GetString().Should().Be("delay");
        node.GetProperty("in_flight").GetInt32().Should().Be(5);
    }

    [Fact]
    public void AnUnknownType_IsRefused_ListingTheDelay()
    {
        Action build = () => new ModelRegistry().Create(Topology(new[] { Node("x", "conveyor") }));

        build.Should().Throw<InvalidOperationException>().Where(e => e.Message.Contains("delay"));
    }

    [Fact]
    public void AConnectionWeight_BindsFromJson()
    {
        ConnectionDefinition weighted = JsonSerializer.Deserialize<ConnectionDefinition>(
            """{"from": "r", "to": "a", "weight": 0.7}""", McpJsonUtilities.DefaultOptions)!;
        ConnectionDefinition plain = JsonSerializer.Deserialize<ConnectionDefinition>(
            """{"from": "r", "to": "a"}""", McpJsonUtilities.DefaultOptions)!;

        weighted.Weight.Should().Be(0.7);
        plain.Weight.Should().BeNull();
    }

    [Fact]
    public void AWeightOnAConnectionNotFromARouter_IsRefused()
    {
        TopologyDefinition t = SourceDelaySink(("duration", Constant(5)));
        t.Connections[0].Weight = 0.5;

        Action build = () => new ModelRegistry().Create(t);

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'arrivals'") && e.Message.Contains("'d'")
                      && e.Message.Contains("weight") && e.Message.Contains("router"));
    }
}
