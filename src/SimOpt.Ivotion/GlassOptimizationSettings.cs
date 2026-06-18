namespace SimOpt.Glass;

/// <summary>
/// Settings bundle driving a single optimization run.
/// Populated by the UI view-model, consumed by <see cref="IGlassOptimizationEngine"/>.
/// </summary>
public sealed record GlassOptimizationSettings
{
    public GlassObjective Objective { get; init; } = GlassObjective.MaximizeThroughput;
    public GlassStrategyKind Strategy { get; init; } = GlassStrategyKind.Evolutionary;

    /// <summary>Iterations for Random; generations for EA.</summary>
    public int Iterations { get; init; } = 30;

    /// <summary>Population size for EA (ignored by Random).</summary>
    public int PopulationSize { get; init; } = 12;

    public double OperatorWagePerHour { get; init; } = GlassCostModel.OperatorWagePerHour;
    public double SimDurationMinutes { get; init; } = 480.0;
    public int Seed { get; init; } = 42;
}
