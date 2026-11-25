using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-86 slice C (UN-034, Q-PROV-REPORT): <c>report_run</c> renders a run record as a Markdown appendix for an
/// expert report, in English or German: the provenance (record and model hashes, checked), the run's settings,
/// the results with their intervals, what the run assumed, what the model does not represent, and how to repeat
/// it. It reads the record and checks its hashes; it does not run anything.
/// </summary>
public sealed class RunReportTests : IDisposable
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

    private static readonly JsonElement Flu = Json("""[{"name":"flu","patch":[{"op":"set","path":"arrivals.arrival.mean","value":4}]}]""");

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim86c-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static string En(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
        return (tools, Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic) })).GetProperty("model_id").GetString()!);
    }

    /// <summary>The clinic run with a flu scenario, recorded; returns the answer and the record's path.</summary>
    private async Task<(JsonElement Answer, string Path)> Recorded()
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, "clinic-run.json");
        string answer = await Call(tools, nameof(SimulationTools.RunExperiment),
            new { model_id = id, duration = 540.0, replications = 5, scenarios = Flu, record = path });
        return (Json(answer), path);
    }

    private static async Task<JsonElement> Reported(string path, string? language = null) =>
        Json(await Call(new SimulationTools(new ModelRegistry()), nameof(SimulationTools.ReportRun),
            language == null ? new { path } : new { path, language }));

    [Fact]
    public async Task TheEnglishReport_CarriesTheProvenanceSettingsResultsAssumptionsLimitsAndHowToRepeat()
    {
        var (answer, path) = await Recorded();

        JsonElement report = await Reported(path);
        string md = report.GetProperty("markdown").GetString()!;

        report.GetProperty("language").GetString().Should().Be("en");
        report.GetProperty("record_intact").GetBoolean().Should().BeTrue();
        report.GetProperty("model_intact").GetBoolean().Should().BeTrue();
        md.Should().StartWith("# Simulation run report");
        md.Should().Contain("`clinic-run.json`").And.Contain(Json(File.ReadAllText(path)).GetProperty("record_sha256").GetString());
        md.Should().Contain(answer.GetProperty("model_sha256").GetString()).And.Contain("run_experiment");
        md.Should().Contain("| Replications | 5 |").And.Contain("| Run length | 540 |").And.Contain("| Warm-up | 54 |");
        md.Should().Contain("| Base seed | 7 |");

        // every recorded figure as recorded (four significant digits), with its interval
        foreach (JsonElement scenario in answer.GetProperty("scenarios").EnumerateArray())
            foreach (JsonProperty metric in scenario.GetProperty("metrics").EnumerateObject())
            {
                double mean = metric.Value.GetProperty("mean").GetDouble();
                double[] ci = metric.Value.GetProperty("ci95").EnumerateArray().Select(e => e.GetDouble()).ToArray();
                md.Should().Contain($"| {metric.Name} | {En(mean)} | [{En(ci[0])}, {En(ci[1])}] |");
            }
        md.Should().Contain("## Scenario: flu").And.Contain("`set arrivals.arrival.mean = 4`");
        md.Should().Contain("| Metric | Difference from baseline | 95 % interval | Significant |");

        md.Should().Contain("## What the run assumed").And.Contain("| warmup | a tenth of the duration: 54 |");
        foreach (string limit in SchemaCatalog.Limits) md.Should().Contain(limit);
        md.Should().Contain("reproduce_run").And.Contain("load_model");
        md.Should().NotContain("\r");
    }

    [Fact]
    public async Task TheGermanReport_IsGermanThroughout_WithDecimalCommas()
    {
        var (answer, path) = await Recorded();

        string md = (await Reported(path, "de")).GetProperty("markdown").GetString()!;

        md.Should().StartWith("# Bericht über einen Simulationslauf");
        md.Should().Contain("| Replikationen | 5 |").And.Contain("| Einschwingphase | 54 |");
        double mean = answer.GetProperty("scenarios")[0].GetProperty("metrics").GetProperty("wait_exam").GetProperty("mean").GetDouble();
        md.Should().Contain("| wait_exam | " + mean.ToString(System.Globalization.CultureInfo.GetCultureInfo("de-AT")) + " |");
        md.Should().Contain("| warmup | ein Zehntel der Laufzeit: 54 |");
        foreach (string limit in SchemaCatalog.Limits) md.Should().NotContain(limit, "every limit is given in German");
        md.Should().Contain("## Was das Modell nicht abbildet");
    }

    [Fact]
    public async Task ARunOfAModelWithoutASeed_ReportsTheDefaultSeed_InEnglishAndGerman()
    {
        var tools = new SimulationTools(new ModelRegistry());
        string id = Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic.Replace("\"seed\":7,", "")) }))
            .GetProperty("model_id").GetString()!;
        string path = Path.Combine(folder, "seedless-run.json");
        await Call(tools, nameof(SimulationTools.RunExperiment), new { model_id = id, duration = 540.0, replications = 2, record = path });

        string en = (await Reported(path)).GetProperty("markdown").GetString()!;
        string de = (await Reported(path, "de")).GetProperty("markdown").GetString()!;

        en.Should().Contain("| seed | the default; the model gives none: 42 |");
        de.Should().Contain("| seed | der Standardwert; das Modell gibt keinen an: 42 |");
    }

    [Fact]
    public void EveryLimitOfTheSchema_HasAGermanText()
    {
        RunReport.GermanLimits.Keys.Should().BeEquivalentTo(SchemaCatalog.Limits);
        RunReport.GermanLimits.Values.Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);
    }

    [Fact]
    public async Task AChangedRecord_IsReportedAsChanged()
    {
        var (_, path) = await Recorded();
        JsonObject record = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        record["result"]!["scenarios"]![0]!["metrics"]!["wait_exam"]!["mean"] = 0.5;
        File.WriteAllText(path, record.ToJsonString());

        JsonElement report = await Reported(path);

        report.GetProperty("record_intact").GetBoolean().Should().BeFalse();
        report.GetProperty("markdown").GetString().Should().Contain("**changed after it was written**");
    }

    [Fact]
    public async Task ASearchRecord_ReportsTheBestPointAndHowTheSearchRan()
    {
        var (tools, id) = await Created();
        string path = Path.Combine(folder, "search.json");
        await Call(tools, nameof(SimulationTools.Optimize), new
        {
            model_id = id,
            variables = Json("""[{"path":"exam.servers","type":"integer","min":5,"max":10}]"""),
            objective = Json("""{"metric":"exam.servers","direction":"min","constraints":[{"metric":"wait_exam","op":"<","value":15}]}"""),
            duration = 300.0, replications_per_eval = 3, record = path,
        });
        JsonElement record = Json(File.ReadAllText(path));
        JsonElement best = record.GetProperty("result").GetProperty("best");

        string md = (await Reported(path)).GetProperty("markdown").GetString()!;

        md.Should().Contain("optimize").And.Contain("## Best configuration found");
        md.Should().Contain($"| exam.servers | {En(best.GetProperty("point").GetProperty("exam.servers").GetDouble())} |");
        md.Should().Contain($"| Strategy | {record.GetProperty("result").GetProperty("strategy").GetString()} |");
        md.Should().Contain($"| Evaluations | {record.GetProperty("result").GetProperty("evaluations").GetInt32()} |");
        md.Should().Contain("wait_exam < 15");
    }

    [Theory]
    [InlineData("fr", "en, de")]
    [InlineData(null, "No file")]
    public async Task AWrongLanguageOrPath_IsAnError(string? language, string named)
    {
        var (_, path) = await Recorded();
        string target = language == null ? Path.Combine(folder, "missing.json") : path;

        JsonElement report = await Reported(target, language);

        report.GetProperty("error").GetString().Should().Contain(named);
    }
}
