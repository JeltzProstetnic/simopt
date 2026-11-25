using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-67 (UN-005): a model outlives the server. save_model writes a version's topology as SIM-65 JSON,
/// load_model registers such a file as create_model would, and export_model returns it inline.
/// </summary>
public sealed class ModelPersistenceTests : IDisposable
{
    private const string Clinic = """
        {"schema_version":"1","name":"clinic","seed":7,
         "nodes":[
          {"id":"arrivals","type":"source","params":{"arrival":{"type":"exponential","mean":5.0}}},
          {"id":"exam_q","type":"queue"},
          {"id":"exam","type":"station","params":{"servers":8,"service":{"type":"triangular","min":20,"mode":30,"max":40}}},
          {"id":"exit","type":"sink"}],
         "connections":[["arrivals","exam_q"],["exam_q","exam"],["exam","exit"]],
         "metrics":[{"id":"wait_exam","kind":"wait_time","node":"exam_q"},{"id":"los","kind":"cycle_time"}]}
        """;

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim67-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static async Task<string> Call(SimulationTools tools, string name, object arguments)
    {
        var function = AIFunctionFactory.Create(typeof(SimulationTools).GetMethod(name)!, tools,
            new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });
        var args = new AIFunctionArguments();
        foreach (JsonProperty p in JsonSerializer.SerializeToElement(arguments).EnumerateObject()) args[p.Name] = p.Value;
        return (await function.InvokeAsync(args))!.ToString()!;
    }

    private static async Task<(SimulationTools Tools, string Id)> Created()
    {
        var tools = new SimulationTools(new ModelRegistry());
        string id = Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic) }))
            .GetProperty("model_id").GetString()!;
        return (tools, id);
    }

    private static async Task<JsonElement> Scenarios(SimulationTools tools, string id) =>
        Json(await Call(tools, nameof(SimulationTools.RunExperiment), new { model_id = id, duration = 540.0, warmup = 60.0, replications = 5 }))
            .GetProperty("scenarios");

    [Fact]
    public async Task ASavedModel_ReloadedByAnotherServer_GivesTheSameResults()
    {
        var (tools, id) = await Created();
        await Call(tools, nameof(SimulationTools.PatchModel), new { model_id = id, patch = Json("""[{"op":"set","path":"exam.servers","value":7}]""") });
        string path = Path.Combine(folder, "clinic-7-rooms.json");

        JsonElement saved = Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path }));

        saved.GetProperty("version").GetInt32().Should().Be(2);
        saved.GetProperty("path").GetString().Should().Be(path);
        saved.GetProperty("bytes").GetInt64().Should().Be(new FileInfo(path).Length);

        var another = new SimulationTools(new ModelRegistry());
        JsonElement loaded = Json(await Call(another, nameof(SimulationTools.LoadModel), new { path }));
        string reloaded = loaded.GetProperty("model_id").GetString()!;
        loaded.GetProperty("name").GetString().Should().Be("clinic");

        (await Scenarios(another, reloaded)).GetRawText().Should().Be((await Scenarios(tools, id)).GetRawText());
    }

    [Fact]
    public async Task TheFile_IsTheSchemasJson_AndValidates()
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, "clinic.json");

        await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path });

        JsonElement file = Json(File.ReadAllText(path));
        file.GetProperty("schema_version").GetString().Should().Be("1");
        file.GetProperty("nodes").GetArrayLength().Should().Be(4);
        Json(await Call(tools, nameof(SimulationTools.ValidateModel), new { topology = file })).GetProperty("valid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Export_ReturnsAVersionsTopologyInline()
    {
        var (tools, id) = await Created();
        await Call(tools, nameof(SimulationTools.PatchModel), new { model_id = id, patch = Json("""[{"op":"set","path":"exam.servers","value":7}]""") });

        JsonElement first = Json(await Call(tools, nameof(SimulationTools.ExportModel), new { model_id = id, version = 1 }));
        JsonElement latest = Json(await Call(tools, nameof(SimulationTools.ExportModel), new { model_id = id }));

        first.GetProperty("version").GetInt32().Should().Be(1);
        Servers(first.GetProperty("topology")).Should().Be(8);
        latest.GetProperty("version").GetInt32().Should().Be(2);
        Servers(latest.GetProperty("topology")).Should().Be(7);
    }

    private static int Servers(JsonElement topology) =>
        topology.GetProperty("nodes").EnumerateArray().Single(n => n.GetProperty("id").GetString() == "exam")
                .GetProperty("params").GetProperty("servers").GetInt32();

    [Fact]
    public async Task Saving_DoesNotOverwriteByAccident_AndWantsAJsonPath()
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, "clinic.json");
        File.WriteAllText(path, "precious");

        Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path })).GetProperty("error").GetString()
            .Should().Contain("exists").And.Contain("overwrite");
        File.ReadAllText(path).Should().Be("precious");

        Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path, overwrite = true }))
            .GetProperty("version").GetInt32().Should().Be(1);
        File.ReadAllText(path).Should().Contain("\"schema_version\"");

        Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path = Path.Combine(folder, "clinic.txt") }))
            .GetProperty("error").GetString().Should().Contain(".json");
    }

    [Fact]
    public async Task AnUnknownModelOrVersion_IsRefused()
    {
        var (tools, id) = await Created();

        Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = "nope", path = Path.Combine(folder, "a.json") }))
            .GetProperty("error").GetString().Should().Contain("nope");
        Json(await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path = Path.Combine(folder, "b.json"), version = 5 }))
            .GetProperty("error").GetString().Should().Contain("no version 5");
        Json(await Call(tools, nameof(SimulationTools.ExportModel), new { model_id = "nope" }))
            .GetProperty("error").GetString().Should().Contain("nope");
        File.Exists(Path.Combine(folder, "b.json")).Should().BeFalse();
    }

    [Fact]
    public async Task Loading_AMissingOrFaultyFile_IsRefused_WithEveryFault()
    {
        var tools = new SimulationTools(new ModelRegistry());
        string missing = Path.Combine(folder, "missing.json");
        string faulty = Path.Combine(folder, "faulty.json");
        File.WriteAllText(faulty, """{"schema_version":"1","name":"broken","nodes":[{"id":"q","type":"queue"},{"id":"exit","type":"sink"}],"connections":[["q","exit"]]}""");
        string garbled = Path.Combine(folder, "garbled.json");
        File.WriteAllText(garbled, "{ not json");

        Json(await Call(tools, nameof(SimulationTools.LoadModel), new { path = missing })).GetProperty("error").GetString()
            .Should().Contain("missing.json");
        JsonElement refused = Json(await Call(tools, nameof(SimulationTools.LoadModel), new { path = faulty }));
        refused.GetProperty("validation").GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("code").GetString()).Should().Contain("NO_SOURCE");
        Json(await Call(tools, nameof(SimulationTools.LoadModel), new { path = garbled })).GetProperty("error").GetString()
            .Should().Contain("garbled.json");
        Json(await Call(tools, nameof(SimulationTools.ListModels), new { })).GetProperty("model_count").GetInt32().Should().Be(0);
    }
}
