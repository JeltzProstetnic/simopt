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
/// SIM-66 step 1 (D1, D2, D7): the builder reports every fault of a topology at once instead of
/// throwing at the first, each as an issue with a code, the node, the message and the fix (UN-008:
/// "all problems are reported at once", "every problem reported carries a suggested correction").
/// <c>Create</c> still throws, now a <see cref="ModelValidationException"/> listing every error, so
/// every refusal test of SIM-65 keeps passing unchanged; <c>create_model</c> returns the report.
/// </summary>
public class ValidationReportTests
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
            Name = "faults",
            Seed = 7,
            Nodes = nodes.ToList(),
            Connections = connections.Select(c => new ConnectionDefinition { From = c.From, To = c.To }).ToList(),
        };

    private static TopologyDefinition Line(params NodeDefinition[] replacing)
    {
        var nodes = new List<NodeDefinition>
        {
            Node("arrivals", "source", ("arrival", Exponential(2))),
            Node("q", "queue"),
            Node("s", "station", ("service", Exponential(1))),
            Node("done", "sink"),
        };
        foreach (NodeDefinition node in replacing) nodes[nodes.FindIndex(n => n.Id == node.Id)] = node;
        return Topology(nodes, ("arrivals", "q"), ("q", "s"), ("s", "done"));
    }

    private static ValidationReport Refusal(TopologyDefinition topology)
    {
        Action build = () => new ModelRegistry().Create(topology);
        return build.Should().Throw<ModelValidationException>().Which.Report;
    }

    [Fact]
    public void IndependentFaults_AreAllReported_AtOnce_EachWithItsCodeAndNode()
    {
        TopologyDefinition topology = Line(
            Node("arrivals", "source", ("arrival", new DistributionSpec { Type = "triangular", Min = 5, Mode = 4, Max = 3 })),
            Node("q", "queue", ("discipline", "random")),
            Node("s", "station", ("servers", 2.5), ("service", Exponential(1))));
        topology.Connections.Add(new ConnectionDefinition { From = "done", To = "nowhere" });
        topology.Metrics.Add(new MetricDefinition { Id = "joy", Kind = "happiness", Node = "q" });

        ValidationReport report = Refusal(topology);

        report.Valid.Should().BeFalse();
        report.Errors.Select(e => (e.Code, e.Node)).Should().BeEquivalentTo(new[]
        {
            ("INVALID_DISTRIBUTION", "arrivals"),
            ("INVALID_PARAMETER", "q"),
            ("INVALID_PARAMETER", "s"),
            ("INVALID_CONNECTION", "done"),
            ("INVALID_METRIC", (string?)null),
        });
    }

    [Fact]
    public void TheExceptionMessage_ListsEveryError_OnALineOfItsOwn()
    {
        TopologyDefinition topology = Line(
            Node("q", "queue", ("discipline", "random")),
            Node("s", "station", ("servers", 0), ("service", Exponential(1))));

        Action build = () => new ModelRegistry().Create(topology);

        ModelValidationException refused = build.Should().Throw<ModelValidationException>().Which;
        refused.Should().BeAssignableTo<InvalidOperationException>("every SIM-65 refusal test expects one");
        refused.Message.Split('\n').Should().HaveCount(2);
        refused.Message.Should().Contain("discipline").And.Contain("servers");
    }

    [Fact]
    public void AConnectionToANodeThatFailed_AddsNoSecondError()
    {
        ValidationReport report = Refusal(Line(Node("s", "station", ("servers", -1), ("service", Exponential(1)))));

        report.Errors.Should().ContainSingle().Which.Node.Should().Be("s");
    }

    [Fact]
    public void AMetricAtANodeThatFailed_AddsNoSecondError()
    {
        TopologyDefinition topology = Line(Node("q", "queue", ("capacity", "lots")));
        topology.Metrics.Add(new MetricDefinition { Id = "wq", Kind = "wait_time", Node = "q" });

        Refusal(topology).Errors.Should().ContainSingle().Which.Code.Should().Be("INVALID_PARAMETER");
    }

    [Fact]
    public void ARefusal_CarriesItsFixAsASuggestion()
    {
        TopologyDefinition topology = Topology(new[]
        {
            Node("arrivals", "source", ("arrival", Exponential(2))),
            Node("s", "station", ("service", Exponential(1))),
            Node("done", "sink"),
        }, ("arrivals", "s"), ("s", "done"));

        ValidationIssue issue = Refusal(topology).Errors.Should().ContainSingle().Which;

        issue.Code.Should().Be("STATION_NEEDS_QUEUE");
        issue.Node.Should().Be("s");
        issue.Suggestion.Should().Contain("Insert a queue between 'arrivals' and 's'");
        issue.Message.Should().NotContain("Insert a queue", "the fix is the suggestion, not part of the diagnosis");
    }

    [Fact]
    public void DuplicateAndMissingNodeIds_AreRefused()
    {
        TopologyDefinition topology = Line();
        topology.Nodes.Add(Node("q", "queue"));
        topology.Nodes.Add(Node(" ", "sink"));

        Refusal(topology).Errors.Select(e => e.Code).Should().BeEquivalentTo("DUPLICATE_NODE_ID", "MISSING_NODE_ID");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(2.5)]
    public void AQueueCapacity_IsAWholeNumberOfAtLeastOne(double capacity)
    {
        ValidationIssue issue = Refusal(Line(Node("q", "queue", ("capacity", capacity)))).Errors.Should().ContainSingle().Which;

        issue.Code.Should().Be("INVALID_PARAMETER");
        issue.Message.Should().Contain("capacity");
    }

    [Fact]
    public void AnUnsupportedSchemaVersion_IsTheOnlyErrorReported()
    {
        TopologyDefinition topology = Line(Node("q", "queue", ("discipline", "random")));
        topology.SchemaVersion = "2";

        Refusal(topology).Errors.Should().ContainSingle().Which.Code.Should().Be("SCHEMA_VERSION");
    }

    [Fact]
    public void CreateModel_ReturnsEveryErrorAndTheReport_AndRegistersNothing()
    {
        var registry = new ModelRegistry();
        TopologyDefinition topology = Line(
            Node("q", "queue", ("discipline", "random")),
            Node("s", "station", ("servers", 0), ("service", Exponential(1))));

        JsonElement created = JsonDocument.Parse(new SimulationTools(registry).CreateModel(topology)).RootElement;

        created.GetProperty("error").GetString().Should().Contain("discipline").And.Contain("servers");
        JsonElement validation = created.GetProperty("validation");
        validation.GetProperty("valid").GetBoolean().Should().BeFalse();
        validation.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("code").GetString())
                  .Should().Equal("INVALID_PARAMETER", "INVALID_PARAMETER");
        registry.AllIds().Should().BeEmpty();
    }

    [Fact]
    public void CreateModel_OnAValidTopology_ReturnsTheModelIdAndAValidReport()
    {
        var registry = new ModelRegistry();

        JsonElement created = JsonDocument.Parse(new SimulationTools(registry).CreateModel(Line())).RootElement;

        created.TryGetProperty("error", out _).Should().BeFalse();
        registry.AllIds().Should().ContainSingle().Which.Should().Be(created.GetProperty("model_id").GetString());
        JsonElement validation = created.GetProperty("validation");
        validation.GetProperty("valid").GetBoolean().Should().BeTrue();
        validation.GetProperty("errors").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void AnIssue_SerializesWithoutItsEmptyFields()
    {
        string json = JsonSerializer.Serialize(new ValidationIssue("NO_SINK", "No sink."));

        json.Should().Be("""{"code":"NO_SINK","message":"No sink."}""");
    }
}
