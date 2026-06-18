namespace SimOpt.Glass;

/// <summary>
/// A single progress sample emitted during a run. Iteration is 1-based.
/// BestSoFarFitness is monotone (non-decreasing) across samples since
/// fitness is always maximized internally by <see cref="GlassProblem"/>.
/// </summary>
public readonly record struct GlassFitnessSample(
    int Iteration,
    double BestSoFarFitness,
    GlassSolution BestSoFarSolution);
