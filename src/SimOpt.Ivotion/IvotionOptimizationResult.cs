namespace SimOpt.Ivotion;

/// <summary>
/// Terminal result of an optimization run. BestSolution and BestKpis are null
/// only when the run completed zero successful evaluations (e.g. cancelled
/// before the first evaluation).
/// </summary>
public sealed record IvotionOptimizationResult(
    IvotionSolution? BestSolution,
    IvotionKpis? BestKpis,
    int TotalIterations,
    long ElapsedMilliseconds,
    bool WasCancelled);
