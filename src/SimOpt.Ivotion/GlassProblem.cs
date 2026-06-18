using System;
using System.Collections.Generic;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Glass;

/// <summary>
/// Optimization objective selector. Fitness is always maximized internally;
/// "minimize" objectives are returned as their negation so that higher fitness
/// remains the conventional "better" signal.
/// </summary>
public enum GlassObjective
{
    MaximizeThroughput,
    MinimizeTotalCost,
    MinimizeCostPerPiece,
    MinimizeLaborHours,
}

/// <summary>
/// IProblem wrapping the headless glass production-line simulation. Evaluate()
/// builds a topology from the candidate solution, runs it for
/// SimDurationMinutes, and extracts KPIs to compute the fitness of the chosen
/// objective.
/// </summary>
public sealed class GlassProblem : IProblem
{
    public GlassObjective Objective { get; set; } = GlassObjective.MaximizeThroughput;
    public double SimDurationMinutes { get; set; } = 480.0;
    public int Seed { get; set; } = 42;

    /// <summary>
    /// Optional override for inter-arrival time (minutes). When null the
    /// builder uses its baseline value.
    /// </summary>
    public double? ArrivalIntervalMinutes { get; set; }

    /// <summary>
    /// Operator wage (currency units/hour) used for cost-side KPIs. Defaults to
    /// the demo cost model constant; UI overrides for sensitivity analysis.
    /// </summary>
    public double OperatorWagePerHour { get; set; } = GlassCostModel.OperatorWagePerHour;

    /// <summary>
    /// No closed-form optimum exists for this problem; return +∞ so strategies
    /// that use <see cref="IProblem.OptimumFitness"/> as an early-termination
    /// bound keep searching.
    /// </summary>
    public double OptimumFitness => double.MaxValue;

    public bool IsValid(ISolution solutionCandidate)
    {
        if (solutionCandidate is not GlassSolution gl) return false;
        return GlassSolution.IsInRange(gl.Parameters);
    }

    public bool Evaluate(ISolution solutionCandidate)
    {
        ArgumentNullException.ThrowIfNull(solutionCandidate);

        if (solutionCandidate is not GlassSolution gl)
        {
            solutionCandidate.Fitness = -double.MaxValue;
            solutionCandidate.HasFitness = true;
            return false;
        }

        var handles = GlassTopologyBuilder.Build(gl, Seed, ArrivalIntervalMinutes);
        handles.RunFor(SimDurationMinutes);
        var kpis = GlassKpis.Extract(handles, SimDurationMinutes, OperatorWagePerHour);

        gl.Fitness = MapObjectiveToFitness(kpis, Objective);
        gl.HasFitness = true;
        return true;
    }

    public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
    {
        var rng = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            var parameters = new int[GlassSolution.DimensionCount];
            for (int d = 0; d < GlassSolution.DimensionCount; d++)
            {
                int[] allowed = GlassSolution.AllowedValues[d];
                parameters[d] = allowed[rng.Next(allowed.Length)];
            }
            yield return new GlassSolution(parameters);
        }
    }

    private static double MapObjectiveToFitness(GlassKpis kpis, GlassObjective objective) =>
        objective switch
        {
            GlassObjective.MaximizeThroughput => kpis.ThroughputPerHour,
            GlassObjective.MinimizeTotalCost => -kpis.TotalCostPerHour,
            GlassObjective.MinimizeCostPerPiece =>
                kpis.CostPerPiece == double.MaxValue ? -double.MaxValue : -kpis.CostPerPiece,
            GlassObjective.MinimizeLaborHours => -kpis.LaborHoursPerSimHour,
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unknown objective."),
        };
}
