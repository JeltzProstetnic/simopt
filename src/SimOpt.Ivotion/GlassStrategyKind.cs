namespace SimOpt.Glass;

/// <summary>
/// Optimization strategy choice surfaced in the Glass UI. Kinds marked
/// as <see cref="GlassStrategyInfo.IsEnabled"/> = false appear in the UI
/// dropdown but are greyed out until implemented.
/// </summary>
public enum GlassStrategyKind
{
    Random,
    Evolutionary,
    ParticleSwarm,
    Sweep,
}

/// <summary>Static metadata about a strategy kind for UI rendering.</summary>
public static class GlassStrategyInfo
{
    public static string DisplayName(GlassStrategyKind kind) => kind switch
    {
        GlassStrategyKind.Random => "Random Search",
        GlassStrategyKind.Evolutionary => "Evolutionary Algorithm",
        GlassStrategyKind.ParticleSwarm => "Particle Swarm (coming soon)",
        GlassStrategyKind.Sweep => "Sweep / Exhaustive (later)",
        _ => kind.ToString(),
    };

    public static bool IsEnabled(GlassStrategyKind kind) => kind switch
    {
        GlassStrategyKind.Random => true,
        GlassStrategyKind.Evolutionary => true,
        _ => false,
    };
}
