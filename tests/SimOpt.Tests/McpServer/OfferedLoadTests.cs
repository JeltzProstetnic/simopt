using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-66 step 3 (D5): the offered load of every station before a run, so an overloaded one is named
/// with its fix (UN-008: "overloaded stations ... are flagged with the offending station named").
/// The rate into each node solves the traffic equations <c>λ = γ + λP</c>: sources add 1 / mean
/// interarrival, a probabilistic router splits by weight, a shortest-queue router equally (an
/// estimate, said so), every other node passes its flow on, and a rework loop is solved exactly. A
/// station's load is ρ = λ × E[S] / c, with E[S] the service distribution's mean, shift included
/// (SIM-102: a pre-check that read the unshifted mean passed exactly the model it exists to catch).
/// The load is the demand reaching a station if everything upstream keeps up.
/// </summary>
public class OfferedLoadTests
{
    private static DistributionSpec Exponential(double mean, double? shift = null) =>
        new DistributionSpec { Type = "exponential", Mean = mean, Shift = shift };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static NodeDefinition Source(string id, double meanInterval) => Node(id, "source", ("arrival", Exponential(meanInterval)));
    private static NodeDefinition Queue(string id) => Node(id, "queue");
    private static NodeDefinition Station(string id, double meanService, int servers = 1) =>
        Node(id, "station", ("servers", servers), ("service", Exponential(meanService)));
    private static NodeDefinition Sink(string id) => Node(id, "sink");
    private static NodeDefinition Router(string id, string policy = "probabilistic") => Node(id, "router", ("policy", policy));

    private static TopologyDefinition Topology(IEnumerable<NodeDefinition> nodes, params (string From, string To, double? Weight)[] connections) =>
        new TopologyDefinition
        {
            Name = "load",
            Seed = 7,
            Nodes = nodes.ToList(),
            Connections = connections.Select(c => new ConnectionDefinition { From = c.From, To = c.To, Weight = c.Weight }).ToList(),
        };

    private static TopologyDefinition Line(double meanInterval, NodeDefinition station, NodeDefinition? queue = null) =>
        Topology(new[] { Source("in", meanInterval), queue ?? Queue("q"), station, Sink("out") },
            ("in", "q", null), ("q", station.Id, null), (station.Id, "out", null));

    private static ValidationReport Check(TopologyDefinition topology)
    {
        var report = new ValidationReport();
        ModelRegistry.TryBuild(topology, report).Should().NotBeNull(string.Join("; ", report.Errors.Select(e => e.Line)));
        return report;
    }

    private static double Load(ValidationReport report, string station) =>
        report.Info.Single(i => i.Code == "OFFERED_LOAD" && i.Node == station).Value!.Value;

    [Fact]
    public void AnMM1Line_ReportsItsLoad_AndNoWarning()
    {
        ValidationReport report = Check(Line(2, Station("s", 1)));

        Load(report, "s").Should().BeApproximately(0.5, 1e-9);
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void TheClinic_HasExamLoad075_AndTriageLoad040()
    {
        TopologyDefinition clinic = Topology(new[]
        {
            Source("arrivals", 5),
            Queue("triage_q"),
            Node("triage", "station", ("servers", 3), ("service", new DistributionSpec { Type = "triangular", Min = 3, Mode = 5, Max = 10 })),
            Queue("exam_q"),
            Node("exam", "station", ("servers", 8), ("service", new DistributionSpec { Type = "triangular", Min = 20, Mode = 30, Max = 40 })),
            Sink("exit"),
        }, ("arrivals", "triage_q", null), ("triage_q", "triage", null), ("triage", "exam_q", null), ("exam_q", "exam", null), ("exam", "exit", null));

        ValidationReport report = Check(clinic);

        Load(report, "exam").Should().BeApproximately(0.75, 1e-9, "(12/h x 30 min) / 8 rooms, as the readiness review computes it");
        Load(report, "triage").Should().BeApproximately(0.4, 1e-9, "0.2 per minute x a 6-minute mean triage / 3 nurses");
    }

    [Fact]
    public void AnOverloadedStation_IsSaturated_NamedWithTheServersItNeeds()
    {
        ValidationReport report = Check(Line(1, Station("s", 1.38)));

        ValidationIssue saturated = report.Warnings.Should().ContainSingle().Which;
        saturated.Code.Should().Be("SATURATED");
        saturated.Node.Should().Be("s");
        saturated.Value.Should().BeApproximately(1.38, 1e-9);
        saturated.Message.Should().Contain("1.38").And.Contain("without bound");
        saturated.Suggestion.Should().Contain("2 servers", "1.38 / 2 = 0.69 is the first count below 0.9");
    }

    [Fact]
    public void AnOverloadedStationBehindAFiniteQueue_RefusesItemsThere()
    {
        ValidationReport report = Check(Line(1, Station("s", 1.5), Node("q", "queue", ("capacity", 10))));

        ValidationIssue saturated = report.Warnings.Should().ContainSingle().Which;
        saturated.Code.Should().Be("SATURATED");
        saturated.Message.Should().Contain("'q'").And.Contain("refused").And.NotContain("without bound");
    }

    [Fact]
    public void AStationJustBelowOne_IsNearSaturation()
    {
        ValidationReport report = Check(Line(1, Station("s", 0.95)));

        report.Warnings.Should().ContainSingle().Which.Code.Should().Be("NEAR_SATURATION");
    }

    [Fact]
    public void AShiftedServiceTime_CountsItsShift()
    {
        // SIM-102: exponential mean 2 shifted by 5 serves at 7; read without the shift it looks like 0.29
        TopologyDefinition topology = Line(6.86, Node("s", "station", ("service", Exponential(2, shift: 5))));

        ValidationReport report = Check(topology);

        Load(report, "s").Should().BeApproximately(7 / 6.86, 1e-9);
        report.Warnings.Should().ContainSingle().Which.Code.Should().Be("SATURATED");
    }

    [Fact]
    public void AProbabilisticRouter_SplitsByWeight()
    {
        TopologyDefinition topology = Topology(
            new[] { Source("in", 1), Router("r"), Queue("qa"), Station("a", 1, 2), Queue("qb"), Station("b", 1), Sink("out") },
            ("in", "r", null), ("r", "qa", 3), ("r", "qb", 1), ("qa", "a", null), ("a", "out", null), ("qb", "b", null), ("b", "out", null));

        ValidationReport report = Check(topology);

        Load(report, "a").Should().BeApproximately(0.75 / 2, 1e-9);
        Load(report, "b").Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void AReworkLoop_IsSolved_NotWalked()
    {
        // 20 % come back for another pass: the station sees 1 / 0.8 = 1.25 times the arrival rate
        TopologyDefinition topology = Topology(
            new[] { Source("in", 1), Queue("q"), Station("s", 0.5), Router("r"), Sink("out") },
            ("in", "q", null), ("q", "s", null), ("s", "r", null), ("r", "q", 0.2), ("r", "out", 0.8));

        Load(Check(topology), "s").Should().BeApproximately(1.25 * 0.5, 1e-9);
    }

    [Fact]
    public void AShortestQueueRouter_IsEstimatedAsAnEqualSplit_AndSaysSo()
    {
        TopologyDefinition topology = Topology(
            new[] { Source("in", 1), Router("r", "shortest_queue"), Queue("qa"), Station("a", 1), Queue("qb"), Station("b", 1), Sink("out") },
            ("in", "r", null), ("r", "qa", null), ("r", "qb", null), ("qa", "a", null), ("a", "out", null), ("qb", "b", null), ("b", "out", null));

        ValidationReport report = Check(topology);

        Load(report, "a").Should().BeApproximately(0.5, 1e-9);
        report.Info.Single(i => i.Node == "a").Message.Should().Contain("estimate").And.Contain("'r'");
    }

    [Fact]
    public void TwoSourcesAndADelay_AddTheirFlows()
    {
        TopologyDefinition topology = Topology(
            new[] { Source("in1", 4), Source("in2", 4), Node("walk", "delay", ("duration", Exponential(3))), Queue("q"), Station("s", 1), Sink("out") },
            ("in1", "walk", null), ("in2", "q", null), ("walk", "q", null), ("q", "s", null), ("s", "out", null));

        Load(Check(topology), "s").Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void AQueueFeedingTwoStations_PoolsThem()
    {
        TopologyDefinition topology = Topology(
            new[] { Source("in", 1 / 2.4), Queue("q"), Station("s1", 1), Station("s2", 1, 2), Sink("out") },
            ("in", "q", null), ("q", "s1", null), ("q", "s2", null), ("s1", "out", null), ("s2", "out", null));

        ValidationReport report = Check(topology);

        Load(report, "s1").Should().BeApproximately(0.8, 1e-9, "2.4 arrivals over three servers of rate 1");
        Load(report, "s2").Should().BeApproximately(0.8, 1e-9);
    }

    [Fact]
    public void TheOlderMeanParameters_AreRead()
    {
        TopologyDefinition topology = Topology(
            new[] { Node("in", "source", ("mean_interval", 2.0)), Queue("q"), Node("s", "server", ("service_time", 1.5)), Sink("out") },
            ("in", "q", null), ("q", "s", null), ("s", "out", null));

        Load(Check(topology), "s").Should().BeApproximately(0.75, 1e-9);
    }
}
