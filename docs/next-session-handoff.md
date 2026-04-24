Action: act

# Next Session — SIM-36 (Particle Swarm Optimization)

Tracked-by: SIM-36

## Starting point

Phase A (SIM-35) and Phase B UI (SIM-37) are shipped. Ivotion runs end-to-end
with Random + EA. PSO is the next strategy in the agile order.

The current `src/SimOpt.Optimization/Strategies/ParticleSwarm/` directory holds
a scaffold (`ParticleSwarmOptimization`, `SwarmingAlgorithm`,
`IParticleSwarmConfiguration`) — stub, not a working algorithm.

## Plan

1. **Implement the algorithm**
   - Particle state: position vector, velocity vector, personal best (position + fitness), global best (shared).
   - Update rule: `v_{t+1} = ω·v_t + c1·r1·(pbest − x_t) + c2·r2·(gbest − x_t)`.
   - Inertia weight ω, cognitive c1, social c2 come from `IParticleSwarmConfiguration`.
   - Boundary handling: clamp-and-reflect or absorbing walls — pick one and document.
   - Discrete-parameter projection (needed for `IvotionSolution`): map continuous particle
     to nearest allowed value per dimension via `IvotionSolution.AllowedValues[d]`.

2. **Benchmark validation** (TDD)
   - Test problems: Sphere (unimodal) and Rosenbrock (non-convex valley).
   - Assert convergence within tolerance after N iterations with fixed seed.
   - Reuse the `TestProblem`/`TestSolution` helpers in `tests/SimOpt.Tests/Helpers/`.

3. **Wire into IvotionOptimizationEngine**
   - Add `IvotionStrategyKind.ParticleSwarm` branch to the `Run` switch in
     `src/SimOpt.Ivotion/IvotionOptimizationEngine.cs`.
   - Flip `IvotionStrategyInfo.IsEnabled(ParticleSwarm) => true` (IvotionStrategyKind.cs).
   - Drop the `if (Strategy is ParticleSwarm or Sweep) throw …` guard's ParticleSwarm arm.
   - Remove test `RunAsync_ParticleSwarm_Throws` (rename or replace with a
     happy-path test: `RunAsync_ParticleSwarm_ReturnsBestSolution`).
   - Update display name in `IvotionStrategyInfo.DisplayName` — drop "(coming soon)".

4. **Progress reporting**
   - Current pattern: emit `IvotionFitnessSample` per generation/iteration with
     best-so-far. PSO should emit once per swarm update cycle.

## Follow-up test (15-min, separate commit)

`VizTopology.IvotionPacking(IvotionSolution)` shipped untested. Add
`tests/SimOpt.Tests/Visualization/VizTopologyIvotionPackingTests.cs`:
- 1-Roland solution → nodes contains `"roland"` id, connections has buf2→roland and roland→buf3.
- 2-Roland solution → nodes contains both `"roland_a"` and `"roland_b"`; buf2 splits to both; both merge to buf3.
- Node labels reflect operator counts and batch size.

## Context references

- Build order and locked-in decisions: `docs/pending-ivotion-optimization.md` (reference)
- SIM-35 Phase A summary: backlog.md, commit 8371b57
- SIM-37 Phase B summary: this-session-context below it and session-history after rotation

## Baseline

After shutdown:
- 615 tests passing / 1 skipped / 0 failing
- Full solution builds clean (0 errors, 2 pre-existing warnings)
- `git log --oneline -1` should be the SIM-37 commit
