using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Simulation.Statistics;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 6 (D7): one catalogue (<see cref="SchemaCatalog"/>) is the source of truth for the
/// vocabulary. The builder, <c>get_schema</c> and <c>list_templates</c> all read it, and these tests
/// assert they agree, because a client LLM correcting itself against a schema that has drifted from
/// the parser is worse off than one with no schema at all. The acceptance test of SIM-65 is the clinic
/// walkthrough of the readiness review, built from its JSON exactly as written there (connections as
/// arrays, a <c>schema_version</c>) with one <c>create_model</c> call.
/// </summary>
public class SchemaCatalogTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>A chain through every node type the catalogue publishes.</summary>
    private const string EveryNodeType = """
        {"name": "every", "seed": 7,
         "nodes": [{"id": "arrivals", "type": "source", "params": {"arrival": {"type": "exponential", "mean": 2}}},
                   {"id": "q", "type": "queue", "params": {"capacity": 50, "discipline": "fifo"}},
                   {"id": "s", "type": "station", "params": {"servers": 2, "service": {"type": "constant", "value": 1}}},
                   {"id": "d", "type": "delay", "params": {"duration": {"type": "uniform", "min": 1, "max": 2}}},
                   {"id": "r", "type": "router", "params": {"policy": "probabilistic"}},
                   {"id": "a", "type": "sink"}, {"id": "b", "type": "sink"}],
         "connections": [["arrivals", "q"], ["q", "s"], ["s", "d"], ["d", "r"], ["r", "a", 3], {"from": "r", "to": "b", "weight": 1}]}
        """;

    private static async Task<JsonElement> CreateModel(SimulationTools tools, string topology)
    {
        AIFunction createModel = AIFunctionFactory.Create(
            typeof(SimulationTools).GetMethod(nameof(SimulationTools.CreateModel))!, tools,
            new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });
        object? result = await createModel.InvokeAsync(new AIFunctionArguments { ["topology"] = Json(topology) });
        return Json(result!.ToString()!);
    }

    [Fact]
    public async Task EveryNodeTypeTheCatalogPublishes_Builds_AndConnectionsBindAsArraysOrObjects()
    {
        var registry = new ModelRegistry();
        JsonElement created = await CreateModel(new SimulationTools(registry), EveryNodeType);

        created.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        ActiveModel active = registry.Get(created.GetProperty("model_id").GetString()!);
        active.Topology.Nodes.Select(n => n.Type).Distinct()
              .Should().BeEquivalentTo(SchemaCatalog.NodeTypes.Select(t => t.Name));
        active.Topology.Connections[4].Weight.Should().Be(3);
        active.Topology.Connections[5].Weight.Should().Be(1);
    }

    [Fact]
    public void EveryAlias_BuildsAsItsType_AndATypeTheCatalogDoesNotPublish_IsRefusedListingTheCatalog()
    {
        foreach (NodeTypeSpec type in SchemaCatalog.NodeTypes)
            foreach (string alias in type.Aliases)
                SchemaCatalog.Canonical(alias).Should().Be(type.Name);

        Action build = () => new ModelRegistry().Create(new TopologyDefinition
        {
            Nodes = { new NodeDefinition { Id = "x", Type = "conveyor" } },
        });

        build.Should().Throw<InvalidOperationException>()
             .Where(e => SchemaCatalog.NodeTypes.All(t => e.Message.Contains(t.Name)));
    }

    [Fact]
    public void TheCatalogsMetricKinds_AreTheOnesTheBuilderMeasures()
    {
        SchemaCatalog.MetricKinds.Select(k => k.Name).Should().BeEquivalentTo(
            Instrumentation.WaitTime, Instrumentation.QueueLength, Instrumentation.Utilization,
            Instrumentation.ItemCount, Instrumentation.Throughput, Instrumentation.CycleTime);
        SchemaCatalog.DistributionTypes.Should().Equal(DistributionFactory.KnownTypes);
    }

    [Fact]
    public void GetSchema_PublishesTheCatalogAsJsonSchema()
    {
        JsonElement schema = Json(new SimulationTools(new ModelRegistry()).GetSchema());

        schema.GetProperty("schema_version").GetString().Should().Be(SchemaCatalog.SchemaVersion);
        JsonElement json = schema.GetProperty("json_schema");
        json.GetProperty("$schema").GetString().Should().Contain("json-schema.org");
        JsonElement defs = json.GetProperty("$defs");
        defs.GetProperty("node").GetProperty("properties").GetProperty("type").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(SchemaCatalog.NodeTypes.SelectMany(t => t.Aliases.Prepend(t.Name)));
        defs.GetProperty("metric").GetProperty("properties").GetProperty("kind").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(SchemaCatalog.MetricKinds.Select(k => k.Name));
        defs.GetProperty("distribution").GetProperty("properties").GetProperty("type").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString())
            .Should().Equal(SchemaCatalog.DistributionTypes);
        schema.GetProperty("node_types").EnumerateArray().Select(t => t.GetProperty("name").GetString())
              .Should().Equal(SchemaCatalog.NodeTypes.Select(t => t.Name));
    }

    [Fact]
    public void GetSchema_StatesWhatASchemaV1ModelDoesNotRepresent()
    {
        // SIM-86 (UN-035): the tool says plainly what it did not model
        JsonElement schema = Json(new SimulationTools(new ModelRegistry()).GetSchema());

        string[] limits = schema.GetProperty("limits").EnumerateArray().Select(e => e.GetString()!).ToArray();

        limits.Should().Equal(SchemaCatalog.Limits);
        limits.Should().Contain(l => l.Contains("break down"))
              .And.Contain(l => l.Contains("delay node"))
              .And.Contain(l => l.Contains("warm-up"));
    }

    [Fact]
    public void ListTemplates_DescribesTheCatalogsTypes_ByTheirV1Names()
    {
        JsonElement listed = Json(new SimulationTools(new ModelRegistry()).ListTemplates());

        JsonElement[] templates = listed.GetProperty("templates").EnumerateArray().ToArray();
        templates.Select(t => t.GetProperty("type").GetString()).Should().Equal(SchemaCatalog.NodeTypes.Select(t => t.Name));
        foreach ((JsonElement template, NodeTypeSpec type) in templates.Zip(SchemaCatalog.NodeTypes))
            template.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
                    .Should().Equal(type.Parameters.Select(p => p.Name));
    }

    [Fact]
    public async Task ADocumentOfAnotherSchemaVersion_IsRefused()
    {
        JsonElement created = await CreateModel(new SimulationTools(new ModelRegistry()),
            EveryNodeType.Replace("{\"name\": \"every\"", "{\"schema_version\": \"2\", \"name\": \"every\""));

        created.GetProperty("error").GetString().Should().Contain("schema_version").And.Contain("\"2\"");
    }

    [Theory]
    [InlineData("""["a"]""")]
    [InlineData("""["a", "b", "heavy"]""")]
    [InlineData("""["a", "b", 1, 2]""")]
    [InlineData("""{"from": "a"}""")]
    public void AMalformedConnection_IsRefusedWhenItIsRead(string connection)
    {
        Action read = () => JsonSerializer.Deserialize<ConnectionDefinition>(connection, McpJsonUtilities.DefaultOptions);

        read.Should().Throw<JsonException>();
    }

    /// <summary>The clinic walkthrough, as written in the readiness review (§C).</summary>
    private const string Clinic = """
        {"schema_version":"1","name":"clinic","seed":7,
         "nodes":[
          {"id":"arrivals","type":"source","params":{"arrival":{"type":"exponential","mean":5.0}}},
          {"id":"triage_q","type":"queue"},
          {"id":"triage","type":"station","params":{"servers":3,"service":{"type":"triangular","min":3,"mode":5,"max":10}}},
          {"id":"exam_q","type":"queue"},
          {"id":"exam","type":"station","params":{"servers":8,"service":{"type":"triangular","min":20,"mode":30,"max":40}}},
          {"id":"exit","type":"sink"}],
         "connections":[["arrivals","triage_q"],["triage_q","triage"],["triage","exam_q"],["exam_q","exam"],["exam","exit"]],
         "metrics":[{"id":"wait_exam","kind":"wait_time","node":"exam_q"},
                    {"id":"util_exam","kind":"utilization","node":"exam"},
                    {"id":"los","kind":"cycle_time"}]}
        """;

    [Fact]
    public async Task TheClinicWalkthrough_BuildsFromOneCreateModelCall_AndMeasuresItsMetrics()
    {
        var registry = new ModelRegistry();
        JsonElement created = await CreateModel(new SimulationTools(registry), Clinic);
        created.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        ActiveModel active = registry.Get(created.GetProperty("model_id").GetString()!);

        // 10 replications of an 8-hour day (minutes) after a 1-hour warm-up, as in the walkthrough
        ReplicationResult day = new ReplicationRunner(active.Model, runLength: 540d, warmupTime: 60d).Run(10, 7);

        active.Metrics.Keys.Should().BeEquivalentTo("wait_exam", "util_exam", "los");
        day[active.Metrics["util_exam"]].Mean.Should().BeApproximately(0.75, 0.08,
            "12 patients an hour, 30-minute exams, 8 rooms: offered load 0.75");
        day[active.Metrics["los"]].Mean.Should().BeInRange(36, 60,
            "a stay is at least a 6-minute triage and a 30-minute exam, plus some waiting");
        day[active.Metrics["wait_exam"]].Mean.Should().BeLessThan(20, "at 0.75 on eight rooms the wait is short");
    }
}
