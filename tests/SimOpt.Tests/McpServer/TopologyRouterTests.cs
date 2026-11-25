using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 4: the <c>router</c> node, <c>{policy: probabilistic | shortest_queue}</c>, built as a
/// <c>SimpleRouter</c> keyed to its node id. A probabilistic router reads the <c>weight</c> of each
/// connection leaving it, on all of them or on none (an equal split); a shortest-queue router reads no
/// weight and routes only to queues. A router is fed like a delay (by a source, station, delay or
/// router, not by a queue) and feeds a queue, delay, sink or router; a station still takes its work
/// from a queue (D3).
/// </summary>
public class TopologyRouterTests
{
    private static DistributionSpec Constant(double v) => new DistributionSpec { Type = "constant", Value = v };
    private static DistributionSpec Exponential(double mean) => new DistributionSpec { Type = "exponential", Mean = mean };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static ConnectionDefinition Wire(string from, string to, double? weight = null) =>
        new ConnectionDefinition { From = from, To = to, Weight = weight };

    private static TopologyDefinition Topology(IEnumerable<NodeDefinition> nodes, params ConnectionDefinition[] connections) =>
        new TopologyDefinition { Name = "routers", Seed = 7, Nodes = nodes.ToList(), Connections = connections.ToList() };

    private static TopologyDefinition SplitToTwoSinks(string policy, double? weightA, double? weightB) => Topology(new[]
        {
            Node("arrivals", "source", ("arrival", Constant(1))),
            Node("r", "router", ("policy", policy)),
            Node("a", "sink"),
            Node("b", "sink"),
        },
        Wire("arrivals", "r"), Wire("r", "a", weightA), Wire("r", "b", weightB));

    private static JsonElement Stats(ModelRegistry registry, string id, double duration)
    {
        using JsonDocument doc = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, duration));
        doc.RootElement.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        return doc.RootElement.GetProperty("stats").Clone();
    }

    private static int Sink(JsonElement stats, string id) => stats.GetProperty("sinks").GetProperty(id).GetInt32();

    private static Action Build(TopologyDefinition t) => () => new ModelRegistry().Create(t);

    [Fact]
    public void AProbabilisticRouter_SplitsByTheConnectionWeights()
    {
        var registry = new ModelRegistry();
        JsonElement stats = Stats(registry, registry.Create(SplitToTwoSinks("probabilistic", 1, 3)), 10_000.5);

        int a = Sink(stats, "a"), b = Sink(stats, "b");
        (a + b).Should().Be(10_001);
        (a / 10_001d).Should().BeApproximately(0.25, 0.02, "weights 1 and 3; 0.02 is over four standard errors");
        JsonElement router = stats.GetProperty("routers").GetProperty("r");
        router.GetProperty("routed").GetProperty("a").GetInt32().Should().Be(a);
        router.GetProperty("routed").GetProperty("b").GetInt32().Should().Be(b);
        router.GetProperty("refused").GetInt32().Should().Be(0);
    }

    [Fact]
    public void WithoutWeights_AProbabilisticRouterSplitsEqually()
    {
        var registry = new ModelRegistry();
        JsonElement stats = Stats(registry, registry.Create(SplitToTwoSinks("probabilistic", null, null)), 10_000.5);

        (Sink(stats, "a") / 10_001d).Should().BeApproximately(0.5, 0.02);
    }

    [Fact]
    public void AShortestQueueRouter_KeepsTwoQueuedStationsBusy()
    {
        // λ = 2 split over two stations of μ = 1.25: each runs at ρ = 0.8 under an even split.
        var registry = new ModelRegistry();
        string id = registry.Create(Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Exponential(0.5))),
                Node("r", "router", ("policy", "shortest_queue")),
                Node("q1", "queue"), Node("s1", "station", ("service", Exponential(0.8))),
                Node("q2", "queue"), Node("s2", "station", ("service", Exponential(0.8))),
                Node("done", "sink"),
            },
            Wire("arrivals", "r"), Wire("r", "q1"), Wire("r", "q2"),
            Wire("q1", "s1"), Wire("q2", "s2"), Wire("s1", "done"), Wire("s2", "done")));

        JsonElement stats = Stats(registry, id, 5_000);

        int delivered = Sink(stats, "done");
        delivered.Should().BeInRange(9_500, 10_500, "about 10,000 arrivals and both stations below saturation");
        JsonElement routed = stats.GetProperty("routers").GetProperty("r").GetProperty("routed");
        int toQ1 = routed.GetProperty("q1").GetInt32(), toQ2 = routed.GetProperty("q2").GetInt32();
        (toQ1 / (double)(toQ1 + toQ2)).Should().BeApproximately(0.5, 0.05, "two equal stations share the work");
    }

    [Fact]
    public void TheRoutersStreamIsKeyedToItsId_SoTheNodeOrderDoesNotMatter()
    {
        TopologyDefinition forward = SplitToTwoSinks("probabilistic", 1, 1);
        TopologyDefinition backward = SplitToTwoSinks("probabilistic", 1, 1);
        backward.Nodes.Reverse();
        var registry = new ModelRegistry();

        int a1 = Sink(Stats(registry, registry.Create(forward), 500.5), "a");
        int a2 = Sink(Stats(registry, registry.Create(backward), 500.5), "a");

        a2.Should().Be(a1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("round_robin")]
    public void APolicyThatIsMissingOrUnknown_IsRefused_NamingTheChoices(string? policy)
    {
        TopologyDefinition t = SplitToTwoSinks("probabilistic", null, null);
        if (policy == null) t.Nodes[1].Params.Remove("policy");
        else t.Nodes[1].Params["policy"] = policy;

        Build(t).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'r'") && e.Message.Contains("probabilistic") && e.Message.Contains("shortest_queue"));
    }

    [Fact]
    public void WeightsOnSomeButNotAllConnections_AreRefused()
    {
        Build(SplitToTwoSinks("probabilistic", 2, null)).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'r'") && e.Message.Contains("weight"));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void AWeightThatIsNotPositive_IsRefused_NamingTheConnection(double weight)
    {
        Build(SplitToTwoSinks("probabilistic", weight, 1)).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'r'") && e.Message.Contains("'a'") && e.Message.Contains("weight"));
    }

    [Fact]
    public void AShortestQueueRouter_ReadsNoWeight()
    {
        TopologyDefinition t = Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("r", "router", ("policy", "shortest_queue")),
                Node("q1", "queue"), Node("s1", "station", ("service", Constant(0.5))),
                Node("done", "sink"),
            },
            Wire("arrivals", "r"), Wire("r", "q1", 2), Wire("q1", "s1"), Wire("s1", "done"));

        Build(t).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'r'") && e.Message.Contains("weight") && e.Message.Contains("shortest_queue"));
    }

    [Fact]
    public void AShortestQueueRouter_RoutesOnlyToQueues()
    {
        Build(SplitToTwoSinks("shortest_queue", null, null)).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'r'") && e.Message.Contains("'a'") && e.Message.Contains("queue"));
    }

    [Fact]
    public void ARouterWithoutAConnectionLeavingIt_IsRefused()
    {
        TopologyDefinition t = Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("r", "router", ("policy", "probabilistic")),
            },
            Wire("arrivals", "r"));

        Build(t).Should().Throw<InvalidOperationException>().Where(e => e.Message.Contains("'r'"));
    }

    [Fact]
    public void AQueueCannotFeedARouter_AndTheRefusalNamesTheFix()
    {
        TopologyDefinition t = Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("q", "queue"),
                Node("r", "router", ("policy", "probabilistic")),
                Node("a", "sink"),
            },
            Wire("arrivals", "q"), Wire("q", "r"), Wire("r", "a"));

        Build(t).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'q'") && e.Message.Contains("'r'") && e.Message.Contains("'arrivals'"));
    }

    [Fact]
    public void ARouterCannotFeedAStationDirectly()
    {
        TopologyDefinition t = Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("r", "router", ("policy", "probabilistic")),
                Node("s", "station", ("service", Constant(0.5))),
                Node("done", "sink"),
            },
            Wire("arrivals", "r"), Wire("r", "s"), Wire("s", "done"));

        Build(t).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'s'") && e.Message.Contains("'r'") && e.Message.Contains("queue"));
    }

    [Fact]
    public void RoutersChainThroughDelaysAndOtherRouters()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Constant(1))),
                Node("walk", "delay", ("duration", Constant(2))),
                Node("r1", "router", ("policy", "probabilistic")),
                Node("r2", "router", ("policy", "probabilistic")),
                Node("a", "sink"), Node("b", "sink"), Node("c", "sink"),
            },
            Wire("arrivals", "walk"), Wire("walk", "r1"),
            Wire("r1", "a", 1), Wire("r1", "r2", 1), Wire("r2", "b"), Wire("r2", "c")));

        JsonElement stats = Stats(registry, id, 4_000.5);

        int a = Sink(stats, "a"), b = Sink(stats, "b"), c = Sink(stats, "c");
        (a + b + c).Should().Be(3_999, "items of t = 0..3998 have left the two-unit walk");
        (a / 3_999d).Should().BeApproximately(0.5, 0.04);
        (b / 3_999d).Should().BeApproximately(0.25, 0.04);
    }
}
