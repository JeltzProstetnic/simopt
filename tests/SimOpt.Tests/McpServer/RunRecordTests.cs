using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// SIM-86 slice B (UN-033, UN-034, the hook for UN-036): <c>run_experiment</c> with a <c>record</c> path
/// writes one file that holds everything needed to repeat the run: the model's schema v1 document, the
/// call with every default resolved, the answer, and a SHA-256 over the record. <c>reproduce_run</c>
/// re-runs such a file on any server and says whether the record and its model are as written and
/// whether every figure came out the same.
/// </summary>
public sealed class RunRecordTests : IDisposable
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

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim86b-").FullName;

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
        string answer = await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic) });
        return (tools, Json(answer).GetProperty("model_id").GetString()!);
    }

    private static readonly JsonElement Flu = Json("""[{"name":"flu","patch":[{"op":"set","path":"arrivals.arrival.mean","value":4}]}]""");

    /// <summary>A recorded run of the clinic with a flu scenario; returns the answer and the record's path.</summary>
    private async Task<(JsonElement Answer, string Path)> Recorded(string name = "run.json")
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, name);
        string answer = await Call(tools, nameof(SimulationTools.RunExperiment),
            new { model_id = id, duration = 540.0, replications = 5, scenarios = Flu, record = path });
        return (Json(answer), path);
    }

    private static async Task<JsonElement> Reproduced(string path) =>
        Json(await Call(new SimulationTools(new ModelRegistry()), nameof(SimulationTools.ReproduceRun), new { path }));

    [Fact]
    public async Task TheRecord_HoldsTheModelTheResolvedCallAndTheAnswer()
    {
        var (answer, path) = await Recorded();

        JsonElement record = Json(File.ReadAllText(path));

        record.GetProperty("record").GetString().Should().Be("simopt-run/1");
        record.GetProperty("tool_call").GetString().Should().Be("run_experiment");
        JsonElement call = record.GetProperty("call");
        call.GetProperty("duration").GetDouble().Should().Be(540);
        call.GetProperty("replications").GetInt32().Should().Be(5);
        call.GetProperty("warmup").GetDouble().Should().Be(54, "the default, a tenth of the duration, written out");
        call.GetProperty("seed").GetInt32().Should().Be(7, "the model's seed, written out");
        Compact(call.GetProperty("scenarios")).Should().Be(Compact(Flu));
        record.GetProperty("model").GetProperty("nodes").GetArrayLength().Should().Be(4);
        Compact(record.GetProperty("result").GetProperty("scenarios")).Should().Be(Compact(answer.GetProperty("scenarios")));
        record.GetProperty("result").GetProperty("model_sha256").GetString()
            .Should().Be(answer.GetProperty("model_sha256").GetString());
        record.GetProperty("record_sha256").GetString().Should().MatchRegex("^[0-9a-f]{64}$");
        File.ReadAllText(path).Should().NotContain("\r");
    }

    [Fact]
    public async Task TheRecord_StatesWhatTheModelDoesNotRepresent()
    {
        var (_, path) = await Recorded();

        JsonElement record = Json(File.ReadAllText(path));

        record.GetProperty("limits").EnumerateArray().Select(e => e.GetString())
              .Should().Equal(SimOpt.Engine.Api.Models.SchemaCatalog.Limits);
    }

    [Fact]
    public async Task TheAnswer_SaysWhereTheRecordIs_AndItsHash()
    {
        var (answer, path) = await Recorded();

        JsonElement written = answer.GetProperty("record");

        written.GetProperty("path").GetString().Should().Be(path);
        written.GetProperty("record_sha256").GetString().Should().Be(Json(File.ReadAllText(path)).GetProperty("record_sha256").GetString());
    }

    [Fact]
    public async Task AnUntouchedRecord_ReproducesIdentically_OnAnotherServer()
    {
        var (_, path) = await Recorded();

        JsonElement check = await Reproduced(path);

        check.GetProperty("record_intact").GetBoolean().Should().BeTrue();
        check.GetProperty("model_intact").GetBoolean().Should().BeTrue();
        check.GetProperty("identical").GetBoolean().Should().BeTrue(check.GetRawText());
        check.TryGetProperty("differences", out _).Should().BeFalse();
        check.GetProperty("recorded_with").GetString().Should().Be(ExperimentRunner.ToolVersion);
        check.GetProperty("reproduced_with").GetString().Should().Be(ExperimentRunner.ToolVersion);
    }

    [Fact]
    public async Task AFigureChangedInTheRecord_IsCaught_AndNamed()
    {
        var (answer, path) = await Recorded();
        double mean = answer.GetProperty("scenarios")[0].GetProperty("metrics").GetProperty("los").GetProperty("mean").GetDouble();
        string text = File.ReadAllText(path);
        string forged = $"\"mean\": {(mean + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        File.WriteAllText(path, ReplaceFirst(text, $"\"mean\": {mean.ToString(System.Globalization.CultureInfo.InvariantCulture)}", forged));

        JsonElement check = await Reproduced(path);

        check.GetProperty("record_intact").GetBoolean().Should().BeFalse();
        check.GetProperty("identical").GetBoolean().Should().BeFalse();
        check.GetProperty("differences").EnumerateArray().Select(d => d.GetString())
            .Should().Contain(d => d!.StartsWith("baseline los:"));
    }

    [Fact]
    public async Task AModelChangedInTheRecord_IsCaught()
    {
        var (_, path) = await Recorded();
        File.WriteAllText(path, ReplaceFirst(File.ReadAllText(path), "\"servers\": 8", "\"servers\": 9"));

        JsonElement check = await Reproduced(path);

        check.GetProperty("record_intact").GetBoolean().Should().BeFalse();
        check.GetProperty("model_intact").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AnExistingFile_IsKept_AndNothingIsRun()
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, "kept.json");
        File.WriteAllText(path, "mine");

        JsonElement answer = Json(await Call(tools, nameof(SimulationTools.RunExperiment),
            new { model_id = id, duration = 540.0, record = path }));

        answer.GetProperty("error").GetString().Should().Contain("exists");
        File.ReadAllText(path).Should().Be("mine");
    }

    [Theory]
    [InlineData("run.txt")]
    [InlineData("missing/run.json")]
    public async Task ARecordPathThatCannotBeWritten_IsAnError(string name)
    {
        var (tools, id) = await Created();

        JsonElement answer = Json(await Call(tools, nameof(SimulationTools.RunExperiment),
            new { model_id = id, duration = 540.0, record = Path.Combine(folder, name) }));

        answer.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AFileThatIsNoRecord_IsAnError()
    {
        string path = Path.Combine(folder, "model.json");
        File.WriteAllText(path, Clinic);

        JsonElement check = await Reproduced(path);

        check.GetProperty("error").GetString().Should().Contain("simopt-run/1");
    }

    private static string Compact(JsonElement element) => JsonSerializer.Serialize(element);

    private static string ReplaceFirst(string text, string old, string replacement)
    {
        int at = text.IndexOf(old, StringComparison.Ordinal);
        at.Should().BeGreaterThanOrEqualTo(0, $"the record should contain {old}");
        return text.Substring(0, at) + replacement + text.Substring(at + old.Length);
    }
}
