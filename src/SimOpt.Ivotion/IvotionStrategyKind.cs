namespace SimOpt.Ivotion;

/// <summary>
/// Optimization strategy choice surfaced in the Ivotion UI. Kinds marked
/// as <see cref="IsEnabled"/> = false appear in the UI dropdown but are
/// greyed out until implemented.
/// </summary>
public enum IvotionStrategyKind
{
    Random,
    Evolutionary,
    ParticleSwarm,
    Sweep,
}

/// <summary>Static metadata about a strategy kind for UI rendering.</summary>
public static class IvotionStrategyInfo
{
    public static string DisplayName(IvotionStrategyKind kind) => kind switch
    {
        IvotionStrategyKind.Random => "Random Search",
        IvotionStrategyKind.Evolutionary => "Evolutionary Algorithm",
        IvotionStrategyKind.ParticleSwarm => "Particle Swarm (coming soon)",
        IvotionStrategyKind.Sweep => "Sweep / Exhaustive (later)",
        _ => kind.ToString(),
    };

    public static bool IsEnabled(IvotionStrategyKind kind) => kind switch
    {
        IvotionStrategyKind.Random => true,
        IvotionStrategyKind.Evolutionary => true,
        _ => false,
    };
}
