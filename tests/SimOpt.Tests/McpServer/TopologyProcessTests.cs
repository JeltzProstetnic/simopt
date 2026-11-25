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
/// SIM-126 schema, slice 2 (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, Q-PROC-SCHEMA): a
/// <c>process</c> node runs every item it receives through a closed list of steps (<c>delay</c>,
/// <c>seize</c>, <c>release</c>, <c>take</c>, <c>put</c>) and passes it on like a delay. An item
/// leaving frees every unit it holds; step values are drawn on entry from streams keyed to the node and
/// the step id (<c>&lt;type&gt;#&lt;n&gt;</c> when none is given). The acceptance test is an M/M/3
/// written as a process, a resource of 3 and one delay, against Erlang-C, as the station is in the
/// analytic battery. Every fault in the steps is reported at once, with the fix.
/// </summary>
public class TopologyProcessTests
{
    private readonly ITestOutputHelper output;

    public TopologyProcessTests(ITestOutputHelper output) { this.output = output; }

    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    private static (ActiveModel? Model, ValidationReport Report) Build(string json)
    {
        var report = new ValidationReport();
        return (ModelRegistry.TryBuild(Read(json), report), report);
    }

    /// <summary>A wash with two bays and a soap tank; the steps go where {STEPS} is.</summary>
    private const string Wash = """
        {"schema_version": "1", "name": "wash", "seed": 7,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 3}}},
                   {"id": "bays", "type": "resource", "params": {"capacity": 2}},
                   {"id": "soap", "type": "stock", "params": {"initial": 40, "capacity": 40}},
                   {"id": "wash", "type": "process", "steps": {STEPS}},
                   {"id": "washed", "type": "sink"}],
         "connections": [["arrivals", "wash"], ["wash", "washed"]]}
        """;

    private const string WashSteps = """
        [{"type": "seize", "resource": "bays"},
         {"type": "take", "stock": "soap", "amount": 1},
         {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 6}},
         {"type": "release", "resource": "bays"},
         {"type": "put", "stock": "soap", "amount": {"type": "constant", "value": 1}}]
        """;

    private static string WashWith(string steps) => Wash.Replace("{STEPS}", steps);

    [Fact]
    public void Steps_BindFromJson_AndAreWrittenBackAsTheyWereRead()
    {
        TopologyDefinition t = Read(WashWith(WashSteps));

        List<StepDefinition> steps = t.Nodes.Single(n => n.Id == "wash").Steps!;
        steps.Select(s => (s.Id, s.Type)).Should().Equal((null, "seize"), (null, "take"), ("washing", "delay"), (null, "release"), (null, "put"));
        steps[0].Fields["resource"].Text.Should().Be("bays");
        steps[1].Fields["amount"].Number.Should().Be(1);
        steps[2].Fields["duration"].Distribution!.Max.Should().Be(6);

        string written = ModelDocument.Write(t);
        TopologyDefinition again = Read(written);
        ModelDocument.Write(again).Should().Be(written);
        written.Should().Contain("\"steps\"").And.Contain("\"washing\"");
    }

    [Fact]
    public void ADocumentWithoutAProcess_IsWrittenWithoutSteps_SoItsHashIsUnchanged()
    {
        ModelDocument.Write(Read(Wash.Replace("""{"id": "wash", "type": "process", "steps": {STEPS}},""", "")
                                     .Replace("""["arrivals", "wash"], ["wash", "washed"]""", """["arrivals", "washed"]""")))
                     .Should().NotContain("steps");
    }

    [Fact]
    public void AProcessBuilds_WithItsStepsNamedByIdOrByTypeAndPosition()
    {
        var (active, report) = Build(WashWith(WashSteps));

        report.Errors.Should().BeEmpty();
        active!.Processes["wash"].Steps.Select(s => s.Id).Should().Equal("seize#1", "take#1", "washing", "release#1", "put#1");
    }

    [Fact]
    public void ItemsFlowThroughAProcess_AndLeaveHoldingNothing()
    {
        var registry = new ModelRegistry();
        string id = registry.Create(Read(WashWith(WashSteps)));
        ActiveModel active = registry.Get(id);
        var tools = new SimulationTools(registry);

        JsonElement run = JsonDocument.Parse(tools.RunSimulation(id, 600)).RootElement.GetProperty("stats");
        JsonElement status = JsonDocument.Parse(tools.GetStatus(id)).RootElement;

        int washed = run.GetProperty("sinks").GetProperty("washed").GetInt32();
        int inside = run.GetProperty("processes").GetProperty("wash").GetProperty("inside").GetInt32();
        washed.Should().BeInRange(150, 250, "an arrival every 3 minutes over 600");
        active.Resources["bays"].InUse.Should().BeLessThanOrEqualTo(inside);
        active.Stocks["soap"].Level.Should().BeInRange(0, 40);
        status.GetProperty("nodes").EnumerateArray().Should().Contain(n =>
            n.GetProperty("id").GetString() == "wash" && n.GetProperty("type").GetString() == "process"
            && n.GetProperty("inside").GetInt32() == inside);
    }

    // ── acceptance: an M/M/3 written as a process agrees with Erlang-C ─────────

    private const string MM3Process = """
        {"name": "mm3 process", "seed": 1,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 0.4166666666666667}}},
                   {"id": "staff", "type": "resource", "params": {"capacity": 3}},
                   {"id": "serve", "type": "process", "steps": [
                       {"type": "seize", "resource": "staff"},
                       {"type": "delay", "duration": {"type": "exponential", "mean": 1}}]},
                   {"id": "done", "type": "sink"}],
         "connections": [["arrivals", "serve"], ["serve", "done"]],
         "metrics": [{"id": "wq", "kind": "wait_time", "node": "staff"},
                     {"id": "lq", "kind": "queue_length", "node": "staff"},
                     {"id": "rho", "kind": "utilization", "node": "staff"}]}
        """;

    private const string MM3Station = """
        {"name": "mm3 station", "seed": 1,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 0.4166666666666667}}},
                   {"id": "q", "type": "queue"},
                   {"id": "staff", "type": "station", "params": {"servers": 3, "service": {"type": "exponential", "mean": 1}}},
                   {"id": "done", "type": "sink"}],
         "connections": [["arrivals", "q"], ["q", "staff"], ["staff", "done"]],
         "metrics": [{"id": "wq", "kind": "wait_time", "node": "q"},
                     {"id": "lq", "kind": "queue_length", "node": "q"},
                     {"id": "rho", "kind": "utilization", "node": "staff"}]}
        """;

    private void AgreesWithErlangC(string topology, string what)
    {
        // λ = 2.4, μ = 1, c = 3: ρ = 0.8, the analytic battery's M/M/c (AnalyticBatteryTests.RunMMc)
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Read(topology)));
        ReplicationResult run = new ReplicationRunner(active.Model, runLength: 21_000d, warmupTime: 1_000d).Run(20, 20_260_826);

        foreach (var (metric, analytic) in new[]
                 {
                     ("wq", QueueingFormulas.MMc_Wq(2.4, 1, 3)),
                     ("lq", QueueingFormulas.MMc_Lq(2.4, 1, 3)),
                     ("rho", 0.8),
                 })
        {
            double[] estimates = run[active.Metrics[metric]].ReplicateEstimates.ToArray();
            double mean = estimates.Average();
            double gate = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.999)!.Value;
            double reported = ReplicationRunner.ConfidenceHalfWidth(estimates, 0.95)!.Value;
            output.WriteLine($"{what} {metric}: analytic {analytic:F5}, simulated {mean:F5} ± {reported:F5}");
            Math.Abs(mean - analytic).Should().BeLessThanOrEqualTo(gate, $"{what} {metric} {mean:F5} against Erlang-C {analytic:F5}");
            reported.Should().BeLessThanOrEqualTo(0.05 * analytic, $"{what} {metric}: an interval that wide is no evidence");
        }
    }

    [Fact]
    public void AnMM3WrittenAsAProcess_AgreesWithErlangC_AsTheStationDoes()
    {
        AgreesWithErlangC(MM3Process, "process");
        AgreesWithErlangC(MM3Station, "station");
    }

    [Fact]
    public void TwoProcessesSeizingOneResource_ShareItsUnits()
    {
        // two flows of 0.3 an hour, one hour each, on one nurse: she is busy 0.6 of the time
        var (active, report) = Build("""
            {"name": "shared", "seed": 3,
             "nodes": [{"id": "walk_in", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 3.3333333333333335}}},
                       {"id": "booked", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 3.3333333333333335}}},
                       {"id": "nurse", "type": "resource"},
                       {"id": "triage", "type": "process", "steps": [{"type": "seize", "resource": "nurse"}, {"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "check", "type": "process", "steps": [{"type": "seize", "resource": "nurse"}, {"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "home", "type": "sink"}],
             "connections": [["walk_in", "triage"], ["booked", "check"], ["triage", "home"], ["check", "home"]],
             "metrics": [{"id": "busy", "kind": "utilization", "node": "nurse"}]}
            """);
        report.Errors.Should().BeEmpty();

        ReplicationResult run = new ReplicationRunner(active!.Model, runLength: 10_100d, warmupTime: 100d).Run(5, 11);

        run[active.Metrics["busy"]].Mean.Should().BeApproximately(0.6, 0.03);
    }

    // ── wiring ───────────────────────────────────────────────────────────────

    [Fact]
    public void AProcessIsFedAndFeeds_AsADelayIs()
    {
        // source -> process -> queue -> station -> process -> process -> delay -> router -> {sink, process -> sink}
        var registry = new ModelRegistry();
        string id = registry.Create(Read("""
            {"name": "chain", "seed": 5,
             "nodes": [{"id": "in", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 2}}},
                       {"id": "p1", "type": "process", "steps": [{"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "q", "type": "queue"},
                       {"id": "s", "type": "station", "params": {"service": {"type": "exponential", "mean": 1}}},
                       {"id": "p2", "type": "process", "steps": [{"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "p3", "type": "process", "steps": [{"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "d", "type": "delay", "params": {"duration": {"type": "constant", "value": 1}}},
                       {"id": "r", "type": "router", "params": {"policy": "probabilistic"}},
                       {"id": "p4", "type": "process", "steps": [{"type": "delay", "duration": {"type": "constant", "value": 1}}]},
                       {"id": "a", "type": "sink"}, {"id": "b", "type": "sink"}],
             "connections": [["in", "p1"], ["p1", "q"], ["q", "s"], ["s", "p2"], ["p2", "p3"], ["p3", "d"], ["d", "r"],
                             ["r", "a"], ["r", "p4"], ["p4", "b"]]}
            """));
        var tools = new SimulationTools(registry);

        JsonElement sinks = JsonDocument.Parse(tools.RunSimulation(id, 2_000)).RootElement.GetProperty("stats").GetProperty("sinks");

        int a = sinks.GetProperty("a").GetInt32(), b = sinks.GetProperty("b").GetInt32();
        (a + b).Should().BeInRange(900, 1_100, "an arrival every 2 over 2,000, all of them through every node");
        a.Should().BeInRange((a + b) * 4 / 10, (a + b) * 6 / 10, "the router splits equally");
    }

    [Fact]
    public void AQueueFeedingAProcess_IsRefused_BecauseTheProcessWaitsAtItsSeizeStep()
    {
        var (active, report) = Build(WashWith(WashSteps)
            .Replace("""{"id": "washed", "type": "sink"}""", """{"id": "washed", "type": "sink"}, {"id": "q", "type": "queue"}""")
            .Replace("""["arrivals", "wash"]""", """["arrivals", "q"], ["q", "wash"]"""));

        active.Should().BeNull();
        ValidationIssue issue = report.Errors.Should().ContainSingle().Subject;
        issue.Code.Should().Be("INVALID_CONNECTION");
        issue.Message.Should().Contain("'q'").And.Contain("'wash'");
        issue.Suggestion.Should().Contain("'arrivals'").And.Contain("seize");
    }

    [Fact]
    public void AProcessWiredToTwoNodes_WouldDuplicateEveryItem_AndIsRefused()
    {
        ValidationReport report = ModelValidator.Validate(Read(WashWith(WashSteps)
            .Replace("""{"id": "washed", "type": "sink"}""", """{"id": "washed", "type": "sink"}, {"id": "other", "type": "sink"}""")
            .Replace("""["wash", "washed"]""", """["wash", "washed"], ["wash", "other"]""")));

        report.Errors.Should().Contain(e => e.Code == "FAN_OUT_WITHOUT_ROUTER" && e.Node == "wash");
    }

    // ── what the builder refuses in the steps, all at once ──────────────────────

    [Theory]
    [InlineData("""[]""", "MISSING_STEPS", "steps")]
    [InlineData("""[{"type": "jump"}]""", "UNKNOWN_STEP_TYPE", "seize")]
    [InlineData("""[{"type": "delay"}]""", "MISSING_STEP_FIELD", "duration")]
    [InlineData("""[{"type": "delay", "duration": 5}]""", "INVALID_STEP_FIELD", "constant")]
    [InlineData("""[{"type": "delay", "duration": {"type": "uniform", "min": -1, "max": 2}}]""", "INVALID_STEP_FIELD", "below 0")]
    [InlineData("""[{"type": "delay", "duration": {"type": "exponential", "mean": 1}, "speed": 2}]""", "UNKNOWN_STEP_FIELD", "duration")]
    [InlineData("""[{"type": "seize", "resource": "nobody"}]""", "UNKNOWN_RESOURCE", "'bays'")]
    [InlineData("""[{"type": "seize", "resource": "soap"}]""", "UNKNOWN_RESOURCE", "'bays'")]
    [InlineData("""[{"type": "release", "resource": "bays"}, {"type": "seize", "resource": "bays"}]""", "RELEASE_NOT_HELD", "release#1")]
    [InlineData("""[{"type": "take", "stock": "bays", "amount": 1}]""", "UNKNOWN_STOCK", "'soap'")]
    [InlineData("""[{"type": "take", "stock": "soap"}]""", "MISSING_STEP_FIELD", "amount")]
    [InlineData("""[{"type": "take", "stock": "soap", "amount": 0}]""", "INVALID_STEP_FIELD", "positive")]
    [InlineData("""[{"type": "take", "stock": "soap", "amount": 41}]""", "INVALID_STEP_FIELD", "capacity")]
    [InlineData("""[{"type": "put", "stock": "soap", "amount": {"type": "exponential", "mean": 1}}]""", "INVALID_STEP_FIELD", "capacity")]
    [InlineData("""[{"type": "seize", "resource": 3}]""", "INVALID_STEP_FIELD", "resource")]
    [InlineData("""[{"id": "x", "type": "delay", "duration": {"type": "constant", "value": 1}}, {"id": "x", "type": "seize", "resource": "bays"}]""", "DUPLICATE_STEP_ID", "'x'")]
    public void AFaultyStep_IsRefused_NamingTheNodeTheStepAndTheFix(string steps, string code, string named)
    {
        var (active, report) = Build(WashWith(steps));

        active.Should().BeNull();
        ValidationIssue issue = report.Errors.Should().ContainSingle(e => e.Code == code).Subject;
        issue.Node.Should().Be("wash");
        issue.Line.Should().Contain(named);
    }

    [Fact]
    public void AProcessWithoutSteps_AndStepsOnAnotherNode_AreRefused()
    {
        var (_, missing) = Build(Wash.Replace(""", "steps": {STEPS}}""", "}"));
        var (_, notRead) = Build(WashWith(WashSteps).Replace("""{"id": "washed", "type": "sink"}""",
            """{"id": "washed", "type": "sink", "steps": [{"type": "delay", "duration": {"type": "constant", "value": 1}}]}"""));

        missing.Errors.Should().ContainSingle(e => e.Code == "MISSING_STEPS" && e.Node == "wash");
        notRead.Errors.Should().ContainSingle(e => e.Code == "STEPS_NOT_READ" && e.Node == "washed");
    }

    [Fact]
    public void EveryFaultyStepOfAProcess_IsReportedInOneRound()
    {
        var (_, report) = Build(WashWith("""
            [{"type": "seize", "resource": "nobody"}, {"type": "delay"}, {"type": "take", "stock": "soap", "amount": -1}]
            """));

        report.Errors.Select(e => e.Code).Should().BeEquivalentTo("UNKNOWN_RESOURCE", "MISSING_STEP_FIELD", "INVALID_STEP_FIELD");
        report.Errors.Should().OnlyContain(e => e.Node == "wash");
    }

    [Fact]
    public void TheTrialRun_CountsTheItemsInsideAProcess()
    {
        ValidationReport report = ModelValidator.Validate(Read(WashWith(
            """[{"type": "delay", "duration": {"type": "constant", "value": 100000}}]""")));

        ValidationIssue trial = report.Info.Single(i => i.Code == "TRIAL");
        trial.Message.Should().Contain("0 finished").And.NotContain(" 0 still inside");
        report.Warnings.Should().Contain(w => w.Code == "NOTHING_FINISHED");
    }
}
