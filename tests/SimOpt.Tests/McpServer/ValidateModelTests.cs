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
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-66 step 4 (D6): <c>validate_model</c> runs every layer on a topology without registering it:
/// the builder's checks, the structural lint, the offered load, and a short trial replication on a
/// copy of its own. The trial (500 mean interarrival times of the busiest source, at most 1 s of wall
/// clock) reports what it saw, and warns when no item finished or a node refused items. A full queue's
/// refusals used to vanish without a trace; the trial counts them.
/// </summary>
public class ValidateModelTests
{
    private static DistributionSpec Exponential(double mean) => new DistributionSpec { Type = "exponential", Mean = mean };
    private static DistributionSpec Constant(double value) => new DistributionSpec { Type = "constant", Value = value };

    private static NodeDefinition Node(string id, string type, params (string Key, ParamValue Value)[] parameters)
    {
        var node = new NodeDefinition { Id = id, Type = type };
        foreach (var (key, value) in parameters) node.Params[key] = value;
        return node;
    }

    private static TopologyDefinition Line(DistributionSpec arrival, DistributionSpec service, NodeDefinition? queue = null) => new TopologyDefinition
    {
        Name = "line",
        Seed = 7,
        Nodes =
        {
            Node("in", "source", ("arrival", arrival)),
            queue ?? Node("q", "queue"),
            Node("s", "station", ("service", service)),
            Node("out", "sink"),
        },
        Connections =
        {
            new ConnectionDefinition { From = "in", To = "q" },
            new ConnectionDefinition { From = "q", To = "s" },
            new ConnectionDefinition { From = "s", To = "out" },
        },
    };

    private static TopologyDefinition Clinic() => new TopologyDefinition
    {
        SchemaVersion = "1",
        Name = "clinic",
        Seed = 7,
        Nodes =
        {
            Node("arrivals", "source", ("arrival", Exponential(5))),
            Node("triage_q", "queue"),
            Node("triage", "station", ("servers", 3), ("service", new DistributionSpec { Type = "triangular", Min = 3, Mode = 5, Max = 10 })),
            Node("exam_q", "queue"),
            Node("exam", "station", ("servers", 8), ("service", new DistributionSpec { Type = "triangular", Min = 20, Mode = 30, Max = 40 })),
            Node("exit", "sink"),
        },
        Connections =
        {
            new ConnectionDefinition { From = "arrivals", To = "triage_q" },
            new ConnectionDefinition { From = "triage_q", To = "triage" },
            new ConnectionDefinition { From = "triage", To = "exam_q" },
            new ConnectionDefinition { From = "exam_q", To = "exam" },
            new ConnectionDefinition { From = "exam", To = "exit" },
        },
    };

    private static ValidationReport Report(JsonElement json) =>
        json.Deserialize<ValidationReport>(new JsonSerializerOptions())!;

    private static ValidationIssue Trial(ValidationReport report) => report.Info.Single(i => i.Code == "TRIAL");

    [Fact]
    public void TheClinic_Validates_WithItsLoadsAndATrialInWhichPatientsFinish()
    {
        ValidationReport report = ModelValidator.Validate(Clinic());

        report.Valid.Should().BeTrue();
        report.Warnings.Should().BeEmpty();
        report.Info.Single(i => i.Code == "OFFERED_LOAD" && i.Node == "exam").Value.Should().BeApproximately(0.75, 1e-9);
        ValidationIssue trial = Trial(report);
        trial.Value.Should().BeGreaterThan(400, "about 500 patients arrive in 2,500 minutes and most finish");
        trial.Message.Should().Contain("2500").And.Contain("finished");
    }

    [Fact]
    public void ATopologyWithErrors_IsNotRun()
    {
        TopologyDefinition faulty = Line(Exponential(1), Exponential(1), Node("q", "queue", ("discipline", "random")));

        ValidationReport report = ModelValidator.Validate(faulty);

        report.Valid.Should().BeFalse();
        report.Info.Should().NotContain(i => i.Code == "TRIAL");
    }

    [Fact]
    public void AFullQueue_RefusesItems_AndTheTrialCountsThem()
    {
        ValidationReport report = ModelValidator.Validate(
            Line(Exponential(1), Exponential(1.5), Node("q", "queue", ("capacity", 5))));

        report.Warnings.Select(w => w.Code).Should().BeEquivalentTo("SATURATED", "REFUSED");
        ValidationIssue refused = report.Warnings.Single(w => w.Code == "REFUSED");
        refused.Node.Should().Be("q");
        refused.Value.Should().BeGreaterThan(50, "a third of 500 arrivals find the queue full at load 1.5");
        refused.Suggestion.Should().Contain("capacity");
    }

    [Fact]
    public void ATrialInWhichNoItemFinishes_IsWarnedAbout()
    {
        ValidationReport report = ModelValidator.Validate(Line(Constant(1), Constant(1000)));

        report.Warnings.Select(w => w.Code).Should().Contain("NOTHING_FINISHED");
        Trial(report).Value.Should().Be(0);
    }

    [Fact]
    public void TheTrial_StopsAtItsWallClockBudget_AndSaysSo()
    {
        var report = new ValidationReport();

        TrialRun.Check(Line(Exponential(1), Exponential(0.5)), report, budget: TimeSpan.Zero);

        Trial(report).Message.Should().Contain("stopped");
    }

    [Fact]
    public void ValidateModel_RegistersNothing_AndReturnsTheReport()
    {
        var registry = new ModelRegistry();

        JsonElement json = JsonDocument.Parse(new SimulationTools(registry).ValidateModel(Clinic())).RootElement;

        registry.AllIds().Should().BeEmpty();
        json.GetProperty("valid").GetBoolean().Should().BeTrue();
        Report(json).Info.Select(i => i.Code).Should().Contain(new[] { "OFFERED_LOAD", "TRIAL" });
    }

    [Fact]
    public void CreateModel_ReportsTheTrialToo()
    {
        JsonElement created = JsonDocument.Parse(new SimulationTools(new ModelRegistry()).CreateModel(Clinic())).RootElement;

        Report(created.GetProperty("validation")).Info.Select(i => i.Code).Should().Contain("TRIAL");
    }

    [Fact]
    public void TheTrial_LeavesTheRegisteredModelUntouched()
    {
        var registry = new ModelRegistry();
        var tools = new SimulationTools(registry);
        string id = JsonDocument.Parse(tools.CreateModel(Clinic())).RootElement.GetProperty("model_id").GetString()!;

        registry.Get(id).Model.CurrentTime.Should().Be(0, "the trial ran on a copy of its own");
    }
}
