using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 3: <c>station(servers: c)</c>. D2: a pool of c single servers behind the upstream
/// queue, an arrival waking the first idle one. That is the construction SIM-64's analytic battery
/// validates against Erlang-C (<c>AnalyticBatteryTests.RunMMc</c>), so the pool the builder makes is
/// checked against the same closed form here. D3, corrected: every station is fed by a queue. A push
/// would reach every server of a pool at once, which is not a c-server station, and a single server
/// refuses a push, which the builder used to ignore (SIM-135); both are refused with the fix named.
/// A station with one server keeps its node id as its server id, so its random stream, and every
/// result built on it, does not move.
/// </summary>
public class TopologyMultiServerTests
{
    private static DistributionSpec Exponential(double mean) => new DistributionSpec { Type = "exponential", Mean = mean };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static TopologyDefinition Topology(IEnumerable<NodeDefinition> nodes, params (string From, string To)[] connections) =>
        new TopologyDefinition
        {
            Name = "pools",
            Seed = 7,
            Nodes = nodes.ToList(),
            Connections = connections.Select(c => new ConnectionDefinition { From = c.From, To = c.To }).ToList(),
        };

    private static NodeDefinition[] MMcNodes(double lambda, ParamValue servers, double mu = 1.0) => new[]
    {
        Node("arrivals", "source", ("arrival", Exponential(1 / lambda))),
        Node("q", "queue"),
        Node("s", "station", ("servers", servers), ("service", Exponential(1 / mu))),
        Node("done", "sink"),
    };

    private static readonly (string, string)[] MMcWires = { ("arrivals", "q"), ("q", "s"), ("s", "done") };

    private static TopologyDefinition MMc(double lambda, ParamValue servers, double mu = 1.0) =>
        Topology(MMcNodes(lambda, servers, mu), MMcWires);

    private static void AgreesWithErlangC(MetricSummary metric, double analytic, string what)
    {
        double[] estimates = metric.ReplicateEstimates.Where(v => !double.IsNaN(v)).ToArray();
        estimates.Should().HaveCount(Replications, $"{what}: every replication must produce data");
        double gate = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.999)!.Value;
        double reported = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.95)!.Value;

        Math.Abs(metric.Mean - analytic).Should().BeLessThanOrEqualTo(gate,
            $"{what}: simulated {metric.Mean:F4} must fall inside the 99.9 % interval ±{gate:F4} around Erlang-C's {analytic:F4}");
        reported.Should().BeLessThanOrEqualTo(0.05 * analytic,
            $"{what}: an interval of ±{reported:F4} is too wide to be evidence");
    }

    private const int Replications = 20;

    [Fact]
    public void AThreeServerStation_AgreesWithErlangC()
    {
        // λ = 2.4, μ = 1, c = 3: ρ = 0.8. One server would be unstable at this load, and a pool whose
        // arrivals woke only the first server would queue without bound too.
        const double lambda = 2.4, mu = 1.0;
        const int c = 3;
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(MMc(lambda, c, mu)));
        Instrumentation.ObserveWaitingTime(active.Buffers["q"]);
        Instrumentation.ObserveQueueLength(active.Buffers["q"]);

        ReplicationResult result = new ReplicationRunner(active.Model, runLength: 21_000d, warmupTime: 1_000d)
            .Run(Replications, 20_261_125);

        AgreesWithErlangC(result["q.stat." + Instrumentation.WaitTime], QueueingFormulas.MMc_Wq(lambda, mu, c), "M/M/3 Wq");
        AgreesWithErlangC(result["q.stat." + Instrumentation.QueueLength], QueueingFormulas.MMc_Lq(lambda, mu, c), "M/M/3 Lq");
    }

    [Fact]
    public void EveryServerOfThePool_WorksAndIsReported()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(MMc(2.4, 3));
        ActiveModel active = registry.Get(id);
        var done = new int[3];
        for (int k = 0; k < 3; k++)
        {
            int member = k;
            active.Servers["s#" + (k + 1)].EntityCreatedEvent.AddHandler((_, _) => done[member]++);
        }

        using JsonDocument doc = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, 1000));

        JsonElement stats = doc.RootElement.GetProperty("stats");
        stats.GetProperty("servers").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("s#1", "s#2", "s#3");
        done.Should().OnlyContain(n => n > 100, "at rho = 0.8 every server of the pool carries work");
        done.Sum().Should().Be(stats.GetProperty("sinks").GetProperty("done").GetInt32());
    }

    [Fact]
    public void AStationWithOneServer_KeepsItsNodeIdAsServerId()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(MMc(0.5, 1)));

        active.Servers.Keys.Should().Equal("s");
    }

    [Theory]
    [InlineData("source")]
    [InlineData("station")]
    [InlineData("delay")]
    public void APoolFedWithoutAQueue_IsRefused_NamingTheFix(string upstreamType)
    {
        NodeDefinition upstream = upstreamType switch
        {
            "source" => Node("up", "source", ("arrival", Exponential(1))),
            "station" => Node("up", "station", ("service", Exponential(0.5))),
            _ => Node("up", "delay", ("duration", Exponential(0.5))),
        };
        var nodes = new List<NodeDefinition>
        {
            upstream,
            Node("s", "station", ("servers", 3), ("service", Exponential(1))),
            Node("done", "sink"),
        };
        var wires = new List<(string, string)> { ("up", "s"), ("s", "done") };
        if (upstreamType == "station")
        {
            nodes.InsertRange(0, new[] { Node("arrivals", "source", ("arrival", Exponential(1))), Node("qa", "queue") });
            wires.InsertRange(0, new[] { ("arrivals", "qa"), ("qa", "up") });
        }
        else if (upstreamType == "delay")
        {
            nodes.Insert(0, Node("arrivals", "source", ("arrival", Exponential(1))));
            wires.Insert(0, ("arrivals", "up"));
        }

        Action build = () => new ModelRegistry().Create(Topology(nodes, wires.ToArray()));

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'s'") && e.Message.Contains("3 servers")
                      && e.Message.Contains("'up'") && e.Message.Contains("queue"));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("station")]
    public void AStationOfOneServer_IsFedByAQueueToo(string upstreamType)
    {
        // A server refuses a push, and the builder used to ignore the refusal: a source or station
        // wired straight to a station built without error and delivered nothing.
        var nodes = new List<NodeDefinition> { Node("arrivals", "source", ("arrival", Exponential(1))) };
        var wires = new List<(string, string)>();
        string upstream = "arrivals";
        if (upstreamType == "station")
        {
            nodes.Add(Node("qa", "queue"));
            nodes.Add(Node("up", "station", ("service", Exponential(0.5))));
            wires.Add(("arrivals", "qa"));
            wires.Add(("qa", "up"));
            upstream = "up";
        }
        nodes.Add(Node("s", "station", ("service", Exponential(0.5))));
        nodes.Add(Node("done", "sink"));
        wires.Add((upstream, "s"));
        wires.Add(("s", "done"));

        Action build = () => new ModelRegistry().Create(Topology(nodes, wires.ToArray()));

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'s'") && e.Message.Contains("'" + upstream + "'") && e.Message.Contains("queue"));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-2d)]
    [InlineData(2.5d)]
    [InlineData(100_000d)]
    public void AServerCountThatIsNotAWholeNumberFrom1To10000_IsRefused(double servers)
    {
        Action build = () => new ModelRegistry().Create(MMc(1, servers));

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'s'") && e.Message.Contains("servers"));
    }

    [Fact]
    public void APoolFeedsTheNextQueue()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Topology(new[]
            {
                Node("arrivals", "source", ("arrival", Exponential(1 / 2.4))),
                Node("q", "queue"),
                Node("s", "station", ("servers", 3), ("service", Exponential(1))),
                Node("q2", "queue"),
                Node("s2", "station", ("service", Exponential(0.3))),
                Node("done", "sink"),
            },
            ("arrivals", "q"), ("q", "s"), ("s", "q2"), ("q2", "s2"), ("s2", "done")));

        using JsonDocument doc = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, 2000));

        doc.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done").GetInt32()
           .Should().BeInRange(4_500, 5_100, "about 4,800 arrivals in 2,000 time units, both stages below saturation");
    }

    [Fact]
    public void ThePoolsRandomStreamsAreKeyedToTheirIds_SoTheNodeOrderDoesNotMatter()
    {
        var registry = new ModelRegistry();
        string forward = registry.Create(Topology(MMcNodes(2.4, 3), MMcWires));
        string backward = registry.Create(Topology(MMcNodes(2.4, 3).Reverse(), MMcWires));
        var tools = new SimulationTools(registry);

        using JsonDocument a = JsonDocument.Parse(tools.RunSimulation(forward, 500));
        using JsonDocument b = JsonDocument.Parse(tools.RunSimulation(backward, 500));

        b.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done").GetInt32()
         .Should().Be(a.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done").GetInt32());
        b.RootElement.GetProperty("stats").GetProperty("buffers").GetProperty("q").GetInt32()
         .Should().Be(a.RootElement.GetProperty("stats").GetProperty("buffers").GetProperty("q").GetInt32());
    }
}
