using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Optimization;
using SimOpt.McpServer.Simulation;
using SimOpt.Simulation.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-126 schema, slice 5 (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, § Validation,
/// pre-check and patching): a resource's offered load before any run (UN-008), from the traffic
/// equations and the mean time a unit is held between its seize and its release; a warning when a
/// stock's reorder level is below what is used during one delivery; patch paths into steps
/// (<c>&lt;node&gt;.steps.&lt;step id&gt;.&lt;field&gt;[.&lt;distribution field&gt;]</c>), so <c>optimize</c> can
/// search bays and a reorder level together (UN-015).
/// </summary>
public class TopologyProcessLoadTests
{
    private readonly ITestOutputHelper output;

    public TopologyProcessLoadTests(ITestOutputHelper output) { this.output = output; }

    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    /// <summary>The brief's car wash (as in <see cref="TopologySupplierTests"/>); the reorder level goes where {LOW} is.</summary>
    private const string CarWash = """
        {"schema_version": "1", "name": "car wash", "seed": 7,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 3}}},
                   {"id": "bays", "type": "resource", "params": {"capacity": 2}},
                   {"id": "soap", "type": "stock", "params": {"initial": 40, "capacity": 40}},
                   {"id": "wash", "type": "process", "steps": [
                       {"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"},
                       {"type": "take", "stock": "soap", "amount": 1},
                       {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 6}}]},
                   {"id": "supplier", "type": "process", "steps": [
                       {"id": "low", "type": "wait_until", "stock": "soap", "at_most": {LOW}},
                       {"type": "delay", "duration": {"type": "uniform", "min": 30, "max": 60}},
                       {"type": "put", "stock": "soap", "up_to": 40}]},
                   {"id": "washed", "type": "sink"}, {"id": "left", "type": "sink"}],
         "connections": [["arrivals", "wash"], ["wash", "washed"], {"from": "wash", "to": "left", "outlet": "gave_up"}],
         "metrics": [{"id": "gave-up", "kind": "share", "node": "left"}, {"id": "soap-empty", "kind": "empty_share", "node": "soap"}]}
        """;

    private static string WithReorderLevel(int level) => CarWash.Replace("{LOW}", level.ToString());

    /// <summary>One source feeding one process; the steps go where {STEPS} is, and resources where {RESOURCES} is.</summary>
    private static string Line(double meanGap, string resources, string steps) => """
        {"name": "line", "seed": 3,
         "nodes": [{"id": "in", "type": "source", "params": {"arrival": {"type": "exponential", "mean": {GAP}}}},
                   {RESOURCES},
                   {"id": "work", "type": "process", "steps": {STEPS}},
                   {"id": "out", "type": "sink"}],
         "connections": [["in", "work"], ["work", "out"]]}
        """.Replace("{GAP}", meanGap.ToString(System.Globalization.CultureInfo.InvariantCulture))
           .Replace("{RESOURCES}", resources).Replace("{STEPS}", steps);

    private static ValidationIssue Load(ValidationReport report, string resource) =>
        report.Info.Single(i => i.Code == "OFFERED_LOAD" && i.Node == resource);

    [Fact]
    public void AResourcesOfferedLoad_IsTheArrivalRateTimesTheHoldOverItsUnits_ALowerBoundWhenItHoldsThroughAWait()
    {
        ValidationReport report = ModelValidator.Validate(Read(WithReorderLevel(5)));

        ValidationIssue bays = Load(report, "bays");
        bays.Value.Should().BeApproximately(1d / 3 * 5 / 2, 1e-9, "a car every 3 minutes holds a bay for a 5-minute wash, of 2 bays");
        bays.Message.Should().Contain("at least", "the wait for soap inside the bay counts as nothing");
    }

    [Fact]
    public void AUnitIsHeldOnlyUntilItsRelease()
    {
        ValidationReport report = ModelValidator.Validate(Read(Line(4, """{"id": "desk", "type": "resource"}""", """
            [{"type": "seize", "resource": "desk"}, {"type": "delay", "duration": {"type": "constant", "value": 2}},
             {"type": "release", "resource": "desk"}, {"type": "delay", "duration": {"type": "constant", "value": 10}}]
            """)));

        Load(report, "desk").Value.Should().BeApproximately(0.5, 1e-9, "a quarter an hour, two hours each; the ten after the release do not count");
        Load(report, "desk").Message.Should().NotContain("at least");
    }

    [Fact]
    public void AnOverloadedResource_WithoutPatience_GrowsItsQueue_AndTheFixNamesEnoughUnits()
    {
        ValidationReport report = ModelValidator.Validate(Read(Line(1, """{"id": "staff", "type": "resource"}""", """
            [{"type": "seize", "resource": "staff"}, {"type": "delay", "duration": {"type": "exponential", "mean": 2}}]
            """)));

        ValidationIssue saturated = report.Warnings.Single(w => w.Code == "SATURATED" && w.Node == "staff");
        saturated.Message.Should().Contain("grows without bound");
        saturated.Suggestion.Should().Contain("3 units");
        saturated.Value.Should().BeApproximately(2, 1e-9);
    }

    [Fact]
    public void AnOverloadedResource_WhoseItemsHavePatience_LosesAShareOfThem_RatherThanGrowingItsQueue()
    {
        ValidationReport report = ModelValidator.Validate(Read(WithReorderLevel(5)
            .Replace("""{"type": "uniform", "min": 4, "max": 6}""", """{"type": "uniform", "min": 6, "max": 8}""")));

        ValidationIssue saturated = report.Warnings.Single(w => w.Code == "SATURATED" && w.Node == "bays");
        saturated.Message.Should().Contain("give up").And.Contain("'wash'").And.NotContain("without bound");
    }

    private const string Breakdowns = """
        {"id": "out", "type": "sink"},
        {"id": "breakdowns", "type": "process", "steps": [
            {"type": "delay", "duration": {"type": "constant", "value": 90}},
            {"type": "seize", "resource": "crew"},
            {"type": "delay", "duration": {"type": "constant", "value": 10}}]}
        """;

    [Fact]
    public void AProcessNothingFeeds_HoldsAUnitForItsShareOfEachCycle_AndSharedUnitsAddUp()
    {
        // a breakdown every 90 that takes the crew 10 to repair, beside jobs that take it 1 at 0.4 an hour
        ValidationReport report = ModelValidator.Validate(Read(Line(2.5, """{"id": "crew", "type": "resource"}""", """
            [{"type": "seize", "resource": "crew"}, {"type": "delay", "duration": {"type": "constant", "value": 1}}]
            """).Replace("""{"id": "out", "type": "sink"}""", Breakdowns)));

        Load(report, "crew").Value.Should().BeApproximately(0.4 * 1 + 10d / 100, 1e-9);
    }

    [Fact]
    public void AReorderLevelBelowWhatIsUsedDuringOneDelivery_IsWarned_AndTheMeasurementBearsItOut()
    {
        ValidationReport low = ModelValidator.Validate(Read(WithReorderLevel(5)));
        ValidationReport enough = ModelValidator.Validate(Read(WithReorderLevel(20)));
        double EmptyShare(int level)
        {
            var registry = new ModelRegistry();
            ActiveModel active = registry.Get(registry.Create(Read(WithReorderLevel(level))));
            return new ReplicationRunner(active.Model, runLength: 6_000d, warmupTime: 600d).Run(10, 7)[active.Metrics["soap-empty"]].Mean;
        }
        double emptyAt5 = EmptyShare(5), emptyAt20 = EmptyShare(20);
        output.WriteLine($"soap empty: reorder level 5 -> {emptyAt5:F4}, 20 -> {emptyAt20:F4}");

        ValidationIssue warning = low.Warnings.Single(w => w.Code == "STOCK_RUNS_OUT");
        warning.Node.Should().Be("soap");
        warning.Message.Should().Contain("'supplier'").And.Contain("5");
        warning.Value.Should().BeApproximately(1d / 3 * 45, 1e-9, "a third of a car a minute, one unit each, over a 45-minute delivery");
        warning.Suggestion.Should().Contain("supplier.steps.low.at_most");
        enough.Warnings.Should().NotContain(w => w.Code == "STOCK_RUNS_OUT");
        emptyAt5.Should().BeGreaterThan(0.1, "warned: the tank stands empty for a good part of the day");
        emptyAt20.Should().BeLessThan(emptyAt5 / 5, "not warned: it rarely runs dry");
    }

    // ── patch paths into steps ──────────────────────────────────────────────────

    private static (TopologyDefinition? Patched, ValidationReport Report, List<string> Changes) Patch(string path, string value)
    {
        var report = new ValidationReport();
        var changes = new List<string>();
        TopologyDefinition? patched = TopologyPatcher.Apply(Read(WithReorderLevel(5)),
            new[] { new PatchOperation { Op = "set", Path = path, Value = JsonDocument.Parse(value).RootElement } }, report, changes);
        return (patched, report, changes);
    }

    private static StepDefinition Step(TopologyDefinition t, string node, int index) => t.Nodes.Single(n => n.Id == node).Steps![index];

    [Fact]
    public void AStepFieldIsAPatchPath_ByTheStepsIdOrItsTypeAndPosition()
    {
        var (byId, report1, changes) = Patch("supplier.steps.low.at_most", "20");
        var (byPosition, report2, _) = Patch("supplier.steps.put#1.up_to", "35");
        var (distributionField, report3, _) = Patch("wash.steps.washing.duration.max", "7");

        new[] { report1, report2, report3 }.Should().OnlyContain(r => r.Errors.Count == 0);
        Step(byId!, "supplier", 0).Fields["at_most"].Number.Should().Be(20);
        changes.Should().ContainSingle().Which.Should().Contain("supplier.steps.low.at_most").And.Contain("20");
        Step(byPosition!, "supplier", 2).Fields["up_to"].Number.Should().Be(35);
        Step(distributionField!, "wash", 2).Fields["duration"].Distribution!.Max.Should().Be(7);
        ModelRegistry.TryBuild(byId!, new ValidationReport()).Should().NotBeNull();
    }

    [Fact]
    public void APathToAStepThatIsNotThere_IsRefused_NamingTheSteps()
    {
        var (patched, report, _) = Patch("supplier.steps.reorder.at_most", "20");

        patched.Should().BeNull();
        report.Errors.Should().ContainSingle().Which.Line.Should().Contain("'low'").And.Contain("'delay#1'").And.Contain("'put#1'");
    }

    [Fact]
    public void Optimize_SearchesBaysAndTheReorderLevelTogether()
    {
        OptimizationResult result = Optimizer.Run("car wash", 1, Read(WithReorderLevel(5)),
            new[]
            {
                new DecisionVariable { Path = "bays.capacity", Type = "integer", Min = 2, Max = 3 },
                new DecisionVariable { Path = "supplier.steps.low.at_most", Type = "integer", Min = 5, Max = 25, Step = 10 },
            },
            new ObjectiveDefinition { Metric = "gave-up", Direction = "min" },
            duration: 600, warmup: 0, replicationsPerEval: 10, strategy: "grid", seed: 2026);

        output.WriteLine(result.Summary);
        result.Space.Should().Be(6);
        result.Evaluations.Should().Be(6);
        result.Best.Point["bays.capacity"].Should().Be(3);
        double baseline = ExperimentRunner.Run("car wash", 1, Read(WithReorderLevel(5)), duration: 600, replications: 10, warmup: 0, seed: 2026)
            .Scenarios.Single().Metrics!["gave-up"].Mean!.Value;
        output.WriteLine($"baseline (2 bays, reorder at 5): {baseline:F4}; best: {result.Best.Objective:F4}");
        result.Best.Objective.Should().BeLessThan(baseline / 2, "a third bay and an earlier reorder keep most drivers");
    }
}
