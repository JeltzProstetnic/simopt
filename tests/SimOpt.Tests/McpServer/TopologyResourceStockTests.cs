using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Simulation.Processes;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-126 schema, slice 1 (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, Q-PROC-SCHEMA): a
/// <c>resource</c> (units held by a process: bays, nurses, forklifts) and a <c>stock</c> (a level a
/// process takes from and puts into) are nodes of the topology, but not on the item flow: nothing is
/// connected to them. They are patchable and assumed like every node parameter, report their state
/// in <c>run_simulation</c> and <c>get_status</c>, and are measured by the metrics a queue and station
/// have (<c>utilization</c>, <c>wait_time</c>, <c>queue_length</c> at a resource) and two of their own
/// (<c>level</c>, <c>empty_share</c> at a stock). The process node that uses them is slice 2; here a C#
/// process drives them.
/// </summary>
public class TopologyResourceStockTests
{
    /// <summary>A line with a resource and a stock beside it, plus one of each left to its defaults.</summary>
    private const string Washroom = """
        {"schema_version": "1", "name": "washroom", "seed": 7,
         "nodes": [{"id": "in", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 1}}},
                   {"id": "out", "type": "sink"},
                   {"id": "bays", "type": "resource", "params": {"capacity": 2}},
                   {"id": "pool", "type": "resource"},
                   {"id": "soap", "type": "stock", "params": {"initial": 40, "capacity": 40}},
                   {"id": "bin", "type": "stock"}],
         "connections": [["in", "out"]]}
        """;

    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    private static (ActiveModel? Model, ValidationReport Report) Build(string json)
    {
        var report = new ValidationReport();
        return (ModelRegistry.TryBuild(Read(json), report), report);
    }

    private static string WithNode(string node) =>
        Washroom.Replace("""{"id": "bin", "type": "stock"}""", """{"id": "bin", "type": "stock"}, """ + node);

    [Fact]
    public void AResourceAndAStock_Build_WithTheirParametersAndDefaults()
    {
        var (active, report) = Build(Washroom);

        report.Errors.Should().BeEmpty();
        active!.Resources["bays"].Capacity.Should().Be(2);
        active.Resources["pool"].Capacity.Should().Be(1, "a resource has one unit by default");
        active.Stocks["soap"].Level.Should().Be(40);
        active.Stocks["soap"].Capacity.Should().Be(40);
        active.Stocks["bin"].Level.Should().Be(0, "a stock starts empty by default");
        active.Stocks["bin"].Capacity.Should().Be(double.PositiveInfinity);
    }

    [Theory]
    [InlineData("""{"capacity": 0}""")]
    [InlineData("""{"capacity": 2.5}""")]
    [InlineData("""{"capacity": 10001}""")]
    [InlineData("""{"capacity": "two"}""")]
    public void AResourceCapacity_IsAWholeNumberFrom1To10000(string parameters)
    {
        var (active, report) = Build(WithNode("""{"id": "crew", "type": "resource", "params": """ + parameters + "}"));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Should().Match<ValidationIssue>(e =>
            e.Code == "INVALID_PARAMETER" && e.Node == "crew" && e.Message.Contains("capacity"));
    }

    [Theory]
    [InlineData("""{"initial": -1}""", "initial")]
    [InlineData("""{"initial": 50, "capacity": 40}""", "initial")]
    [InlineData("""{"capacity": 0}""", "capacity")]
    [InlineData("""{"capacity": "full"}""", "capacity")]
    public void AStock_StartsBetweenEmptyAndItsCapacity_WhichIsPositive(string parameters, string named)
    {
        var (active, report) = Build(WithNode("""{"id": "tank", "type": "stock", "params": """ + parameters + "}"));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Should().Match<ValidationIssue>(e =>
            e.Code == "INVALID_PARAMETER" && e.Node == "tank" && e.Message.Contains(named));
    }

    [Theory]
    [InlineData("in", "bays", "bays", "seize")]
    [InlineData("bays", "out", "bays", "seize")]
    [InlineData("in", "soap", "soap", "take")]
    [InlineData("soap", "out", "soap", "take")]
    public void AConnectionToOrFromAResourceOrAStock_IsRefused_NamingWhatUsesIt(string from, string to, string node, string step)
    {
        var (active, report) = Build(Washroom.Replace("""[["in", "out"]]""", $$"""[["in", "out"], ["{{from}}", "{{to}}"]]"""));

        active.Should().BeNull();
        ValidationIssue issue = report.Errors.Should().ContainSingle().Subject;
        issue.Code.Should().Be("NOT_ON_ITEM_FLOW");
        issue.Node.Should().Be(node);
        issue.Message.Should().Contain($"'{from}'").And.Contain($"'{to}'").And.Contain("item flow");
        issue.Suggestion.Should().Contain(step).And.Contain($"'{node}'");
    }

    [Fact]
    public void ValidateModel_RaisesNoStructuralWarningAboutNodesBesideTheFlow()
    {
        ValidationReport report = ModelValidator.Validate(Read(Washroom));

        report.Valid.Should().BeTrue();
        report.Warnings.Should().NotContain(w => new[] { "bays", "pool", "soap", "bin" }.Contains(w.Node),
            "a resource and a stock are used by process steps, not connected");
    }

    private const string Measured = """
        {"name": "measured", "seed": 7,
         "nodes": [{"id": "in", "type": "source", "params": {"arrival": {"type": "constant", "value": 100}}},
                   {"id": "out", "type": "sink"},
                   {"id": "bay", "type": "resource"},
                   {"id": "soap", "type": "stock", "params": {"initial": 10, "capacity": 10}}],
         "connections": [["in", "out"]],
         "metrics": [{"id": "busy", "kind": "utilization", "node": "bay"},
                     {"id": "wait", "kind": "wait_time", "node": "bay"},
                     {"id": "waiting", "kind": "queue_length", "node": "bay"},
                     {"id": "level", "kind": "level", "node": "soap"},
                     {"id": "dry", "kind": "empty_share", "node": "soap"}]}
        """;

    private static IEnumerable<Wait> Wash(Process p, Resource bay, double start, double hold)
    {
        if (start > 0) yield return p.Timeout(start);
        yield return bay.Request();
        yield return p.Timeout(hold);
        bay.Release();
    }

    private static IEnumerable<Wait> Refill(Process p, SimOpt.Simulation.Templates.Stock soap)
    {
        yield return p.Timeout(2);
        yield return soap.TakeWait(10);   // empty from 2
        yield return p.Timeout(4);
        yield return soap.PutWait(5);     // 5 from 6
    }

    [Fact]
    public void TheMetricsOfAResourceAndAStock_MeasureWhatTheProcessesUsingThemDo()
    {
        var (active, report) = Build(Measured);
        report.Errors.Should().BeEmpty();
        Resource bay = active!.Resources["bay"];
        active.Model.StartProcess(p => Wash(p, bay, 0, 4), id: "A");   // holds [0, 4)
        active.Model.StartProcess(p => Wash(p, bay, 1, 2), id: "B");   // waits [1, 4), holds [4, 6)
        active.Model.StartProcess(p => Refill(p, active.Stocks["soap"]), id: "C");

        ReplicationResult run = new ReplicationRunner(active.Model, runLength: 10d, warmupTime: 0d).Run(2, 7);

        active.Metrics.Should().Contain(new Dictionary<string, string>
        {
            ["busy"] = "bay.stat." + Instrumentation.Utilization,
            ["wait"] = "bay.stat." + Instrumentation.WaitTime,
            ["waiting"] = "bay.stat." + Instrumentation.QueueLength,
            ["level"] = "soap.stat." + Instrumentation.StockLevel,
            ["dry"] = "soap.stat." + Instrumentation.StockEmpty,
        });
        double Mean(string metric) => run[active.Metrics[metric]].Mean;
        Mean("busy").Should().BeApproximately(0.6, 1e-12, "the bay is held 4 + 2 of 10");
        Mean("wait").Should().BeApproximately(1.5, 1e-12, "A waited 0 and B 3");
        Mean("waiting").Should().BeApproximately(0.3, 1e-12, "one request waited in [1, 4)");
        Mean("level").Should().BeApproximately((10 * 2 + 0 * 4 + 5 * 4) / 10d, 1e-12);
        Mean("dry").Should().BeApproximately(0.4, 1e-12, "the tank stood empty in [2, 6)");
    }

    [Theory]
    [InlineData("level", "bay", "stock")]
    [InlineData("empty_share", "in", "stock")]
    [InlineData("wait_time", "soap", "resource")]
    [InlineData("queue_length", "soap", "resource")]
    [InlineData("utilization", "soap", "resource")]
    public void AMetricAtANodeThatDoesNotHaveIt_IsRefused_NamingWhereItIsMeasured(string kind, string node, string needs)
    {
        var (active, report) = Build(Measured.Replace("""{"id": "dry", "kind": "empty_share", "node": "soap"}""",
            $$"""{"id": "dry", "kind": "empty_share", "node": "soap"}, {"id": "m", "kind": "{{kind}}", "node": "{{node}}"}"""));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Message.Should()
              .Contain("'m'").And.Contain(kind).And.Contain($"'{node}'").And.Contain(needs);
    }

    [Fact]
    public void ACapacityAndALevel_ArePatchPaths()
    {
        var report = new ValidationReport();
        TopologyDefinition? patched = TopologyPatcher.Apply(Read(Washroom), new[]
        {
            new PatchOperation { Op = "set", Path = "bays.capacity", Value = JsonDocument.Parse("3").RootElement },
            new PatchOperation { Op = "set", Path = "soap.initial", Value = JsonDocument.Parse("25").RootElement },
        }, report, new List<string>());

        report.Errors.Should().BeEmpty();
        ActiveModel active = ModelRegistry.TryBuild(patched!, report)!;
        active.Resources["bays"].Capacity.Should().Be(3);
        active.Stocks["soap"].Level.Should().Be(25);
    }

    [Fact]
    public void TheParametersLeftOut_AreListedAsAssumed()
    {
        Dictionary<string, string> assumed = ModelDocument.Defaults(Read(Washroom)).ToDictionary(p => p.Key, p => p.Value);

        assumed.Should().Contain(new Dictionary<string, string>
        {
            ["pool.capacity"] = "1",
            ["bin.initial"] = "0",
            ["bin.capacity"] = "unlimited",
        });
        assumed.Keys.Should().NotContain(new[] { "bays.capacity", "soap.initial", "soap.capacity" });
    }

    [Fact]
    public void WithoutDeclaredMetrics_AResourceAndAStockAreMeasuredByDefault()
    {
        TopologyDefinition t = Read(Washroom);

        ExperimentRunner.AddDefaultMetrics(t);

        t.Metrics.Select(m => (m.Id, m.Kind, m.Node)).Should().Contain(new (string, string, string?)[]
        {
            ("util_bays", "utilization", "bays"), ("wait_bays", "wait_time", "bays"),
            ("level_soap", "level", "soap"), ("empty_soap", "empty_share", "soap"),
        });
        new Action(() => new ModelRegistry().Create(t)).Should().NotThrow();
    }

    [Fact]
    public void RunSimulationAndGetStatus_ReportTheirState()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Read(Washroom));
        var tools = new SimulationTools(registry);

        JsonElement run = JsonDocument.Parse(tools.RunSimulation(id, 10)).RootElement;
        JsonElement status = JsonDocument.Parse(tools.GetStatus(id)).RootElement;

        JsonElement stats = run.GetProperty("stats");
        stats.GetProperty("resources").GetProperty("bays").GetProperty("capacity").GetInt32().Should().Be(2);
        stats.GetProperty("resources").GetProperty("bays").GetProperty("in_use").GetInt32().Should().Be(0);
        stats.GetProperty("resources").GetProperty("bays").GetProperty("waiting").GetInt32().Should().Be(0);
        stats.GetProperty("stocks").GetProperty("soap").GetProperty("level").GetDouble().Should().Be(40);
        JsonElement[] nodes = status.GetProperty("nodes").EnumerateArray().ToArray();
        nodes.Should().Contain(n => n.GetProperty("id").GetString() == "bays" && n.GetProperty("type").GetString() == "resource"
                                    && n.GetProperty("in_use").GetInt32() == 0);
        nodes.Should().Contain(n => n.GetProperty("id").GetString() == "soap" && n.GetProperty("type").GetString() == "stock"
                                    && n.GetProperty("level").GetDouble() == 40);
    }
}
