using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 5 (D6): a topology declares its metrics, <c>metrics: [{id, kind, node?}]</c>, and
/// the builder attaches a collector for each, addressable by the metric id through
/// <see cref="ActiveModel.Metrics"/> (the statistic key a <see cref="ReplicationResult"/> is indexed
/// by). Reporting them is SIM-66. <c>wait_time</c> and <c>queue_length</c> are measured at a queue,
/// <c>utilization</c> at a station (its pool's busy share), <c>count</c> and <c>throughput</c> at a
/// sink, <c>cycle_time</c> at one sink or, without a node, at all of them.
/// </summary>
public class TopologyMetricTests
{
    private static DistributionSpec Exponential(double mean) => new DistributionSpec { Type = "exponential", Mean = mean };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static MetricDefinition Metric(string id, string kind, string? node = null) =>
        new MetricDefinition { Id = id, Kind = kind, Node = node };

    private static TopologyDefinition MMc(params MetricDefinition[] metrics) => new TopologyDefinition
    {
        Name = "metrics",
        Seed = 7,
        Nodes =
        {
            Node("arrivals", "source", ("arrival", Exponential(1 / 2.4))),
            Node("q", "queue"),
            Node("s", "station", ("servers", 3), ("service", Exponential(1))),
            Node("done", "sink"),
        },
        Connections =
        {
            new ConnectionDefinition { From = "arrivals", To = "q" },
            new ConnectionDefinition { From = "q", To = "s" },
            new ConnectionDefinition { From = "s", To = "done" },
        },
        Metrics = metrics.ToList(),
    };

    private static Action Build(TopologyDefinition t) => () => new ModelRegistry().Create(t);

    [Fact]
    public void DeclaredMetrics_AreAddressableByTheirIds_AfterAReplicatedRun()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(MMc(
            Metric("wq", "wait_time", "q"),
            Metric("lq", "queue_length", "q"),
            Metric("util", "utilization", "s"),
            Metric("n", "count", "done"),
            Metric("tput", "throughput", "done"),
            Metric("los", "cycle_time"))));

        ReplicationResult result = new ReplicationRunner(active.Model, runLength: 4_100d, warmupTime: 100d).Run(5, 2026);

        active.Metrics.Keys.Should().BeEquivalentTo("wq", "lq", "util", "n", "tput", "los");
        foreach (string key in active.Metrics.Values)
            result[key].ReplicateEstimates.Should().HaveCount(5).And.OnlyContain(v => !double.IsNaN(v));
        double util = result[active.Metrics["util"]].Mean;
        double tput = result[active.Metrics["tput"]].Mean;
        double wq = result[active.Metrics["wq"]].Mean;
        double los = result[active.Metrics["los"]].Mean;
        util.Should().BeApproximately(0.8, 0.03, "lambda 2.4 over three servers of mu 1");
        tput.Should().BeApproximately(2.4, 0.1);
        result[active.Metrics["n"]].Mean.Should().BeApproximately(tput * 4_000, 0.005 * tput * 4_000, "a count is the throughput times the 4,000 units after the warm-up");
        los.Should().BeApproximately(wq + 1, 0.15, "a stay is the wait plus a mean service of 1");
    }

    [Fact]
    public void TwoMetricsOfOneKindAtOneNode_ShareTheirCollector()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(MMc(Metric("a", "wait_time", "q"), Metric("b", "wait_time", "q"))));

        active.Metrics["a"].Should().Be(active.Metrics["b"]).And.Be("q.stat." + Instrumentation.WaitTime);
    }

    [Theory]
    [InlineData("wait_time", "s", "queue")]
    [InlineData("queue_length", "done", "queue")]
    [InlineData("utilization", "q", "station")]
    [InlineData("count", "s", "sink")]
    [InlineData("throughput", "q", "sink")]
    [InlineData("cycle_time", "q", "sink")]
    [InlineData("wait_time", "nowhere", "queue")]
    public void AMetricAtANodeOfTheWrongType_IsRefused_NamingWhatItNeeds(string kind, string node, string needs)
    {
        Build(MMc(Metric("m", kind, node))).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'m'") && e.Message.Contains(kind) && e.Message.Contains("'" + node + "'")
                     && e.Message.Contains(needs));
    }

    [Theory]
    [InlineData("wait_time")]
    [InlineData("utilization")]
    [InlineData("count")]
    public void AMetricThatNeedsANode_IsRefusedWithoutOne(string kind)
    {
        Build(MMc(Metric("m", kind))).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'m'") && e.Message.Contains("node"));
    }

    [Fact]
    public void AnUnknownKind_IsRefused_NamingTheChoices()
    {
        Build(MMc(Metric("m", "happiness", "q"))).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'m'") && e.Message.Contains("wait_time") && e.Message.Contains("cycle_time"));
    }

    [Fact]
    public void AMetricIdDeclaredTwice_OrMissing_IsRefused()
    {
        Build(MMc(Metric("m", "wait_time", "q"), Metric("m", "count", "done"))).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'m'"));
        Build(MMc(Metric(" ", "wait_time", "q"))).Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("id"));
    }

    [Fact]
    public void DeclaredMetrics_BindFromJson()
    {
        TopologyDefinition t = JsonSerializer.Deserialize<TopologyDefinition>(
            """{"name": "j", "metrics": [{"id": "wait_exam", "kind": "wait_time", "node": "exam_q"}, {"id": "los", "kind": "cycle_time"}]}""",
            McpJsonUtilities.DefaultOptions)!;

        t.Metrics.Select(m => (m.Id, m.Kind, m.Node)).Should().Equal(("wait_exam", "wait_time", "exam_q"), ("los", "cycle_time", null));
    }
}
