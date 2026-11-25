using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Simulation.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-126 schema, slice 3 (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, rule 3): patience on
/// a <c>seize</c> or <c>take</c> step, with the <c>outlet</c> an item that gives up leaves by, holding
/// nothing; a connection carrying <c>"outlet"</c> leaves the process by that exit. Outlets are distinct
/// exits, so a process with a normal connection and outlet connections does not duplicate items. The
/// <c>share</c> metric at a sink is the share of the items reaching any sink that reach this one. The
/// acceptance test is Erlang's loss formula: three servers whose customers wait not at all.
/// </summary>
public class TopologyPatienceTests
{
    private readonly ITestOutputHelper output;

    public TopologyPatienceTests(ITestOutputHelper output) { this.output = output; }

    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    private static (ActiveModel? Model, ValidationReport Report) Build(string json)
    {
        var report = new ValidationReport();
        return (ModelRegistry.TryBuild(Read(json), report), report);
    }

    /// <summary>The car wash of the brief without its supplier (slice 4): drivers give up after 5-20 minutes.</summary>
    private const string CarWash = """
        {"schema_version": "1", "name": "car wash", "seed": 7,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 3}}},
                   {"id": "bays", "type": "resource", "params": {"capacity": 2}},
                   {"id": "wash", "type": "process", "steps": [
                       {"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"},
                       {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 8}}]},
                   {"id": "washed", "type": "sink"},
                   {"id": "left", "type": "sink"}],
         "connections": [["arrivals", "wash"], ["wash", "washed"], {"from": "wash", "to": "left", "outlet": "gave_up"}],
         "metrics": [{"id": "gave-up", "kind": "share", "node": "left"},
                     {"id": "wait-for-bay", "kind": "wait_time", "node": "bays"}]}
        """;

    [Fact]
    public void AnOutletConnection_BindsFromJson_AndIsWrittenBack()
    {
        TopologyDefinition t = Read(CarWash);

        ConnectionDefinition outlet = t.Connections[2];
        (outlet.From, outlet.To, outlet.Outlet).Should().Be(("wash", "left", "gave_up"));
        t.Connections[1].Outlet.Should().BeNull();
        string written = ModelDocument.Write(t);
        written.Should().Contain("\"outlet\": \"gave_up\"");
        ModelDocument.Write(Read(written)).Should().Be(written);
    }

    [Fact]
    public void DriversWhoWaitedTooLong_LeaveByTheOutlet_AndEveryCarIsAccountedFor()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Read(CarWash));
        ActiveModel active = registry.Get(id);
        int made = 0;
        foreach (var source in active.Sources.Values) source.EntityCreatedEvent.AddHandler((_, _) => made++);

        JsonElement stats = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, 6_000)).RootElement.GetProperty("stats");

        int washed = stats.GetProperty("sinks").GetProperty("washed").GetInt32();
        int left = stats.GetProperty("sinks").GetProperty("left").GetInt32();
        int inside = stats.GetProperty("processes").GetProperty("wash").GetProperty("inside").GetInt32();
        output.WriteLine($"made {made}, washed {washed}, left {left}, inside {inside}");
        (washed + left + inside).Should().Be(made);
        left.Should().BeGreaterThan(0, "two bays at a load of 1 cannot keep every driver");
        washed.Should().BeGreaterThan(left);
        active.Resources["bays"].InUse.Should().BeLessThanOrEqualTo(inside);
    }

    [Fact]
    public void TheShareMetric_IsTheShareOfFinishedItemsAtTheSink_AndTheWaitCountsOnlyThoseServed()
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read(CarWash)));

        ReplicationResult run = new ReplicationRunner(active.Model, runLength: 6_000d, warmupTime: 600d).Run(5, 7);

        double share = run[active.Metrics["gave-up"]].Mean;
        double wait = run[active.Metrics["wait-for-bay"]].Mean;
        output.WriteLine($"share who gave up {share:F4}, wait for a bay {wait:F3}");
        share.Should().BeInRange(0.05, 0.5);
        wait.Should().BeInRange(0d, 20d, "a driver who was served waited less than the longest patience");
        active.Metrics["gave-up"].Should().Be("left.stat." + Instrumentation.Share);
    }

    [Fact]
    public void ThreeServersWhoseCustomersDoNotWait_LoseTheShareErlangsFormulaGives()
    {
        // λ = 2.4, μ = 1, c = 3, patience 0: an arrival that finds every unit busy leaves at once, so
        // the share lost is Erlang B(3, 2.4) = (a³/3!) / (1 + a + a²/2 + a³/6) with a = 2.4
        const double a = 2.4;
        double erlangB = a * a * a / 6 / (1 + a + a * a / 2 + a * a * a / 6);
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read("""
            {"name": "erlang loss", "seed": 3,
             "nodes": [{"id": "calls", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 0.4166666666666667}}},
                       {"id": "lines", "type": "resource", "params": {"capacity": 3}},
                       {"id": "call", "type": "process", "steps": [
                           {"type": "seize", "resource": "lines", "patience": {"type": "constant", "value": 0}, "outlet": "busy"},
                           {"type": "delay", "duration": {"type": "exponential", "mean": 1}}]},
                       {"id": "served", "type": "sink"}, {"id": "lost", "type": "sink"}],
             "connections": [["calls", "call"], ["call", "served"], {"from": "call", "to": "lost", "outlet": "busy"}],
             "metrics": [{"id": "lost", "kind": "share", "node": "lost"}, {"id": "busy", "kind": "utilization", "node": "lines"}]}
            """)));

        ReplicationResult run = new ReplicationRunner(active.Model, runLength: 10_100d, warmupTime: 100d).Run(20, 20_260_826);

        double[] lost = run[active.Metrics["lost"]].ReplicateEstimates.ToArray();
        double gate = ReplicationRunner.ConfidenceHalfWidth(lost, 0.999)!.Value;
        output.WriteLine($"lost: Erlang B {erlangB:F5}, simulated {lost.Average():F5} ± {ReplicationRunner.ConfidenceHalfWidth(lost, 0.95):F5}");
        Math.Abs(lost.Average() - erlangB).Should().BeLessThanOrEqualTo(gate);
        run[active.Metrics["busy"]].Mean.Should().BeApproximately(a * (1 - erlangB) / 3, 0.01, "the carried load over three lines");
    }

    // ── wiring ───────────────────────────────────────────────────────────────

    [Fact]
    public void ANormalAndAnOutletConnectionToOneNode_AreTwoConnections_NotARepeat()
    {
        ValidationReport report = ModelValidator.Validate(Read(CarWash.Replace(
            """{"from": "wash", "to": "left", "outlet": "gave_up"}""", """{"from": "wash", "to": "washed", "outlet": "gave_up"}""")
            .Replace("""{"id": "gave-up", "kind": "share", "node": "left"},""", "")));

        report.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TwoConnectionsForOneOutlet_WouldDuplicateTheItemsThatGiveUp_AndAreRefused()
    {
        ValidationReport report = ModelValidator.Validate(Read(CarWash.Replace(
            """{"from": "wash", "to": "left", "outlet": "gave_up"}""",
            """{"from": "wash", "to": "left", "outlet": "gave_up"}, {"from": "wash", "to": "washed", "outlet": "gave_up"}""")));

        report.Errors.Should().Contain(e => e.Code == "FAN_OUT_WITHOUT_ROUTER" && e.Node == "wash" && e.Message.Contains("gave_up"));
    }

    [Fact]
    public void AProcessWithOnlyAnOutletConnection_WarnsThatFinishedItemsStopThere()
    {
        ValidationReport report = ModelValidator.Validate(Read(CarWash.Replace("""["wash", "washed"], """, "")));

        report.Warnings.Should().Contain(w => w.Code == "DEAD_END" && w.Node == "wash");
    }

    [Theory]
    [InlineData("""{"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"}""",
                """{"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}}""", "PATIENCE_WITHOUT_OUTLET", "outlet")]
    [InlineData("""{"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"}""",
                """{"type": "seize", "resource": "bays", "outlet": "gave_up"}""", "OUTLET_WITHOUT_PATIENCE", "patience")]
    [InlineData("""{"type": "uniform", "min": 5, "max": 20}""", """{"type": "uniform", "min": -5, "max": 20}""", "INVALID_STEP_FIELD", "below 0")]
    [InlineData("""{"from": "wash", "to": "left", "outlet": "gave_up"}""", """{"from": "wash", "to": "left", "outlet": "bored"}""", "UNKNOWN_OUTLET", "gave_up")]
    [InlineData("""{"from": "wash", "to": "left", "outlet": "gave_up"}""", """{"from": "arrivals", "to": "left", "outlet": "gave_up"}""", "OUTLET_NOT_READ", "process")]
    [InlineData(""", {"from": "wash", "to": "left", "outlet": "gave_up"}""", "", "OUTLET_NOT_CONNECTED", "outlet")]
    public void AnOutletFault_IsRefused_NamingTheFix(string replace, string with, string code, string named)
    {
        var (active, report) = Build(CarWash.Replace(replace, with));

        active.Should().BeNull();
        report.Errors.Should().Contain(e => e.Code == code && e.Line.Contains(named), string.Join("; ", report.Errors.Select(e => e.Code)));
    }

    [Fact]
    public void TheShareMetric_IsMeasuredAtASink()
    {
        var (active, report) = Build(CarWash.Replace("""{"id": "gave-up", "kind": "share", "node": "left"}""",
                                                     """{"id": "gave-up", "kind": "share", "node": "bays"}"""));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Message.Should().Contain("share").And.Contain("sink");
    }

    [Fact]
    public void AnOutletConnection_IsPatchedAndRemoved_ByItsOutlet()
    {
        TopologyDefinition t = Read(CarWash);
        var report = new ValidationReport();
        var changes = new List<string>();

        TopologyDefinition? patched = TopologyPatcher.Apply(t, new[]
        {
            new PatchOperation { Op = "remove_connection", From = "wash", To = "left", Outlet = "gave_up" },
            new PatchOperation { Op = "set_connection", From = "wash", To = "washed", Outlet = "gave_up" },
        }, report, changes);

        report.Errors.Should().BeEmpty();
        patched!.Connections.Select(c => (c.From, c.To, c.Outlet)).Should().BeEquivalentTo(new (string, string, string?)[]
        {
            ("arrivals", "wash", null), ("wash", "washed", null), ("wash", "washed", "gave_up"),
        });
        changes.Should().OnlyContain(c => c.Contains("gave_up"));
    }

    [Fact]
    public void TheLimits_SayWhoMayLeaveEarly()
    {
        SchemaCatalog.Limits.Should().Contain(l => l.Contains("patience"));
    }
}
