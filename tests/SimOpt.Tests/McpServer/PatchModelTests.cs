using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-66 step 5 (D8, D9): a model keeps its versions, and <c>patch_model</c> changes it by saying
/// what changed (UN-003: "incremental changes preserve the rest of the model", "earlier versions remain
/// addressable"). A patch is a list of operations applied to a copy of the latest version, all or
/// none: if any operation is faulty, or the result does not validate, nothing changes and every fault
/// is reported at once. A clean result becomes the next version under the same model id.
/// </summary>
public class PatchModelTests
{
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

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static AIFunction Tool(SimulationTools tools, string name) => AIFunctionFactory.Create(
        typeof(SimulationTools).GetMethod(name)!, tools,
        new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });

    private static async Task<(ModelRegistry Registry, SimulationTools Tools, string Id)> Created()
    {
        var registry = new ModelRegistry();
        var tools = new SimulationTools(registry);
        object? created = await Tool(tools, nameof(SimulationTools.CreateModel))
            .InvokeAsync(new AIFunctionArguments { ["topology"] = Json(Clinic) });
        return (registry, tools, Json(created!.ToString()!).GetProperty("model_id").GetString()!);
    }

    private static async Task<JsonElement> Patch(SimulationTools tools, string id, string patch)
    {
        object? result = await Tool(tools, nameof(SimulationTools.PatchModel))
            .InvokeAsync(new AIFunctionArguments { ["model_id"] = id, ["patch"] = Json(patch) });
        return Json(result!.ToString()!);
    }

    private static NodeDefinition NodeOf(ModelRegistry registry, string id, string node, int? version = null) =>
        registry.Topology(id, version).Nodes.Single(n => n.Id == node);

    private static string[] Codes(JsonElement result, string list) =>
        result.GetProperty("validation").GetProperty(list).EnumerateArray().Select(e => e.GetProperty("code").GetString()!).ToArray();

    [Fact]
    public async Task SettingOneFieldOfADistribution_MakesVersion2_AndKeepsVersion1()
    {
        var (registry, tools, id) = await Created();

        JsonElement patched = await Patch(tools, id, """[{"op": "set", "path": "arrivals.arrival.mean", "value": 2.5}]""");

        patched.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        patched.GetProperty("version").GetInt32().Should().Be(2);
        NodeOf(registry, id, "arrivals").Params["arrival"].Distribution!.Mean.Should().Be(2.5);
        NodeOf(registry, id, "arrivals", version: 1).Params["arrival"].Distribution!.Mean.Should().Be(5.0);
        Codes(patched, "warnings").Should().Contain("SATURATED", "flu season doubles arrivals: exam load 1.5");
        registry.Get(id).Topology.Should().BeSameAs(registry.Topology(id), "the live model is the latest version");
    }

    [Fact]
    public async Task SettingAWholeParameter_AName_AndASeed()
    {
        var (registry, tools, id) = await Created();

        JsonElement patched = await Patch(tools, id, """
            [{"op": "set", "path": "exam.servers", "value": 10},
             {"op": "set", "path": "exam.service", "value": {"type": "constant", "value": 25}},
             {"op": "set", "path": "name", "value": "clinic, ten rooms"},
             {"op": "set", "path": "seed", "value": 11}]
            """);

        patched.GetProperty("version").GetInt32().Should().Be(2);
        NodeOf(registry, id, "exam").Params["servers"].Number.Should().Be(10);
        NodeOf(registry, id, "exam").Params["service"].Distribution!.Type.Should().Be("constant");
        registry.Topology(id).Name.Should().Be("clinic, ten rooms");
        registry.Topology(id).Seed.Should().Be(11);
        patched.GetProperty("changes").GetArrayLength().Should().Be(4);
    }

    [Fact]
    public async Task ANullValue_RemovesAParameter()
    {
        var (registry, tools, id) = await Created();

        await Patch(tools, id, """[{"op": "set", "path": "triage_q.capacity", "value": 20}]""");
        JsonElement patched = await Patch(tools, id, """[{"op": "set", "path": "triage_q.capacity", "value": null}]""");

        patched.GetProperty("version").GetInt32().Should().Be(3);
        NodeOf(registry, id, "triage_q").Params.Should().NotContainKey("capacity");
        NodeOf(registry, id, "triage_q", version: 2).Params["capacity"].Number.Should().Be(20);
    }

    [Fact]
    public async Task ADelayInserted_BetweenExamAndExit_InOnePatch()
    {
        var (registry, tools, id) = await Created();

        JsonElement patched = await Patch(tools, id, """
            [{"op": "remove_connection", "from": "exam", "to": "exit"},
             {"op": "add_node", "value": {"id": "checkout", "type": "delay", "params": {"duration": {"type": "constant", "value": 2}}}},
             {"op": "set_connection", "from": "exam", "to": "checkout"},
             {"op": "set_connection", "from": "checkout", "to": "exit"}]
            """);

        patched.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        registry.Get(id).Delays.Should().ContainKey("checkout");
        registry.Topology(id).Connections.Select(c => (c.From, c.To)).Should().Contain(("exam", "checkout")).And.NotContain(("exam", "exit"));
    }

    [Fact]
    public async Task RemovingANode_RemovesItsConnectionsAndMetrics_AndSaysSo()
    {
        var (registry, tools, id) = await Created();

        JsonElement patched = await Patch(tools, id, """
            [{"op": "remove_node", "node": "exam_q"},
             {"op": "remove_node", "node": "exam"},
             {"op": "set_connection", "from": "triage", "to": "exit"}]
            """);

        patched.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        registry.Topology(id).Metrics.Select(m => m.Id).Should().Equal("los");
        string changes = string.Join("\n", patched.GetProperty("changes").EnumerateArray().Select(e => e.GetString()));
        changes.Should().Contain("wait_exam").And.Contain("util_exam").And.Contain("'exam_q' → 'exam'");
    }

    [Fact]
    public async Task MetricsCanBeSetAndRemoved()
    {
        var (registry, tools, id) = await Created();

        await Patch(tools, id, """
            [{"op": "set_metric", "value": {"id": "util_triage", "kind": "utilization", "node": "triage"}},
             {"op": "set_metric", "value": {"id": "los", "kind": "cycle_time", "node": "exit"}},
             {"op": "remove_metric", "id": "wait_exam"}]
            """);

        registry.Topology(id).Metrics.Select(m => (m.Id, m.Node)).Should()
                .BeEquivalentTo(new[] { ("util_exam", "exam"), ("los", "exit"), ("util_triage", "triage") });
        registry.Get(id).Metrics.Keys.Should().BeEquivalentTo("util_exam", "los", "util_triage");
    }

    [Fact]
    public async Task FaultyOperations_AreAllReported_AndNothingChanges()
    {
        var (registry, tools, id) = await Created();
        TopologyDefinition before = registry.Topology(id);

        JsonElement patched = await Patch(tools, id, """
            [{"op": "rename", "node": "exam"},
             {"op": "set", "path": "ward.servers", "value": 2},
             {"op": "set", "path": "arrivals.arrival.speed", "value": 2},
             {"op": "add_node", "value": {"id": "exam", "type": "station"}},
             {"op": "remove_connection", "from": "exit", "to": "exam"}]
            """);

        patched.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
        patched.GetProperty("version").GetInt32().Should().Be(1);
        Codes(patched, "errors").Should().Equal("PATCH_UNKNOWN_OP", "PATCH_BAD_PATH", "PATCH_BAD_PATH", "PATCH_NODE_EXISTS",
                                                "PATCH_NO_SUCH_CONNECTION");
        patched.GetProperty("validation").GetProperty("errors")[2].GetProperty("message").GetString()
               .Should().StartWith("Operation 3 (set)").And.Contain("speed");
        registry.Topology(id).Should().BeSameAs(before);
    }

    [Fact]
    public async Task APatchThatBreaksTheModel_IsRefused_WithTheModelsErrors()
    {
        var (registry, tools, id) = await Created();

        JsonElement patched = await Patch(tools, id, """[{"op": "remove_connection", "from": "exam_q", "to": "exam"}, {"op": "set", "path": "exam.servers", "value": 0}]""");

        patched.GetProperty("version").GetInt32().Should().Be(1);
        Codes(patched, "errors").Should().Contain("INVALID_PARAMETER");
        Codes(patched, "warnings").Should().Contain("QUEUE_NOT_TAKEN");
        registry.Versions(id).Should().Be(1);
    }

    [Fact]
    public async Task AnUnknownModel_IsAnError()
    {
        var (_, tools, _) = await Created();

        JsonElement patched = await Patch(tools, "nope", """[{"op": "set", "path": "seed", "value": 1}]""");

        patched.GetProperty("error").GetString().Should().Contain("nope");
    }

    [Fact]
    public async Task VersionsAreKept_AndRunSimulationRunsTheLatest()
    {
        var (registry, tools, id) = await Created();

        await Patch(tools, id, """[{"op": "set", "path": "exam.servers", "value": 9}]""");
        await Patch(tools, id, """[{"op": "set", "path": "exam.servers", "value": 10}]""");

        registry.Versions(id).Should().Be(3);
        Enumerable.Range(1, 3).Select(v => NodeOf(registry, id, "exam", v).Params["servers"].Number).Should().Equal(8, 9, 10);
        registry.Get(id).Servers.Keys.Should().Contain("exam#10");
        Action tooFar = () => registry.Topology(id, 4);
        tooFar.Should().Throw<InvalidOperationException>().WithMessage("*version 4*3*");
    }
}
