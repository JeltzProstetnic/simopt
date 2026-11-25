using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-66 step 2 (D4): structural faults are found before a run (UN-008: "unreachable stations, no
/// exit, disconnected flow are reported before running"). Errors where the model cannot be what was
/// meant (no source, an item copied into every branch, a connection listed twice); warnings where it
/// runs but loses or strands items (no sink, unreachable, dead end, unconnected, a queue nobody takes
/// from). One fault gives one issue: an unconnected node is not also reported as unreachable and as
/// a dead end.
/// </summary>
public class TopologyLintTests
{
    private static DistributionSpec Exponential(double mean) => new DistributionSpec { Type = "exponential", Mean = mean };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static NodeDefinition Source(string id) => Node(id, "source", ("arrival", Exponential(2)));
    private static NodeDefinition Queue(string id) => Node(id, "queue");
    private static NodeDefinition Station(string id) => Node(id, "station", ("service", Exponential(1)));
    private static NodeDefinition Sink(string id) => Node(id, "sink");

    private static TopologyDefinition Topology(IEnumerable<NodeDefinition> nodes, params (string From, string To)[] connections) =>
        new TopologyDefinition
        {
            Name = "lint",
            Seed = 7,
            Nodes = nodes.ToList(),
            Connections = connections.Select(c => new ConnectionDefinition { From = c.From, To = c.To }).ToList(),
        };

    private static readonly NodeDefinition[] LineNodes = { Source("in"), Queue("q"), Station("s"), Sink("out") };
    private static readonly (string, string)[] LineWires = { ("in", "q"), ("q", "s"), ("s", "out") };

    private static ValidationReport Check(TopologyDefinition topology)
    {
        var report = new ValidationReport();
        ModelRegistry.TryBuild(topology, report);
        return report;
    }

    private static IEnumerable<(string Code, string? Node)> Issues(IEnumerable<ValidationIssue> issues) =>
        issues.Select(i => (i.Code, i.Node));

    [Fact]
    public void ACleanLine_HasNoErrorsAndNoWarnings()
    {
        ValidationReport report = Check(Topology(LineNodes, LineWires));

        report.Errors.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void WithoutASource_NothingEverEnters_AndTheModelIsRefused()
    {
        ValidationReport report = Check(Topology(new[] { Queue("q"), Station("s"), Sink("out") }, ("q", "s"), ("s", "out")));

        Issues(report.Errors).Should().Equal(("NO_SOURCE", null));
        report.Errors[0].Suggestion.Should().Contain("source");
    }

    [Fact]
    public void WithoutASink_ItemsStayInTheSystem_AWarning_AndNoDeadEndPerNode()
    {
        ValidationReport report = Check(Topology(new[] { Source("in"), Queue("q"), Station("s") }, ("in", "q"), ("q", "s")));

        report.Errors.Should().BeEmpty();
        Issues(report.Warnings).Should().Equal(("NO_SINK", null));
    }

    [Fact]
    public void APushNodeWiredToTwoNodes_DuplicatesEveryItem_InTheEngine()
    {
        // Why D4 refuses it: each node downstream subscribes to the release of the node upstream,
        // so every item is put into each of them.
        var model = new Model("fan-out", 1, 0d) { LoggingEnabled = false };
        int made = 0;
        var source = new SimpleSource(model, new ConstantDoubleDistribution(1d, initialize: false),
            () => { made++; return new SimpleEntity(model, $"E{made}", $"E{made}"); }, autoStartDelay: 0d);
        int released = 0;
        source.EntityCreatedEvent.AddHandler((_, _) => released++);
        var a = new SimpleSink(model, "a", "a");
        var b = new SimpleSink(model, "b", "b");
        a.ConnectTo(source);
        b.ConnectTo(source);

        model.Run(9.5);

        released.Should().BeGreaterThan(0);
        a.Count.Should().Be(released);
        b.Count.Should().Be(released, "the same items reach the second sink too");
    }

    [Theory]
    [InlineData("in")]
    [InlineData("s")]
    public void APushNodeWithTwoConnectionsLeavingIt_IsRefused_NamingTheRouterToInsert(string fanning)
    {
        var nodes = LineNodes.Concat(new[] { Queue("q2"), Sink("out2") });
        var wires = LineWires.Concat(new[] { (fanning, fanning == "in" ? "q2" : "out2") });
        if (fanning == "in") wires = wires.Append(("q2", "s"));

        ValidationIssue issue = Check(Topology(nodes, wires.ToArray())).Errors.Should().ContainSingle().Which;

        issue.Code.Should().Be("FAN_OUT_WITHOUT_ROUTER");
        issue.Node.Should().Be(fanning);
        issue.Suggestion.Should().Contain("router");
    }

    [Fact]
    public void AQueueFeedingTwoStations_IsNotAFanOut_StationsTakeFromIt()
    {
        ValidationReport report = Check(Topology(LineNodes.Append(Station("s2")),
            LineWires.Concat(new[] { ("q", "s2"), ("s2", "out") }).ToArray()));

        report.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AConnectionListedTwice_IsRefused_AndNotAlsoCalledAFanOut()
    {
        ValidationReport report = Check(Topology(LineNodes, LineWires.Append(("in", "q")).ToArray()));

        Issues(report.Errors).Should().Equal(("DUPLICATE_CONNECTION", "in"));
    }

    [Fact]
    public void ANodeNoItemCanReach_IsReported()
    {
        ValidationReport report = Check(Topology(LineNodes.Append(Queue("q9")).Append(Station("s9")),
            LineWires.Concat(new[] { ("q9", "s9"), ("s9", "out") }).ToArray()));

        report.Errors.Should().BeEmpty();
        Issues(report.Warnings).Should().BeEquivalentTo(new[] { ("UNREACHABLE", "q9"), ("UNREACHABLE", "s9") });
    }

    [Fact]
    public void ABranchLeadingNowhere_IsADeadEnd_WhereItsItemsStop()
    {
        ValidationReport report = Check(Topology(
            new[] { Source("in"), Node("r", "router", ("policy", "probabilistic")), Queue("q"), Station("s"), Sink("out"),
                    Queue("q2"), Station("s2") },
            ("in", "r"), ("r", "q"), ("q", "s"), ("s", "out"), ("r", "q2"), ("q2", "s2")));

        report.Errors.Should().BeEmpty();
        Issues(report.Warnings).Should().Equal(("DEAD_END", "s2"));
        report.Warnings[0].Suggestion.Should().Contain("sink");
    }

    [Fact]
    public void ALoopWithNoWayOut_IsReported_NamingItsNodes()
    {
        ValidationReport report = Check(Topology(
            new[] { Source("in"), Queue("q"), Station("s"), Node("d", "delay", ("duration", Exponential(1))), Sink("out") },
            ("in", "q"), ("q", "s"), ("s", "d"), ("d", "q")));

        report.Errors.Should().BeEmpty();
        Issues(report.Warnings).Should().BeEquivalentTo(new[] { ("UNCONNECTED", "out"), ("LOOP_WITHOUT_EXIT", "q") });
        report.Warnings.Single(w => w.Code == "LOOP_WITHOUT_EXIT").Message
              .Should().Contain("'q', 's', 'd'").And.NotContain("'in'");
    }

    [Fact]
    public void AReworkLoopWithAnExit_IsFine()
    {
        // half the items come back, so the station sees twice the arrival rate: 0.5 x 2 x 0.5 = load 0.5
        ValidationReport report = Check(Topology(
            new[] { Source("in"), Queue("q"), Node("s", "station", ("service", Exponential(0.5))),
                    Node("r", "router", ("policy", "probabilistic")), Sink("out") },
            ("in", "q"), ("q", "s"), ("s", "r"), ("r", "q"), ("r", "out")));

        report.Errors.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ANodeWithoutConnections_IsUnconnected_AndOnlyThat()
    {
        ValidationReport report = Check(Topology(LineNodes.Append(Station("idle")), LineWires));

        Issues(report.Warnings).Should().Equal(("UNCONNECTED", "idle"));
    }

    [Fact]
    public void AQueueNoStationTakesFrom_OnlyFills()
    {
        ValidationReport report = Check(Topology(LineNodes.Append(Source("in2")).Append(Queue("full")),
            LineWires.Append(("in2", "full")).ToArray()));

        Issues(report.Warnings).Should().Equal(("QUEUE_NOT_TAKEN", "full"));
    }

    [Fact]
    public void LintIssues_CarryASuggestion()
    {
        ValidationReport report = Check(Topology(new[] { Source("in"), Queue("q"), Station("s"), Station("idle") },
            ("in", "q"), ("q", "s")));

        report.Warnings.Should().NotBeEmpty().And.OnlyContain(w => !string.IsNullOrWhiteSpace(w.Suggestion));
    }

    [Fact]
    public void Create_StillBuildsAModelWithWarnings()
    {
        var registry = new ModelRegistry();
        var report = new ValidationReport();

        string id = registry.Create(Topology(LineNodes.Append(Station("idle")), LineWires), report);

        registry.Get(id).Should().NotBeNull();
        report.Warnings.Should().ContainSingle();
    }
}
