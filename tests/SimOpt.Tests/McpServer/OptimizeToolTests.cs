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
/// SIM-69 (D6, D9): the <c>optimize</c> tool. One call searches a model's decision variables for the
/// best point that keeps its constraints, enumerating a space that fits the budget and searching at
/// random otherwise, and answers with the best point, its metrics with intervals, the runners-up and
/// provenance. Acceptance: the clinic of the readiness review, "fewest exam rooms keeping the mean exam
/// wait under 15 minutes", is answered with 7 rooms in one call.
/// </summary>
public class OptimizeToolTests
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

    private const string FewestRooms = """
        {"variables": [{"path": "exam.servers", "type": "integer", "min": 4, "max": 12}],
         "objective": {"metric": "exam.servers", "direction": "min",
                       "constraints": [{"metric": "wait_exam", "op": "<", "value": 15}]},
         "duration": 540, "warmup": 60}
        """;

    // two variables, 6 x 9 = 54 points: more than the searches below may evaluate
    private const string TwoStations = """
        {"variables": [{"path": "triage.servers", "type": "integer", "min": 1, "max": 6},
                       {"path": "exam.servers", "type": "integer", "min": 4, "max": 12}],
         "objective": {"metric": "exam.servers", "direction": "min",
                       "constraints": [{"metric": "wait_exam", "op": "<", "value": 15}]},
         "duration": 540, "warmup": 60}
        """;

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static AIFunction Tool(SimulationTools tools, string name) => AIFunctionFactory.Create(
        typeof(SimulationTools).GetMethod(name)!, tools,
        new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });

    private static async Task<(SimulationTools Tools, string Id)> Created()
    {
        var tools = new SimulationTools(new ModelRegistry());
        object? created = await Tool(tools, nameof(SimulationTools.CreateModel)).InvokeAsync(new AIFunctionArguments { ["topology"] = Json(Clinic) });
        return (tools, Json(created!.ToString()!).GetProperty("model_id").GetString()!);
    }

    private static async Task<string> Optimize(SimulationTools tools, string id, string request, string? overrides = null)
    {
        var arguments = new AIFunctionArguments { ["model_id"] = id };
        foreach (JsonProperty p in Json(request).EnumerateObject()) arguments[p.Name] = p.Value;
        if (overrides != null)
            foreach (JsonProperty p in Json(overrides).EnumerateObject()) arguments[p.Name] = p.Value;
        object? result = await Tool(tools, nameof(SimulationTools.Optimize)).InvokeAsync(arguments);
        return result!.ToString()!;
    }

    [Fact]
    public async Task TheClinic_FewestExamRoomsKeepingTheWaitUnder15_IsSeven_InOneCall()
    {
        var (tools, id) = await Created();

        string text = await Optimize(tools, id, FewestRooms);
        JsonElement result = Json(text);

        result.TryGetProperty("error", out JsonElement error).Should().BeFalse(error.ToString());
        result.GetProperty("strategy").GetString().Should().Be("grid", "nine points fit the default budget");
        result.GetProperty("evaluations").GetInt32().Should().Be(9);
        JsonElement best = result.GetProperty("best");
        best.GetProperty("point").GetProperty("exam.servers").GetDouble().Should().Be(7);
        best.GetProperty("feasible").GetBoolean().Should().BeTrue();
        best.GetProperty("metrics").GetProperty("wait_exam").GetProperty("ci95")[1].GetDouble().Should().BeLessThan(15);
        result.GetProperty("top").EnumerateArray().Select(t => t.GetProperty("point").GetProperty("exam.servers").GetDouble())
              .Should().StartWith(new[] { 7d, 8d });
        result.GetProperty("summary").GetString().Should().Contain("exam.servers = 7").And.Contain("run_experiment");
        text.Length.Should().BeLessThanOrEqualTo(1500, "it was {0}", text.Length);
    }

    [Fact]
    public async Task AContinuousSpace_IsSearchedByTheEvolutionaryAlgorithm_WithinTheBudget()
    {
        var (tools, id) = await Created();

        JsonElement result = Json(await Optimize(tools, id, """
            {"variables": [{"path": "arrivals.arrival.mean", "type": "real", "min": 3, "max": 10}],
             "objective": {"metric": "util_exam", "direction": "max",
                           "constraints": [{"metric": "wait_exam", "op": "<", "value": 5}]},
             "duration": 540, "max_evaluations": 12}
            """));

        // SIM-68 step 4: at the default budget the EA beat random search on every space measured
        result.GetProperty("strategy").GetString().Should().Be("ea");
        result.GetProperty("evaluations").GetInt32().Should().BeLessThanOrEqualTo(12);
        result.GetProperty("stopped").GetString().Should().Be("budget");
        result.GetProperty("best").GetProperty("feasible").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task WhenNoPointKeepsTheConstraints_TheNearestIsReported_AsInfeasible()
    {
        var (tools, id) = await Created();

        JsonElement result = Json(await Optimize(tools, id, FewestRooms,
            """{"variables": [{"path": "exam.servers", "type": "integer", "min": 4, "max": 5}]}"""));

        JsonElement best = result.GetProperty("best");
        best.GetProperty("feasible").GetBoolean().Should().BeFalse();
        best.GetProperty("point").GetProperty("exam.servers").GetDouble().Should().Be(5, "five rooms break the bound by less");
        best.GetProperty("violations")[0].GetString().Should().Contain("wait_exam");
        result.GetProperty("summary").GetString().Should().Contain("No point");
    }

    [Fact]
    public async Task TheSameCall_GivesTheSameResult()
    {
        var (tools, id) = await Created();

        string first = await Optimize(tools, id, FewestRooms, """{"strategy": "random", "max_evaluations": 6, "seed": 5}""");
        string second = await Optimize(tools, id, FewestRooms, """{"strategy": "random", "max_evaluations": 6, "seed": 5}""");

        second.Should().Be(first);
    }

    [Fact]
    public async Task ATimeLimit_StopsTheSearch_AndSaysSo()
    {
        var (tools, id) = await Created();

        JsonElement result = Json(await Optimize(tools, id, FewestRooms, """{"strategy": "random", "max_seconds": 0.000001}"""));

        result.GetProperty("stopped").GetString().Should().Be("time");
        result.GetProperty("evaluations").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    // ── SIM-68 step 4: simulated annealing, the evolutionary algorithm and the particle swarm ──

    [Theory]
    [InlineData("sa")]
    [InlineData("ea")]
    [InlineData("pso")]
    public async Task EachSearch_FindsTheGridsOptimum_OnTwoVariables_EvaluatingFewerPoints(string strategy)
    {
        // under common random numbers the grid's best is the exact optimum of the space (D6)
        var (tools, id) = await Created();
        JsonElement grid = Json(await Optimize(tools, id, TwoStations, """{"strategy": "grid", "max_evaluations": 54}"""));

        JsonElement result = Json(await Optimize(tools, id, TwoStations, $$"""{"strategy": "{{strategy}}", "max_evaluations": 40}"""));

        result.GetProperty("strategy").GetString().Should().Be(strategy);
        result.GetProperty("evaluations").GetInt32().Should().BeLessThanOrEqualTo(40);
        result.GetProperty("best").GetProperty("feasible").GetBoolean().Should().BeTrue();
        result.GetProperty("best").GetProperty("objective").GetDouble()
            .Should().Be(grid.GetProperty("best").GetProperty("objective").GetDouble());
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("ea")]
    [InlineData("pso")]
    public async Task EachSearch_GivesTheSameResult_FromTheSameCall(string strategy)
    {
        var (tools, id) = await Created();
        string call = $$"""{"strategy": "{{strategy}}", "max_evaluations": 15, "seed": 5}""";

        string first = await Optimize(tools, id, TwoStations, call);
        string second = await Optimize(tools, id, TwoStations, call);

        second.Should().Be(first);
        Json(first).GetProperty("evaluations").GetInt32().Should().Be(15);
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("ea")]
    [InlineData("pso")]
    public async Task EachSearch_StopsAtTheBudget(string strategy)
    {
        var (tools, id) = await Created();

        JsonElement result = Json(await Optimize(tools, id, TwoStations, $$"""{"strategy": "{{strategy}}", "max_evaluations": 8}"""));

        result.GetProperty("evaluations").GetInt32().Should().Be(8);
        result.GetProperty("stopped").GetString().Should().Be("budget");
    }

    [Fact]
    public async Task AnImpossibleRequest_IsAnError()
    {
        var (tools, id) = await Created();

        Json(await Optimize(tools, "nope", FewestRooms)).GetProperty("error").GetString().Should().Contain("nope");
        Json(await Optimize(tools, id, FewestRooms, """{"strategy": "hill"}""")).GetProperty("error").GetString()
            .Should().Contain("hill").And.Contain("grid").And.Contain("pso");
        Json(await Optimize(tools, id, FewestRooms, """{"strategy": "grid", "max_evaluations": 5}""")).GetProperty("error").GetString()
            .Should().Contain("9 points");
        Json(await Optimize(tools, id, FewestRooms, """{"objective": {"metric": "joy", "direction": "min"}}""")).GetProperty("error").GetString()
            .Should().Contain("joy");
    }
}
