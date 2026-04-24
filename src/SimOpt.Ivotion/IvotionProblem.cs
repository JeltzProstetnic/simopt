using System;
using System.Collections.Generic;
using SimOpt.Optimization.Interfaces;

namespace SimOpt.Ivotion;

/// <summary>
/// Optimization objective selector. Fitness is always maximized internally;
/// "minimize" objectives are returned as their negation so that higher fitness
/// remains the conventional "better" signal.
/// </summary>
public enum IvotionObjective
{
    MaximizeThroughput,
    MinimizeTotalCost,
    MinimizeCostPerPiece,
    MinimizeLaborHours,
    MinimizeFloorSpace,
}

/// <summary>
/// IProblem wrapping the headless Ivotion simulation. Evaluate() builds a
/// topology from the candidate solution, runs it for SimDurationMinutes, and
/// extracts KPIs to compute the fitness of the chosen objective.
/// </summary>
public sealed class IvotionProblem : IProblem
{
    public IvotionObjective Objective { get; set; } = IvotionObjective.MaximizeThroughput;
    public double SimDurationMinutes { get; set; } = 120.0;
    public int Seed { get; set; } = 42;

    /// <summary>
    /// Optional override for inter-arrival time (minutes). When null the
    /// builder uses its baseline value.
    /// </summary>
    public double? ArrivalIntervalMinutes { get; set; }

    /// <summary>
    /// No closed-form optimum exists for this problem; return +∞ so strategies
    /// that use <see cref="IProblem.OptimumFitness"/> as an early-termination
    /// bound keep searching.
    /// </summary>
    public double OptimumFitness => double.MaxValue;

    public bool IsValid(ISolution solutionCandidate)
    {
        if (solutionCandidate is not IvotionSolution iv) return false;
        return IvotionSolution.IsInRange(iv.Parameters);
    }

    public bool Evaluate(ISolution solutionCandidate)
    {
        ArgumentNullException.ThrowIfNull(solutionCandidate);

        if (solutionCandidate is not IvotionSolution iv)
        {
            solutionCandidate.Fitness = -double.MaxValue;
            solutionCandidate.HasFitness = true;
            return false;
        }

        var handles = IvotionTopologyBuilder.Build(iv, Seed, ArrivalIntervalMinutes);
        handles.RunFor(SimDurationMinutes);
        var kpis = IvotionKpis.Extract(handles, SimDurationMinutes);

        iv.Fitness = MapObjectiveToFitness(kpis, Objective);
        iv.HasFitness = true;
        return true;
    }

    public IEnumerable<ISolution> GenerateCandidates(int seed, int count)
    {
        var rng = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            var parameters = new int[IvotionSolution.DimensionCount];
            for (int d = 0; d < IvotionSolution.DimensionCount; d++)
            {
                int[] allowed = IvotionSolution.AllowedValues[d];
                parameters[d] = allowed[rng.Next(allowed.Length)];
            }
            yield return new IvotionSolution(parameters);
        }
    }

    private static double MapObjectiveToFitness(IvotionKpis kpis, IvotionObjective objective) =>
        objective switch
        {
            IvotionObjective.MaximizeThroughput => kpis.ThroughputPerHour,
            IvotionObjective.MinimizeTotalCost => -kpis.TotalCostPerHour,
            IvotionObjective.MinimizeCostPerPiece =>
                kpis.CostPerPiece == double.MaxValue ? -double.MaxValue : -kpis.CostPerPiece,
            IvotionObjective.MinimizeLaborHours => -kpis.LaborHoursPerSimHour,
            IvotionObjective.MinimizeFloorSpace => -kpis.FloorSpaceM2,
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unknown objective."),
        };
}
