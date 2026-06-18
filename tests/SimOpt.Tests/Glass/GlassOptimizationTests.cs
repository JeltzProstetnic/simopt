using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SimOpt.Glass;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Glass;

/// <summary>
/// Headless tests for the generic glass base-mass production-line optimization
/// pipeline: topology builder, problem evaluation, and a short evolutionary run.
/// </summary>
public class GlassOptimizationTests
{
    private readonly ITestOutputHelper _out;

    public GlassOptimizationTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// (a) The topology builds and a short headless run pushes items through to
    /// the Fertigmeldung sink.
    /// </summary>
    [Fact]
    public void TopologyBuilder_RunFor120_ProducesOutput()
    {
        var solution = new GlassSolution(new[] { 2, 2, 12, 2, 1 });

        var handles = GlassTopologyBuilder.Build(solution, seed: 42);
        handles.RunFor(120.0);

        _out.WriteLine($"Time: {handles.Model.CurrentTime:F2}, Sink count: {handles.DoneSink.Count}");

        handles.Mixers.Should().HaveCount(2);
        handles.ProcessingLines.Should().HaveCount(2);
        handles.DoneSink.Count.Should().BeGreaterThan(0, "items should reach the Fertigmeldung sink");
    }

    /// <summary>
    /// (b) Evaluate on a random candidate sets HasFitness and yields a finite
    /// fitness value.
    /// </summary>
    [Fact]
    public void Problem_Evaluate_SetsFiniteFitness()
    {
        var problem = new GlassProblem
        {
            Objective = GlassObjective.MaximizeThroughput,
            SimDurationMinutes = 120.0,
            Seed = 7,
        };

        var candidate = (GlassSolution)problem.GenerateCandidates(seed: 7, count: 1).First();
        bool valid = problem.Evaluate(candidate);

        _out.WriteLine($"Params: [{string.Join(",", candidate.Parameters)}], Fitness: {candidate.Fitness}");

        valid.Should().BeTrue();
        candidate.HasFitness.Should().BeTrue();
        double.IsFinite(candidate.Fitness).Should().BeTrue("fitness must be a finite number");
    }

    /// <summary>
    /// (c) A short evolutionary run returns a valid best solution with a finite
    /// fitness.
    /// </summary>
    [Fact]
    public async Task Engine_ShortEvolutionaryRun_ReturnsValidBest()
    {
        var engine = new GlassOptimizationEngine();
        var settings = new GlassOptimizationSettings
        {
            Strategy = GlassStrategyKind.Evolutionary,
            Objective = GlassObjective.MaximizeThroughput,
            Iterations = 2,
            PopulationSize = 6,
            SimDurationMinutes = 60.0,
            Seed = 21,
        };

        var result = await engine.RunAsync(settings, progress: null, CancellationToken.None);

        _out.WriteLine($"Iterations: {result.TotalIterations}, " +
                       $"BestFitness: {result.BestSolution?.Fitness}, " +
                       $"Throughput/hr: {result.BestKpis?.ThroughputPerHour}");

        result.BestSolution.Should().NotBeNull();
        result.BestKpis.Should().NotBeNull();
        GlassSolution.IsInRange(result.BestSolution!.Parameters)
            .Should().BeTrue("best solution must lie within the allowed decision space");
        double.IsFinite(result.BestSolution!.Fitness).Should().BeTrue("best fitness must be finite");
        result.WasCancelled.Should().BeFalse();
    }

    /// <summary>
    /// Sanity: decision variables clamp into the allowed value space.
    /// </summary>
    [Fact]
    public void Solution_OutOfRange_ClampsToAllowed()
    {
        var solution = new GlassSolution(new[] { 9, 9, 99, 9, 9 });

        GlassSolution.IsInRange(solution.Parameters).Should().BeTrue();
        solution.NumberOfMixers.Should().Be(3);
        solution.NumberOfProcessingLines.Should().Be(2);
        solution.IntermediateBufferCapacity.Should().Be(20);
        solution.OperatorsQuality.Should().Be(3);
        solution.OperatorsPacking.Should().Be(2);
    }
}
