namespace SimOpt.Ivotion;

/// <summary>
/// A single progress sample emitted during a run. Iteration is 1-based.
/// BestSoFarFitness is monotone (non-decreasing) across samples since
/// fitness is always maximized internally by <see cref="IvotionProblem"/>.
/// </summary>
public readonly record struct IvotionFitnessSample(
    int Iteration,
    double BestSoFarFitness,
    IvotionSolution BestSoFarSolution);
