using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Optimization;
using SimOpt.McpServer.Simulation;
using SimOpt.McpServer.Tools;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-140 (UN-015): a search respects limits on what it may change, such as headcount, and never proposes a
/// configuration that breaks them. A constraint may name an <c>expression</c> over the decision variables'
/// paths instead of a metric, a sum with optional whole or decimal factors ("triage.servers + exam.servers",
/// "2 * exam.servers - 1"). It is checked on the point as applied, before anything is simulated: a point that
/// breaks it is never run, costs no evaluation of the budget, and is counted as skipped.
/// </summary>
public sealed class VariableConstraintTests : IDisposable
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
         "metrics":[{"id":"wait_exam","kind":"wait_time","node":"exam_q"},{"id":"los","kind":"cycle_time"}]}
        """;

    private static readonly string[] Paths = { "triage.servers", "exam.servers" };

    private readonly string folder = Directory.CreateTempSubdirectory("simopt-sim140-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static TopologyDefinition ClinicTopology() =>
        JsonSerializer.Deserialize<TopologyDefinition>(Clinic, McpJsonUtilities.DefaultOptions)!;

    private static DecisionVariable[] Staff() => new[]
    {
        new DecisionVariable { Path = "triage.servers", Type = "integer", Min = 1, Max = 4 },
        new DecisionVariable { Path = "exam.servers", Type = "integer", Min = 5, Max = 10 },
    };

    private static ObjectiveDefinition ShortestStay(string op = "<=", double most = 10) => new()
    {
        Metric = "los",
        Direction = "min",
        Constraints = { new ConstraintDefinition { Expression = "triage.servers + exam.servers", Op = op, Value = most } },
    };

    private static TopologyProblem Problem(ObjectiveDefinition objective, int? budget = null) =>
        new(ClinicTopology(), Staff(), objective, duration: 300, warmup: 30, replications: 3, replicationSeed: 7, budget: budget);

    private static TopologySolution At(TopologyProblem problem, params double[] point)
    {
        var solution = (TopologySolution)problem.GenerateCandidates(1, 1).Single();
        solution.Parameters = point;
        return solution;
    }

    [Theory]
    [InlineData("triage.servers + exam.servers", 11)]
    [InlineData("2 * exam.servers - triage.servers + 1", 14)]
    [InlineData("-triage.servers + 10", 7)]
    [InlineData("exam.servers", 8)]
    [InlineData("0.5*exam.servers+triage.servers", 7)]
    public void AnExpression_IsALinearSumOfTheVariables(string text, double atThreeAndEight)
    {
        VariableExpression expression = VariableExpression.Parse(text, Paths);

        expression.Evaluate(new[] { 3d, 8d }).Should().Be(atThreeAndEight);
    }

    [Theory]
    [InlineData("triage.servers + ward.servers", "ward.servers")]
    [InlineData("triage.servers +", "ends")]
    [InlineData("triage.servers exam.servers", "+ or -")]
    [InlineData("2 *", "ends")]
    [InlineData("", "empty")]
    public void AnUnreadableExpression_IsRefused_SayingWhere(string text, string named)
    {
        Action parse = () => VariableExpression.Parse(text, Paths);

        parse.Should().Throw<ArgumentException>().Which.Message.Should().Contain(named);
    }

    [Fact]
    public void APointThatBreaksIt_IsNotRun_AndCostsNoEvaluation()
    {
        TopologyProblem problem = Problem(ShortestStay(), budget: 1);
        TopologySolution tooMany = At(problem, 4, 8), allowed = At(problem, 3, 7);

        problem.Evaluate(tooMany);
        problem.Evaluate(allowed);

        problem.Evaluations.Select(e => e.Point).Should().ContainSingle().Which.Should().Equal(3, 7);
        problem.Skipped.Should().Be(1);
        tooMany.Fitness.Should().BeLessThan(TopologyProblem.Infeasible / 2, "it ranks below every point that keeps the limit");
        problem.StopReason.Should().Be("budget", "only the point that was run counts against the budget of one");
    }

    [Fact]
    public void ABoundNoPointWithinTheVariablesCanKeep_IsRefused()
    {
        Action define = () => Problem(ShortestStay("<", 6));

        define.Should().Throw<ArgumentException>().Which.Message.Should()
              .Contain("no point").And.Contain("triage.servers + exam.servers").And.Contain("at least 6");
    }

    [Theory]
    [InlineData("wait_exam", "triage.servers + exam.servers")]
    [InlineData(null, null)]
    public void AConstraintNamesAMetricOrAnExpression(string? metric, string? expression)
    {
        var objective = new ObjectiveDefinition
        {
            Metric = "los",
            Constraints = { new ConstraintDefinition { Metric = metric, Expression = expression, Op = "<", Value = 15 } },
        };

        Action define = () => Problem(objective);

        define.Should().Throw<ArgumentException>().Which.Message.Should().Contain("a metric or an expression");
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static async Task<string> Call(SimulationTools tools, string name, object arguments)
    {
        var function = AIFunctionFactory.Create(typeof(SimulationTools).GetMethod(name)!, tools,
            new AIFunctionFactoryOptions { SerializerOptions = McpJsonUtilities.DefaultOptions });
        var args = new AIFunctionArguments();
        foreach (JsonProperty p in JsonSerializer.SerializeToElement(arguments).EnumerateObject()) args[p.Name] = p.Value;
        return (await function.InvokeAsync(args))!.ToString()!;
    }

    [Fact]
    public async Task TheClinicWithAtMostTenStaff_NeverProposesEleven_AndSaysWhatItSkipped()
    {
        var tools = new SimulationTools(new ModelRegistry());
        string id = Json(await Call(tools, nameof(SimulationTools.CreateModel), new { topology = Json(Clinic) }))
            .GetProperty("model_id").GetString()!;
        string record = Path.Combine(folder, "staff.json");

        JsonElement answer = Json(await Call(tools, nameof(SimulationTools.Optimize), new
        {
            model_id = id,
            variables = Json("""[{"path":"triage.servers","type":"integer","min":1,"max":4},{"path":"exam.servers","type":"integer","min":5,"max":10}]"""),
            objective = Json("""{"metric":"los","direction":"min","constraints":[{"expression":"triage.servers + exam.servers","op":"<=","value":10}]}"""),
            duration = 300.0, replications_per_eval = 3, max_evaluations = 24, record,
        }));

        // 24 points in the box; 10 of them have more than 10 staff (1+10, 2+9.., 4+7..10)
        answer.GetProperty("strategy").GetString().Should().Be("grid");
        answer.GetProperty("evaluations").GetInt32().Should().Be(14);
        answer.GetProperty("skipped").GetInt32().Should().Be(10);
        foreach (JsonElement point in answer.GetProperty("top").EnumerateArray().Append(answer.GetProperty("best")))
            (point.GetProperty("point").GetProperty("triage.servers").GetDouble() + point.GetProperty("point").GetProperty("exam.servers").GetDouble())
                .Should().BeLessThanOrEqualTo(10);
        answer.GetProperty("summary").GetString().Should().Contain("10 points").And.Contain("without a run");

        RunReport.Render(record).Markdown.Should().Contain("`triage.servers + exam.servers <= 10`");
    }
}
