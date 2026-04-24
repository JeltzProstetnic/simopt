namespace SimOpt.Ivotion;

/// <summary>
/// Settings bundle driving a single optimization run.
/// Populated by the UI view-model, consumed by <see cref="IIvotionOptimizationEngine"/>.
/// </summary>
public sealed record IvotionOptimizationSettings
{
    public IvotionObjective Objective { get; init; } = IvotionObjective.MaximizeThroughput;
    public IvotionStrategyKind Strategy { get; init; } = IvotionStrategyKind.Evolutionary;

    /// <summary>Iterations for Random; generations for EA.</summary>
    public int Iterations { get; init; } = 30;

    /// <summary>Population size for EA (ignored by Random).</summary>
    public int PopulationSize { get; init; } = 12;

    public double OperatorWagePerHour { get; init; } = IvotionCostModel.OperatorWagePerHour;
    public double SimDurationMinutes { get; init; } = 120.0;
    public int Seed { get; init; } = 42;
}
