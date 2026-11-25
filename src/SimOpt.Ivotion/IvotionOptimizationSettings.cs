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
    /// <summary>
    /// SIM-93 (MG decision Q-IVO-STATS): two shifts, scored after <see cref="WarmupMinutes"/>. The baseline ships a
    /// batch about every 83 minutes, so a short window scores whole batches: 120 minutes from an empty line
    /// scored 7.5 pieces/h, this window 10.71 at every one of 20 seeds.
    /// </summary>
    public double SimDurationMinutes { get; init; } = 960.0;

    /// <summary>SIM-93: the start of each run that throughput leaves out (the line fills up); shorter than the run.</summary>
    public double WarmupMinutes { get; init; } = 120.0;
    public int Seed { get; init; } = 42;
}
