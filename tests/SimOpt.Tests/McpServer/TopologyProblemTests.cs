using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Optimization;
using SimOpt.Optimization.Interfaces;
using SimOpt.Optimization.Strategies.RandomSearch;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-68 step 1 (D1-D5, D8): <see cref="TopologyProblem"/> turns any MCP-built model into a problem
/// the strategies of SimOpt.Optimization can solve. A decision variable is a patch path; the genome is
/// a vector of reals, quantised when applied; every candidate is replicated at one base seed (common
/// random numbers), so candidates differ only where their patch differs; fitness is maximised, with a
/// minimised objective negated and a constraint breach ranked below every feasible point; a point once
/// evaluated is answered from a cache. The clinic question of the readiness review is the yardstick:
/// the fewest exam rooms that keep the mean exam wait under 15 minutes.
/// </summary>
public class TopologyProblemTests
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

    private static TopologyDefinition ClinicTopology() =>
        JsonSerializer.Deserialize<TopologyDefinition>(Clinic, McpJsonUtilities.DefaultOptions)!;

    private static DecisionVariable Rooms(double min = 4, double max = 12) =>
        new DecisionVariable { Path = "exam.servers", Type = "integer", Min = min, Max = max };

    private static ObjectiveDefinition FewestRoomsWaitUnder15() => new ObjectiveDefinition
    {
        Metric = "exam.servers",
        Direction = "min",
        Constraints = { new ConstraintDefinition { Metric = "wait_exam", Op = "<", Value = 15 } },
    };

    private static TopologyProblem Problem(IReadOnlyList<DecisionVariable>? variables = null, ObjectiveDefinition? objective = null,
                                           int? budget = null) =>
        new TopologyProblem(ClinicTopology(), variables ?? new[] { Rooms() }, objective ?? FewestRoomsWaitUnder15(),
                            duration: 540, warmup: 60, replications: 5, replicationSeed: 7, budget: budget);

    private static TopologySolution At(TopologyProblem problem, params double[] point)
    {
        var solution = (TopologySolution)problem.GenerateCandidates(1, 1).Single();
        solution.Parameters = point;
        return solution;
    }

    [Fact]
    public void RandomSearch_FindsTheFewestRoomsKeepingTheWaitUnder15_Seven()
    {
        TopologyProblem problem = Problem();
        var strategy = new RandomStrategy();
        strategy.Initialize(TestConfiguration.CreateRandom(seed: 3, iterations: 60));

        var best = (TopologySolution)strategy.Solve(problem).Single();

        problem.Quantise(best.Parameters).Should().Equal(7);
        CandidateEvaluation seven = problem.Evaluations.Single(e => e.Point[0] == 7);
        seven.Feasible.Should().BeTrue();
        seven.Metrics["wait_exam"].Mean.Should().BeLessThan(15);
        problem.Evaluations.Single(e => e.Point[0] == 6).Feasible.Should().BeFalse("six rooms are a load of 1.0");
        problem.Evaluations.Should().HaveCountLessThanOrEqualTo(9, "a point is evaluated once, then answered from the cache");
    }

    [Fact]
    public void APoint_IsQuantisedWhenApplied_IntegersRounded_StepsSnapped_BoundsKept()
    {
        TopologyProblem problem = Problem(new[]
        {
            Rooms(),
            new DecisionVariable { Path = "arrivals.arrival.mean", Type = "real", Min = 2.5, Max = 10, Step = 0.5 },
            new DecisionVariable { Path = "triage.servers", Type = "integer", Min = 1, Max = 6 },
        });

        problem.Quantise(new[] { 6.6, 4.74, 0.2 }).Should().Equal(7, 4.5, 1);
        problem.Quantise(new[] { 99, 1.0, 5.5 }).Should().Equal(12, 2.5, 6);
    }

    [Fact]
    public void ThePointEvaluatedTwice_IsAnsweredFromTheCache_WithTheSameFitness()
    {
        TopologyProblem problem = Problem();
        TopologySolution a = At(problem, 7.2), b = At(problem, 6.9);

        problem.Evaluate(a);
        problem.Evaluate(b);

        b.Fitness.Should().Be(a.Fitness);
        problem.Evaluations.Should().ContainSingle();
    }

    [Fact]
    public void AMinimisedObjective_IsNegated_AndABreachRanksBelowEveryFeasiblePoint_TheNearerHigher()
    {
        TopologyProblem problem = Problem();
        TopologySolution five = At(problem, 5), six = At(problem, 6), seven = At(problem, 7), nine = At(problem, 9);
        foreach (TopologySolution s in new[] { five, six, seven, nine }) problem.Evaluate(s);

        seven.Fitness.Should().Be(-7);
        nine.Fitness.Should().Be(-9);
        six.Fitness.Should().BeLessThan(nine.Fitness);
        five.Fitness.Should().BeLessThan(six.Fitness, "five rooms break the wait bound by more than six do");
    }

    [Fact]
    public void AMaximisedMetric_IsTheFitnessAsIs()
    {
        TopologyProblem problem = Problem(objective: new ObjectiveDefinition { Metric = "util_exam", Direction = "max" });
        TopologySolution eight = At(problem, 8);

        problem.Evaluate(eight);

        eight.Fitness.Should().BeApproximately(0.75, 0.08);
        eight.Fitness.Should().Be(problem.Evaluations.Single().Metrics["util_exam"].Mean);
    }

    [Fact]
    public void ARealVariable_OnADistributionField_ChangesTheModel()
    {
        TopologyProblem problem = Problem(new[] { new DecisionVariable { Path = "arrivals.arrival.mean", Type = "real", Min = 2.5, Max = 10 } },
                                          new ObjectiveDefinition { Metric = "util_exam", Direction = "max" });
        TopologySolution busy = At(problem, 4), quiet = At(problem, 10);

        problem.Evaluate(busy);
        problem.Evaluate(quiet);

        busy.Fitness.Should().BeApproximately(30d / 4 / 8, 0.08);
        quiet.Fitness.Should().BeApproximately(30d / 10 / 8, 0.05);
    }

    [Fact]
    public void AnImpossibleDefinition_IsRefused_ListingEveryProblem()
    {
        Action define = () => Problem(
            new[]
            {
                new DecisionVariable { Path = "ward.servers", Type = "integer", Min = 1, Max = 3 },
                new DecisionVariable { Path = "exam.servers", Type = "integer", Min = 9, Max = 4 },
                new DecisionVariable { Path = "triage.servers", Type = "count", Min = 1, Max = 3 },
            },
            new ObjectiveDefinition
            {
                Metric = "happiness",
                Direction = "up",
                Constraints = { new ConstraintDefinition { Metric = "wait_exam", Op = "~", Value = 1 } },
            });

        define.Should().Throw<ArgumentException>().Where(e =>
            e.Message.Contains("ward") && e.Message.Contains("min 9") && e.Message.Contains("count") &&
            e.Message.Contains("happiness") && e.Message.Contains("up") && e.Message.Contains("~"));
    }

    [Fact]
    public void Tweaking_IsSeeded_AndStaysInsideTheBounds()
    {
        TopologyProblem problem = Problem(new[] { Rooms(), new DecisionVariable { Path = "triage.servers", Type = "integer", Min = 1, Max = 6 } });
        var a = (TopologySolution)problem.GenerateCandidates(11, 1).Single();
        var b = (TopologySolution)a.Clone();

        for (int i = 0; i < 200; i++) { a.Tweak(); b.Tweak(); }

        b.Parameters.Should().Equal(a.Parameters, "a clone carries its generator's state");
        a.Parameters[0].Should().BeInRange(4, 12);
        a.Parameters[1].Should().BeInRange(1, 6);
        ((TopologySolution)problem.GenerateCandidates(11, 1).Single()).Parameters
            .Should().Equal(((TopologySolution)problem.GenerateCandidates(11, 1).Single()).Parameters);
    }

    [Fact]
    public void ADiscreteSpace_IsCounted_AndEnumeratedPointByPoint()
    {
        TopologyProblem problem = Problem(new[]
        {
            Rooms(),
            new DecisionVariable { Path = "arrivals.arrival.mean", Type = "real", Min = 4, Max = 5, Step = 0.5 },
        });

        problem.SpaceSize.Should().Be(27, "9 room counts times 3 interarrival means");
        problem.Grid().Select(p => (p[0], p[1])).Should().HaveCount(27).And.OnlyHaveUniqueItems()
               .And.Contain((4d, 4d)).And.Contain((12d, 5d));
    }

    [Fact]
    public void ASpaceWithAContinuousVariable_HasNoSize()
    {
        Problem(new[] { new DecisionVariable { Path = "arrivals.arrival.mean", Type = "real", Min = 4, Max = 5 } },
                new ObjectiveDefinition { Metric = "util_exam", Direction = "max" })
            .SpaceSize.Should().BeNull();
    }

    [Fact]
    public void EnumeratingTheClinicsNineRoomCounts_FindsSeven_InNineEvaluations()
    {
        TopologyProblem problem = Problem();

        foreach (double[] point in problem.Grid()) problem.Evaluate(At(problem, point));

        problem.Evaluations.Should().HaveCount(9);
        problem.Evaluations.Where(e => e.Feasible).MaxBy(e => e.Fitness)!.Point.Should().Equal(7);
    }

    [Fact]
    public void TheBudget_StopsEvaluating_AndSaysSo()
    {
        TopologyProblem problem = Problem(budget: 2);
        bool exhausted = false;
        problem.BudgetExhausted += () => exhausted = true;

        foreach (double rooms in new[] { 7d, 8d, 7d, 9d }) problem.Evaluate(At(problem, rooms));

        problem.Evaluations.Select(e => e.Point[0]).Should().Equal(7, 8);
        exhausted.Should().BeTrue();
    }
}
