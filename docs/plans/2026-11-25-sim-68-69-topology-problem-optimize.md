# SIM-68 + SIM-69: `TopologyProblem` and the `optimize` tool

**Status:** steps 1-3 done 2026-11-25 (night, unattended, cfg caretaking); step 4 open.
**Serves:** UN-015 (automated configuration search), UN-012 (every evaluated point is replicated),
UN-033/034 (a search is reproducible from its stored inputs).
**Builds on:** SIM-66 (patch paths, `TopologyPatcher`, `ExperimentRunner`), SIM-63 (`ReplicationRunner`).
Tool design: `docs/commercial/2026-08-25-architecture-readiness-review.md` §C, "The missing keystone component".
**Regression baseline:** 1272 passed, 1 skipped.

## What exists and what is missing

The optimisation layer (`SimOpt.Optimization`) has four strategies (random search, EA, SA, PSO) over
`IProblem`/`ISolution`, and exactly one real problem, `IvotionProblem`, written by hand for one model.
Nothing connects an MCP-built model to a strategy, so the "opt" in SimOpt is absent from the MCP surface.
PSO reads a solution's vector through reflection on a property named `Parameters`, which only
`TestSolution` has, so it searches nothing on a real problem (SIM-59).

## Decisions

**D1. A decision variable is a patch path.** `{path, type: "integer" | "real", min, max, step?}`, where
`path` is SIM-66's set path (`"exam.servers"`, `"arrivals.arrival.mean"`). Applying a candidate is a
`set` patch per variable through `TopologyPatcher`, so one mechanism changes models for `patch_model`,
`run_experiment` scenarios and the optimiser.

**D2. The genome is a vector of reals; the evaluated point is quantised.** Each coordinate lives in
`[min, max]`. An integer variable is rounded, and a `step` snaps to `min + k × step`, when the candidate
is applied. PSO and SA move continuously; what is evaluated, cached and reported is the quantised point.

**D3. Every candidate is replicated at the same base seed (common random numbers).** Streams are keyed
by node id, so two candidates differ only where their patch differs. Comparing candidates then compares
the configurations, not their luck, which is what lets a few replications per evaluation rank them.

**D4. Fitness is maximised; direction and constraints map onto it.** The objective is a metric id or a
variable path (the clinic's "fewest rooms" minimises `exam.servers` itself). `direction: "min"`
negates it. A constraint `{metric, op: "<" | "<=" | ">" | ">=", value}` is kept only when the metric's
whole 95 % interval keeps it (the mean when one replication gives no interval). Measured on the clinic,
mean exam wait under 15 minutes, 8-hour day after 1 hour of warm-up: on the mean, 5 replications passed
6 rooms (12.3 ± 8.0) that 20 replications failed (19.7 ± 6.6), so the answer flipped with the noise; on
the interval both give 7 rooms (6 rooms: up to 20.3). In steady state (10,000 minutes) 6 rooms wait 100
minutes and 7 rooms 8.7, which the M/G/c estimate of about 8 matches. A candidate that breaks a bound ranks below every feasible candidate: its fitness is
`-1e12` minus its total violation relative to each bound, so among infeasible candidates the nearer
one ranks higher and the search is pulled toward feasibility. A candidate whose patched model does not
build gets `-1e15`.

**D5. Evaluations are cached by the quantised point.** Under D3 the same point always gives the same
figures, so a strategy that proposes it again is answered from the cache and does not spend budget.

**D6. A small discrete space is enumerated, not searched.** `strategy: "auto"` evaluates every point
when the quantised space has no more points than the evaluation budget (the clinic's 4 to 12 rooms is
nine points), which gives the exact answer under D3 in the fewest evaluations. Otherwise "auto" uses random
search over the quantised space until SIM-59's SA defects are fixed (an unguarded `while (T > 0)`), then
simulated annealing. `random` and `grid` can be named from step 3; `sa`, `ea` and `pso` after step 4.

**D7. A uniform vector contract replaces PSO's reflection.** `IVectorSolution { double[] Parameters }`.
`TopologySolution` implements it, PSO reads it through the interface, and `TestSolution` implements it
too, which is SIM-59's PSO part.

**D8. Solution operators are seeded.** `TopologySolution`'s `Tweak` (EA mutation, SA Brownian move) and
`CombineWith` (EA crossover) draw from a small generator whose state travels with the solution when it
is cloned, seeded from the search seed and the candidate's number. Two runs with the same seed take the
same path, which SIM-59 found broken for the Ivotion operators (wall-clock `new Random()`).

**D9. The `optimize` tool.** Arguments: `model_id`, `variables`, `objective` `{metric, direction,
constraints?}`, `budget` `{evaluations?, seconds?}` (default 50 evaluations), `duration`, `warmup?`,
`replications_per_eval` (default 5), `strategy` (default "auto"), `seed?`, `version?`. Returns the best
point with its metrics and intervals, the top 5 feasible points, evaluations used, the strategy run,
and provenance. The best point can be confirmed with `run_experiment` at more replications; the result
says so.

## Steps (test-first, a commit each)

1. `TopologyProblem` + `TopologySolution` (D1-D5, D8): variables, quantisation, CRN evaluation,
   fitness with constraints, cache; driven by `RandomStrategy` in the tests.
2. Exhaustive enumeration for small discrete spaces (D6).
3. `optimize` tool (D9). **Acceptance:** the clinic, "fewest exam rooms keeping the mean exam wait
   under 15 minutes", `exam.servers` from 4 to 12: one call returns 7 rooms (hand estimate: 6 rooms
   are a load of 1.0, 7 rooms about 8 minutes of wait).
4. `IVectorSolution` and PSO through it (D7); SA and EA through the seeded operators, checked on a
   problem whose optimum is known.

## Outside this item

SIM-59's EA cloning and SA temperature defects (they are filed there and matter as soon as "ea" or
"sa" is chosen; step 4 checks what the tool exposes), multi-objective search, a choice between the
interval and the mean for constraints, and a stored search record (UN-036).

## Progress and measured numbers

| step | commit | suite (passed, 1 skipped) |
|---|---|---|
| 1 `TopologyProblem`, `TopologySolution` | `228bcc5` | 1272 -> 1281 |
| 2 space size, grid | `28fb063` | 1284 |
| 3 `optimize` (SIM-69) | this commit | 1290 |

The clinic acceptance, measured: `optimize` with `exam.servers` from 4 to 12, minimise it, `wait_exam < 15`,
540 minutes after 60 of warm-up, 5 replications per point: strategy grid, 9 evaluations, 71 ms, 922
characters; best 7 rooms, wait 3.18 [0.92, 5.43], utilisation 0.815, then 8, 9, 10 and 11 rooms. The time
limit evaluates at least one point, so a search always has a best point to report.
