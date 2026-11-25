using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-86 slice A (UN-033, UN-034, UN-035): a result names the model it ran by the SHA-256 of its schema
/// v1 document, the bytes save_model writes, so a third party holding the file can check it is the one
/// run; it lists every value the run used without being given it (call arguments by name, node
/// parameters by patch path), and each scenario carries the patch that made it.
/// </summary>
public sealed class ProvenanceTests : IDisposable
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

    /// <summary>Leaves out the queue's capacity and discipline and the station's servers, and has no metrics.</summary>
    private const string Line = """
        {"name":"line","seed":3,
         "nodes":[{"id":"in","type":"source","params":{"arrival":{"type":"exponential","mean":1}}},
                  {"id":"q","type":"queue"},
                  {"id":"s","type":"station","params":{"service":{"type":"exponential","mean":0.8}}},
                  {"id":"out","type":"sink"}],
         "connections":[["in","q"],["q","s"],["s","out"]]}
        """;

    /// <summary>Every parameter given, one of them by its older alternative name, and a metric declared.</summary>
    private const string Given = """
        {"name":"given","seed":3,
         "nodes":[{"id":"in","type":"source","params":{"mean_interval":1}},
                  {"id":"q","type":"queue","params":{"capacity":50,"discipline":"fifo"}},
                  {"id":"s","type":"station","params":{"servers":1,"service_time":0.8}},
                  {"id":"out","type":"sink"}],
         "connections":[["in","q"],["q","s"],["s","out"]],
         "metrics":[{"id":"wait","kind":"wait_time","node":"q"}]}
        """;

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim86-").FullName;

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

    private static async Task<(SimulationTools Tools, string Id)> Created(string topology, SimulationTools? tools = null)
    {
        tools ??= new SimulationTools(new ModelRegistry());
        string answer = await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(topology) });
        return (tools, Json(answer).GetProperty("model_id").GetString()!);
    }

    private static async Task<JsonElement> Experiment(SimulationTools tools, string id, object? extra = null)
    {
        var arguments = new Dictionary<string, object?> { ["model_id"] = id, ["duration"] = 200.0 };
        if (extra != null)
            foreach (JsonProperty p in JsonSerializer.SerializeToElement(extra).EnumerateObject()) arguments[p.Name] = p.Value;
        return Json(await Call(tools, nameof(SimulationTools.RunExperiment), arguments));
    }

    private static Dictionary<string, string> Assumed(JsonElement result) =>
        result.TryGetProperty("assumed", out JsonElement assumed)
            ? assumed.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!)
            : new Dictionary<string, string>();

    private static string Sha256OfFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    [Fact]
    public async Task TheResult_NamesTheModelByTheSha256OfTheFileSaveModelWrites()
    {
        var (tools, id) = await Created(Clinic);
        string path = Path.Combine(folder, "clinic.json");
        await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path });

        string hash = (await Experiment(tools, id)).GetProperty("model_sha256").GetString()!;

        hash.Should().MatchRegex("^[0-9a-f]{64}$");
        hash.Should().Be(Sha256OfFile(path));
    }

    [Fact]
    public async Task TheSavedFile_UsesLineFeedsOnly_SoItsHashIsTheSameOnEveryOperatingSystem()
    {
        var (tools, id) = await Created(Clinic);
        string path = Path.Combine(folder, "clinic.json");

        await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path });

        File.ReadAllText(path).Should().Contain("\n").And.NotContain("\r");
    }

    [Fact]
    public async Task AChangedParameter_ChangesTheHash_AndTheSameDocumentOnAnotherServerGivesTheSameHashAndFigures()
    {
        var (tools, id) = await Created(Clinic);
        await Call(tools, nameof(SimulationTools.PatchModel), new { model_id = id, patch = Json("""[{"op":"set","path":"exam.servers","value":7}]""") });
        string path = Path.Combine(folder, "clinic-7.json");
        await Call(tools, nameof(SimulationTools.SaveModel), new { model_id = id, path });
        JsonElement eight = await Experiment(tools, id, new { version = 1 });
        JsonElement seven = await Experiment(tools, id, new { version = 2 });

        var elsewhere = new SimulationTools(new ModelRegistry());
        string loaded = Json(await Call(elsewhere, nameof(SimulationTools.LoadModel), new { path })).GetProperty("model_id").GetString()!;
        JsonElement again = await Experiment(elsewhere, loaded);

        seven.GetProperty("model_sha256").GetString().Should().NotBe(eight.GetProperty("model_sha256").GetString());
        again.GetProperty("model_sha256").GetString().Should().Be(seven.GetProperty("model_sha256").GetString());
        again.GetProperty("scenarios").GetRawText().Should().Be(seven.GetProperty("scenarios").GetRawText());
    }

    [Fact]
    public async Task EveryValueTheRunWasNotGiven_IsListedWithTheValueItUsed()
    {
        var (tools, id) = await Created(Line);

        Dictionary<string, string> assumed = Assumed(await Experiment(tools, id));

        assumed.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["warmup"] = "a tenth of the duration",
            ["seed"] = "the model's",
            ["replications"] = "10",
            ["metrics"] = "the default set; the model declares none",
            ["q.capacity"] = "unlimited",
            ["q.discipline"] = "fifo",
            ["s.servers"] = "1",
        });
    }

    [Fact]
    public async Task AValueGiven_ByItsNameOrItsOlderAlternative_IsNotListed()
    {
        var (tools, id) = await Created(Given);

        JsonElement result = await Experiment(tools, id, new { warmup = 20.0, seed = 5, replications = 4 });

        result.TryGetProperty("assumed", out _).Should().BeFalse("everything was given: {0}", result.GetRawText());
    }

    [Fact]
    public async Task ASourceWithoutAnArrival_IsListedWithTheCataloguesDefault()
    {
        const string bare = """
            {"name":"bare","nodes":[{"id":"in","type":"source"},{"id":"out","type":"sink"}],"connections":[["in","out"]],
             "metrics":[{"id":"n","kind":"count","node":"out"}]}
            """;
        var (tools, id) = await Created(bare);

        Dictionary<string, string> assumed = Assumed(await Experiment(tools, id, new { warmup = 0.0, seed = 1, replications = 2 }));

        assumed.Should().BeEquivalentTo(new Dictionary<string, string> { ["in.arrival"] = "exponential, mean 1" });
    }

    [Fact]
    public async Task EachScenario_CarriesThePatchThatMadeIt_AndTheBaselineNone()
    {
        var (tools, id) = await Created(Clinic);
        JsonElement patch = Json("""[{"op":"set","path":"arrivals.arrival.mean","value":4}]""");

        JsonElement result = await Experiment(tools, id, new { scenarios = new[] { new { name = "flu", patch } } });

        JsonElement[] scenarios = result.GetProperty("scenarios").EnumerateArray().ToArray();
        scenarios[0].TryGetProperty("patch", out _).Should().BeFalse();
        scenarios[1].GetProperty("patch").GetRawText().Should().Be(patch.GetRawText());
    }

    [Fact]
    public async Task Optimize_NamesTheModelByItsHash_AndListsWhatItAssumed_ExceptTheVariablesItSearched()
    {
        var (tools, id) = await Created(Line);
        string hash = (await Experiment(tools, id)).GetProperty("model_sha256").GetString()!;

        JsonElement result = Json(await Call(tools, nameof(SimulationTools.Optimize), new
        {
            model_id = id,
            variables = Json("""[{"path":"s.servers","type":"integer","min":1,"max":2}]"""),
            objective = Json("""{"metric":"s.servers","direction":"min"}"""),
            duration = 100.0,
        }));

        result.GetProperty("model_sha256").GetString().Should().Be(hash);
        Assumed(result).Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["warmup"] = "a tenth of the duration",
            ["seed"] = "the model's",
            ["replications_per_eval"] = "5",
            ["metrics"] = "the default set; the model declares none",
            ["q.capacity"] = "unlimited",
            ["q.discipline"] = "fifo",
        });
    }
}
