using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimOpt.Ivotion;

/// <summary>
/// Runs an optimization over <see cref="IvotionProblem"/> using the strategy
/// selected in <see cref="IvotionOptimizationSettings.Strategy"/>. Abstracted
/// so the UI view-model can be tested without spinning up real simulations.
/// </summary>
public interface IIvotionOptimizationEngine
{
    Task<IvotionOptimizationResult> RunAsync(
        IvotionOptimizationSettings settings,
        IProgress<IvotionFitnessSample>? progress,
        CancellationToken ct);
}
