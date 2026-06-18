using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimOpt.Glass;

/// <summary>
/// Runs an optimization over <see cref="GlassProblem"/> using the strategy
/// selected in <see cref="GlassOptimizationSettings.Strategy"/>. Abstracted
/// so the UI view-model can be tested without spinning up real simulations.
/// </summary>
public interface IGlassOptimizationEngine
{
    Task<GlassOptimizationResult> RunAsync(
        GlassOptimizationSettings settings,
        IProgress<GlassFitnessSample>? progress,
        CancellationToken ct);
}
