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
using Xunit.Abstractions;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-126 schema, slice 4 (<c>docs/plans/2026-11-25-sim-126-process-schema.md</c>, rule 4): a process
/// nothing feeds repeats its steps beside the flow, from the start of every run to its end; a
/// <c>wait_until</c> step waits for a stock's level, and a <c>put</c> may fill a stock <c>up_to</c> a
/// level. The acceptance test is the brief's car wash, written as a document exactly as the brief
/// writes it, against the C# example on all four of its figures.
/// </summary>
public class TopologySupplierTests
{
    private readonly ITestOutputHelper output;

    public TopologySupplierTests(ITestOutputHelper output) { this.output = output; }

    private static TopologyDefinition Read(string json) =>
        JsonSerializer.Deserialize<TopologyDefinition>(json, McpJsonUtilities.DefaultOptions)!;

    private static (ActiveModel? Model, ValidationReport Report) Build(string json)
    {
        var report = new ValidationReport();
        return (ModelRegistry.TryBuild(Read(json), report), report);
    }

    /// <summary>The car wash as the brief writes it (§ The car wash as a document), character for character.</summary>
    private const string Document = """
        {
          "schema_version": "1",
          "name": "car wash",
          "nodes": [
            {"id": "arrivals", "type": "source",   "params": {"arrival": {"type": "exponential", "mean": 3}}},
            {"id": "bays",     "type": "resource", "params": {"capacity": 2}},
            {"id": "soap",     "type": "stock",    "params": {"initial": 40, "capacity": 40}},
            {"id": "wash", "type": "process", "steps": [
              {"type": "seize", "resource": "bays", "patience": {"type": "uniform", "min": 5, "max": 20}, "outlet": "gave_up"},
              {"type": "take",  "stock": "soap", "amount": 1},
              {"id": "washing", "type": "delay", "duration": {"type": "uniform", "min": 4, "max": 6}}
            ]},
            {"id": "supplier", "type": "process", "steps": [
              {"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5},
              {"type": "delay", "duration": {"type": "uniform", "min": 30, "max": 60}},
              {"type": "put", "stock": "soap", "up_to": 40}
            ]},
            {"id": "washed", "type": "sink"},
            {"id": "left",   "type": "sink"}
          ],
          "connections": [
            ["arrivals", "wash"],
            ["wash", "washed"],
            {"from": "wash", "to": "left", "outlet": "gave_up"}
          ],
          "metrics": [
            {"id": "gave-up",      "kind": "share",       "node": "left"},
            {"id": "wait-for-bay", "kind": "wait_time",   "node": "bays"},
            {"id": "bays-busy",    "kind": "utilization", "node": "bays"},
            {"id": "soap-empty",   "kind": "empty_share", "node": "soap"}
          ]
        }
        """;

    [Fact]
    public void TheBriefsCarWash_BuildsAsWritten_WithTheSupplierRepeatingBesideTheFlow()
    {
        var (active, report) = Build(Document);

        report.Errors.Should().BeEmpty();
        active!.Processes["supplier"].Repeats.Should().BeTrue("nothing is connected to it");
        active.Processes["wash"].Repeats.Should().BeFalse();
        active.Processes["supplier"].Steps.Select(s => s.Id).Should().Equal("low", "delay#1", "put#1");
        ModelValidator.Validate(Read(Document)).Warnings.Should().NotContain(w => w.Node == "supplier",
            "a process nothing feeds is meant to be off the item flow");
    }

    [Fact]
    public void TheBriefsCarWash_AgreesWithTheCSharpExample_OnAllFourFigures()
    {
        // 20 days of 600 minutes from an empty wash with a full tank, as the example runs them. The two
        // models draw from different streams, so each figure is compared by the 99 % interval of the
        // difference of their means (Welch, with the 19 degrees of freedom of one side, the wider): four
        // figures at 95 % would fail one run in five by chance alone. The share who gave up is counted at
        // the decision in the example and at the sinks here, so cars still in a bay at closing differ; at
        // about 2 of 200 a day that is far inside the interval.
        ExperimentResult document = ExperimentRunner.Run("car wash", 1, Read(Document), duration: 600, replications: 20, warmup: 0, seed: 2026);
        ReplicationResult example = SimOpt.Examples.CarWash.CarWash.Run(new SimOpt.Examples.CarWash.CarWash.Settings(), replications: 20, baseSeed: 2026);
        ScenarioResult baseline = document.Scenarios.Single();
        baseline.Errors.Should().BeNull();

        foreach (var (metric, statistic) in new[]
                 {
                     ("gave-up", SimOpt.Examples.CarWash.CarWash.GaveUp), ("wait-for-bay", SimOpt.Examples.CarWash.CarWash.WaitForBay),
                     ("bays-busy", SimOpt.Examples.CarWash.CarWash.BayUtilization), ("soap-empty", SimOpt.Examples.CarWash.CarWash.SoapEmpty),
                 })
        {
            double[] a = baseline.Replicates[metric];
            double[] b = example[statistic].ReplicateEstimates.ToArray();
            double difference = a.Average() - b.Average();
            double se = Math.Sqrt(Variance(a) / a.Length + Variance(b) / b.Length);
            double halfWidth = 2.8609 * se;   // t(0.995, 19)
            output.WriteLine($"{metric,-13} document {a.Average():F4}  example {b.Average():F4}  difference {difference:+0.0000;-0.0000} ± {halfWidth:F4}");
            Math.Abs(difference).Should().BeLessThanOrEqualTo(halfWidth, $"{metric}: the document and the example are the same car wash");
        }
    }

    private static double Variance(double[] x)
    {
        double mean = x.Average();
        return x.Sum(v => (v - mean) * (v - mean)) / (x.Length - 1);
    }

    [Fact]
    public void AProcessNothingFeeds_ThatHasConnectionsLeavingIt_WarnsThatTheyCarryNothing()
    {
        ValidationReport report = ModelValidator.Validate(Read(Document
            .Replace("""{"id": "left",   "type": "sink"}""", """{"id": "left",   "type": "sink"}, {"id": "nowhere", "type": "sink"}""")
            .Replace("""["wash", "washed"],""", """["wash", "washed"], ["supplier", "nowhere"],""")));

        report.Errors.Should().BeEmpty();
        report.Warnings.Should().Contain(w => w.Code == "PROCESS_NOT_FED" && w.Node == "supplier");
    }

    [Fact]
    public void AProcessNothingFeeds_ThatWouldRepeatAtOneInstant_FailsItsTrialSayingWhy()
    {
        ValidationReport report = ModelValidator.Validate(Read(Document.Replace(
            """{"id": "left",   "type": "sink"}""",
            """{"id": "left",   "type": "sink"}, {"id": "spinner", "type": "process", "steps": [{"type": "seize", "resource": "bays"}, {"type": "release", "resource": "bays"}]}""")));

        report.Errors.Should().Contain(e => e.Code == "TRIAL_FAILED" && e.Message.Contains("one instant") && e.Message.Contains("spinner"));
    }

    [Theory]
    [InlineData("""{"id": "low", "type": "wait_until", "stock": "soap"}""", "MISSING_STEP_FIELD", "at_most")]
    [InlineData("""{"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5, "at_least": 1}""", "CONFLICTING_STEP_FIELDS", "at_least")]
    [InlineData("""{"id": "low", "type": "wait_until", "stock": "soap", "at_most": "low"}""", "INVALID_STEP_FIELD", "at_most")]
    [InlineData("""{"id": "low", "type": "wait_until", "stock": "bays", "at_most": 5}""", "UNKNOWN_STOCK", "'soap'")]
    public void AFaultyWaitUntil_IsRefused(string step, string code, string named)
    {
        var (active, report) = Build(Document.Replace("""{"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5}""", step));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle(e => e.Code == code).Which.Line.Should().Contain(named);
    }

    [Theory]
    [InlineData("""{"type": "put", "stock": "soap"}""", "MISSING_STEP_FIELD", "up_to")]
    [InlineData("""{"type": "put", "stock": "soap", "up_to": 40, "amount": 1}""", "CONFLICTING_STEP_FIELDS", "amount")]
    [InlineData("""{"type": "put", "stock": "soap", "up_to": 41}""", "INVALID_STEP_FIELD", "capacity")]
    [InlineData("""{"type": "put", "stock": "soap", "up_to": 0}""", "INVALID_STEP_FIELD", "up_to")]
    [InlineData("""{"type": "take", "stock": "soap", "up_to": 10}""", "UNKNOWN_STEP_FIELD", "up_to")]
    public void AFaultyFillUp_IsRefused(string step, string code, string named)
    {
        var (active, report) = Build(Document.Replace("""{"type": "put", "stock": "soap", "up_to": 40}""", step));

        active.Should().BeNull();
        report.Errors.Should().ContainSingle(e => e.Code == code).Which.Line.Should().Contain(named);
    }

    [Fact]
    public void AProcessNothingFeeds_MayGiveUp_WithoutConnectingItsOutlet()
    {
        // a breakdown crew that waits at most 10 for the bays and otherwise tries again later
        var (active, report) = Build(Document.Replace(
            """{"id": "low", "type": "wait_until", "stock": "soap", "at_most": 5},""",
            """{"type": "seize", "resource": "bays", "patience": {"type": "constant", "value": 10}, "outlet": "later"},"""));

        report.Errors.Should().BeEmpty();
        active!.Processes["supplier"].Outlets.Should().Equal("later");
    }
}
