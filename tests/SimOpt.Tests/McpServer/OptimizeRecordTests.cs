using System;
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
/// SIM-86: <c>optimize</c> writes the same run record as <c>run_experiment</c> (<c>tool_call</c>
/// "optimize"), with the strategy and seeds resolved, and <c>reproduce_run</c> repeats the search from the
/// record alone. A search stopped by the clock cannot be repeated exactly, so a record is refused with
/// <c>max_seconds</c>.
/// </summary>
public sealed class OptimizeRecordTests : IDisposable
{
    private const string Clinic = """
        {"schema_version":"1","name":"clinic","seed":7,
         "nodes":[
          {"id":"arrivals","type":"source","params":{"arrival":{"type":"exponential","mean":5.0}}},
          {"id":"exam_q","type":"queue"},
          {"id":"exam","type":"station","params":{"servers":8,"service":{"type":"triangular","min":20,"mode":30,"max":40}}},
          {"id":"exit","type":"sink"}],
         "connections":[["arrivals","exam_q"],["exam_q","exam"],["exam","exit"]],
         "metrics":[{"id":"wait_exam","kind":"wait_time","node":"exam_q"}]}
        """;

    private static readonly JsonElement Rooms = Json("""[{"path":"exam.servers","type":"integer","min":5,"max":10}]""");
    private static readonly JsonElement FewestUnder15 =
        Json("""{"metric":"exam.servers","direction":"min","constraints":[{"metric":"wait_exam","op":"<","value":15}]}""");

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim86-opt-").FullName;

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

    private async Task<(JsonElement Answer, string Path)> Searched(object? extra = null)
    {
        var tools = new SimulationTools(new ModelRegistry());
        string id = Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic) }))
            .GetProperty("model_id").GetString()!;
        string path = Path.Combine(folder, "search.json");
        var arguments = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["model_id"] = id, ["variables"] = Rooms, ["objective"] = FewestUnder15, ["duration"] = 300.0,
            ["replications_per_eval"] = 3, ["record"] = path,
        };
        if (extra != null)
            foreach (JsonProperty p in JsonSerializer.SerializeToElement(extra).EnumerateObject()) arguments[p.Name] = p.Value;
        return (Json(await Call(tools, nameof(SimulationTools.Optimize), arguments)), path);
    }

    private static async Task<JsonElement> Reproduced(string path) =>
        Json(await Call(new SimulationTools(new ModelRegistry()), nameof(SimulationTools.ReproduceRun), new { path }));

    [Fact]
    public async Task TheRecord_HoldsTheSearchWithItsStrategyAndSeedsResolved()
    {
        var (answer, path) = await Searched();

        JsonElement record = Json(File.ReadAllText(path));

        answer.GetProperty("record").GetProperty("path").GetString().Should().Be(path);
        record.GetProperty("tool_call").GetString().Should().Be("optimize");
        JsonElement call = record.GetProperty("call");
        call.GetProperty("strategy").GetString().Should().Be("grid", "auto resolves to grid on six points");
        call.GetProperty("seed").GetInt32().Should().Be(7);
        call.GetProperty("warmup").GetDouble().Should().Be(30);
        call.GetProperty("replications_per_eval").GetInt32().Should().Be(3);
        call.GetProperty("variables")[0].GetProperty("path").GetString().Should().Be("exam.servers");
        JsonSerializer.Serialize(record.GetProperty("result").GetProperty("best"))
            .Should().Be(JsonSerializer.Serialize(answer.GetProperty("best")));
        record.GetProperty("limits").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnUntouchedSearchRecord_ReproducesIdentically_OnAnotherServer()
    {
        var (_, path) = await Searched();

        JsonElement check = await Reproduced(path);

        check.GetProperty("record_intact").GetBoolean().Should().BeTrue();
        check.GetProperty("model_intact").GetBoolean().Should().BeTrue();
        check.GetProperty("identical").GetBoolean().Should().BeTrue(check.GetRawText());
    }

    [Fact]
    public async Task AForgedBestPoint_IsCaught_AndNamed()
    {
        var (answer, path) = await Searched();
        double rooms = answer.GetProperty("best").GetProperty("point").GetProperty("exam.servers").GetDouble();
        string text = File.ReadAllText(path);
        string written = $"\"exam.servers\": {rooms.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        int at = text.IndexOf(written, text.IndexOf("\"result\"", StringComparison.Ordinal), StringComparison.Ordinal);
        at.Should().BeGreaterThan(0);
        File.WriteAllText(path, text.Substring(0, at) + $"\"exam.servers\": {rooms - 1}" + text.Substring(at + written.Length));

        JsonElement check = await Reproduced(path);

        check.GetProperty("record_intact").GetBoolean().Should().BeFalse();
        check.GetProperty("identical").GetBoolean().Should().BeFalse();
        check.GetProperty("differences").EnumerateArray().Select(d => d.GetString())
            .Should().Contain(d => d!.StartsWith("best"));
    }

    [Fact]
    public async Task ASearchStoppedByTheClock_CannotBeRecorded()
    {
        var (answer, path) = await Searched(new { max_seconds = 30.0 });

        answer.GetProperty("error").GetString().Should().Contain("max_seconds");
        File.Exists(path).Should().BeFalse();
    }
}
