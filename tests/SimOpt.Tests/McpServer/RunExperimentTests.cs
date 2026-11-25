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
/// SIM-66 step 6 (D10-D12): <c>run_experiment</c> reports a model's declared metrics with confidence
/// intervals over replications (UN-012), runs scenarios in the same call and states whether each
/// differs from the baseline (UN-013), stays small enough for an agent's context (UN-023), and carries
/// what a third party needs to reproduce it (UN-034). Scenarios share the base seed, and streams are
/// keyed by node id, so a node a patch leaves alone draws the same numbers in every scenario; the
/// difference is a paired interval over replications. The acceptance test is the clinic walkthrough of
/// the readiness review, one call per step.
/// </summary>
public class RunExperimentTests
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

    private const string Line = """
        {"name":"line","seed":3,
         "nodes":[{"id":"in","type":"source","params":{"arrival":{"type":"exponential","mean":1}}},
                  {"id":"q","type":"queue"},
                  {"id":"s","type":"station","params":{"service":{"type":"exponential","mean":0.8}}},
                  {"id":"out","type":"sink"}],
         "connections":[["in","q"],["q","s"],["s","out"]]}
        """;

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static AIFunction Tool(SimulationTools tools, string name) => AIFunctionFactory.Create(
        typeof(SimulationTools).GetMethod(name)!, tools,
        new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });

    private static async Task<(SimulationTools Tools, string Id)> Created(string topology)
    {
        var tools = new SimulationTools(new ModelRegistry());
        object? created = await Tool(tools, nameof(SimulationTools.CreateModel)).InvokeAsync(new AIFunctionArguments { ["topology"] = Json(topology) });
        return (tools, Json(created!.ToString()!).GetProperty("model_id").GetString()!);
    }

    private static async Task<string> Run(SimulationTools tools, string id, double duration, string? extra = null)
    {
        var arguments = new AIFunctionArguments { ["model_id"] = id, ["duration"] = duration };
        if (extra != null)
            foreach (JsonProperty p in Json(extra).EnumerateObject()) arguments[p.Name] = p.Value;
        object? result = await Tool(tools, nameof(SimulationTools.RunExperiment)).InvokeAsync(arguments);
        return result!.ToString()!;
    }

    private static JsonElement Scenario(JsonElement result, string name) =>
        result.GetProperty("scenarios").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);

    private static double Mean(JsonElement scenario, string metric) =>
        scenario.GetProperty("metrics").GetProperty(metric).GetProperty("mean").GetDouble();

    [Fact]
    public async Task TheClinic_ReportsItsThreeMetricsWithIntervals_InUnder1200Characters()
    {
        var (tools, id) = await Created(Clinic);

        string text = await Run(tools, id, 540, """{"warmup": 60, "replications": 10}""");
        JsonElement result = Json(text);

        result.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        JsonElement baseline = Scenario(result, "baseline");
        baseline.GetProperty("metrics").EnumerateObject().Select(m => m.Name).Should().Equal("wait_exam", "util_exam", "los");
        Mean(baseline, "util_exam").Should().BeApproximately(0.75, 0.08);
        JsonElement ci = baseline.GetProperty("metrics").GetProperty("util_exam").GetProperty("ci95");
        ci[0].GetDouble().Should().BeLessThan(Mean(baseline, "util_exam"));
        ci[1].GetDouble().Should().BeGreaterThan(Mean(baseline, "util_exam"));
        text.Length.Should().BeLessThanOrEqualTo(1200, "about 400 tokens at three characters a token (D12); it was {0}", text.Length);
    }

    [Fact]
    public async Task FluSeason_IsAScenarioInTheSameCall_SignificantlyWorse_AndExamSaturated()
    {
        var (tools, id) = await Created(Clinic);

        string text = await Run(tools, id, 540, """
            {"warmup": 60, "replications": 10,
             "scenarios": [{"name": "flu", "patch": [{"op": "set", "path": "arrivals.arrival.mean", "value": 2.5}]}]}
            """);
        JsonElement result = Json(text);

        JsonElement flu = Scenario(result, "flu");
        JsonElement wait = flu.GetProperty("vs_baseline").GetProperty("wait_exam");
        wait.GetProperty("diff").GetDouble().Should().BeGreaterThan(0);
        wait.GetProperty("significant").GetBoolean().Should().BeTrue();
        flu.GetProperty("flags").GetProperty("saturated").EnumerateArray().Select(e => e.GetString()).Should().Equal("exam");
        Scenario(result, "baseline").TryGetProperty("vs_baseline", out _).Should().BeFalse();
        result.GetProperty("summary").GetString().Should().Contain("flu").And.Contain("wait_exam");
        text.Length.Should().BeLessThanOrEqualTo(2000, "it was {0}", text.Length);
    }

    [Fact]
    public async Task AScenarioThatChangesNothing_EqualsTheBaselineExactly()
    {
        // common random numbers: the same seed and the same node ids draw the same numbers
        var (tools, id) = await Created(Clinic);

        JsonElement result = Json(await Run(tools, id, 540, """{"warmup": 60, "scenarios": [{"name": "same", "patch": []}]}"""));

        JsonElement same = Scenario(result, "same");
        foreach (JsonProperty diff in same.GetProperty("vs_baseline").EnumerateObject())
        {
            diff.Value.GetProperty("diff").GetDouble().Should().Be(0);
            diff.Value.GetProperty("significant").GetBoolean().Should().BeFalse();
        }
        Mean(same, "los").Should().Be(Mean(Scenario(result, "baseline"), "los"));
    }

    [Fact]
    public async Task AModelWithoutDeclaredMetrics_IsMeasuredByTheDefaultSet()
    {
        var (tools, id) = await Created(Line);

        JsonElement baseline = Scenario(Json(await Run(tools, id, 1000)), "baseline");

        baseline.GetProperty("metrics").EnumerateObject().Select(m => m.Name)
                .Should().BeEquivalentTo("cycle_time", "throughput_out", "util_s", "wait_q");
        Mean(baseline, "util_s").Should().BeApproximately(0.8, 0.08);
    }

    [Fact]
    public async Task TheMetricsArgument_ChoosesWhatIsReported_AndAnUnknownIdIsAnError()
    {
        var (tools, id) = await Created(Clinic);

        JsonElement chosen = Json(await Run(tools, id, 540, """{"metrics": ["util_exam"]}"""));
        JsonElement unknown = Json(await Run(tools, id, 540, """{"metrics": ["happiness"]}"""));

        Scenario(chosen, "baseline").GetProperty("metrics").EnumerateObject().Select(m => m.Name).Should().Equal("util_exam");
        unknown.GetProperty("error").GetString().Should().Contain("happiness").And.Contain("wait_exam");
    }

    [Fact]
    public async Task AnEarlierVersion_CanBeRun()
    {
        var (tools, id) = await Created(Clinic);
        await Tool(tools, nameof(SimulationTools.PatchModel)).InvokeAsync(new AIFunctionArguments
        {
            ["model_id"] = id, ["patch"] = Json("""[{"op": "set", "path": "exam.servers", "value": 12}]"""),
        });

        JsonElement latest = Json(await Run(tools, id, 540, """{"warmup": 60}"""));
        JsonElement first = Json(await Run(tools, id, 540, """{"warmup": 60, "version": 1}"""));

        latest.GetProperty("version").GetInt32().Should().Be(2);
        first.GetProperty("version").GetInt32().Should().Be(1);
        Mean(Scenario(latest, "baseline"), "util_exam").Should().BeApproximately(0.5, 0.06, "12 rooms instead of 8");
        Mean(Scenario(first, "baseline"), "util_exam").Should().BeApproximately(0.75, 0.08);
    }

    [Fact]
    public async Task AScenarioWhosePatchFails_IsReported_AndTheOthersStillRun()
    {
        var (tools, id) = await Created(Clinic);

        JsonElement result = Json(await Run(tools, id, 540, """
            {"scenarios": [{"name": "broken", "patch": [{"op": "set", "path": "ward.servers", "value": 2}]},
                           {"name": "fine", "patch": [{"op": "set", "path": "exam.servers", "value": 9}]}]}
            """));

        Scenario(result, "broken").GetProperty("errors")[0].GetString().Should().Contain("ward");
        Scenario(result, "broken").TryGetProperty("metrics", out _).Should().BeFalse();
        Scenario(result, "fine").GetProperty("metrics").EnumerateObject().Should().HaveCount(3);
    }

    [Fact]
    public async Task ItemsRefusedByAFullQueue_AreFlagged_PerReplication()
    {
        var (tools, id) = await Created(Line.Replace("""{"id":"q","type":"queue"}""", """{"id":"q","type":"queue","params":{"capacity":3}}""")
                                            .Replace("\"mean\":0.8", "\"mean\":1.5"));

        JsonElement flags = Scenario(Json(await Run(tools, id, 300)), "baseline").GetProperty("flags");

        flags.GetProperty("refused").GetProperty("q").GetDouble().Should().BeGreaterThan(50, "a third of 300 arrivals at load 1.5");
        flags.GetProperty("saturated").EnumerateArray().Select(e => e.GetString()).Should().Equal("s");
    }

    [Fact]
    public async Task ARunInWhichNothingFinishes_IsFlagged()
    {
        var (tools, id) = await Created(Line.Replace("""{"type":"exponential","mean":0.8}""", """{"type":"constant","value":1000}"""));

        JsonElement flags = Scenario(Json(await Run(tools, id, 500)), "baseline").GetProperty("flags");

        flags.GetProperty("nothing_finished").GetBoolean().Should().BeTrue();
    }

    [Theory]
    [InlineData(0d, null)]
    [InlineData(100d, """{"warmup": 100}""")]
    [InlineData(100d, """{"replications": 0}""")]
    [InlineData(100d, """{"replications": 1001}""")]
    [InlineData(100d, """{"version": 2}""")]
    public async Task AnImpossibleRequest_IsAnError(double duration, string? extra)
    {
        var (tools, id) = await Created(Line);

        Json(await Run(tools, id, duration, extra)).GetProperty("error").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TheSameCall_GivesTheSameResult_AndCarriesItsProvenance()
    {
        var (tools, id) = await Created(Clinic);

        string first = await Run(tools, id, 540, """{"warmup": 60, "seed": 99}""");
        string second = await Run(tools, id, 540, """{"warmup": 60, "seed": 99}""");

        second.Should().Be(first);
        JsonElement result = Json(first);
        result.GetProperty("seed").GetInt32().Should().Be(99);
        result.GetProperty("duration").GetDouble().Should().Be(540);
        result.GetProperty("warmup").GetDouble().Should().Be(60);
        result.GetProperty("replications").GetInt32().Should().Be(10);
        result.GetProperty("confidence").GetDouble().Should().Be(0.95);
        result.GetProperty("tool").GetString().Should().StartWith("SimOpt");
    }

    [Fact]
    public async Task TheWarmupDefaultsToATenthOfTheDuration_AndTheSeedToTheModels()
    {
        var (tools, id) = await Created(Clinic);

        JsonElement result = Json(await Run(tools, id, 500));

        result.GetProperty("warmup").GetDouble().Should().Be(50);
        result.GetProperty("seed").GetInt32().Should().Be(7);
    }
}
