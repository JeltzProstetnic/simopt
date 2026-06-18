namespace SimOpt.Glass;

/// <summary>
/// Terminal result of an optimization run. BestSolution and BestKpis are null
/// only when the run completed zero successful evaluations (e.g. cancelled
/// before the first evaluation).
/// </summary>
public sealed record GlassOptimizationResult(
    GlassSolution? BestSolution,
    GlassKpis? BestKpis,
    int TotalIterations,
    long ElapsedMilliseconds,
    bool WasCancelled);
