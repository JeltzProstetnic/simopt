# SIM-68 + SIM-69: `TopologyProblem` and the `optimize` tool

**Status:** done. Steps 1-3 2026-11-25 (night, unattended, cfg caretaking); step 4 with SIM-59 the same
night, after restart #4. D6 and D8 were revised by step 4's measurements (marked below).
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
**Revised by step 4:** "auto" runs the EA, not SA, when the space is larger than the budget or continuous.
Measured with the replications fixed and 15 search seeds (table under *Progress*), SA came last on every
larger space, and the EA was best or tied at the default budget with the best worst case. Unattended
choice; MG may prefer another default (Q-AUTO in the handover).

**D7. A uniform vector contract replaces PSO's reflection.** `IVectorSolution { double[] Parameters }`.
`TopologySolution` implements it, PSO reads it through the interface, and `TestSolution` implements it
too, which is SIM-59's PSO part.

**D8. Solution operators are seeded.** `TopologySolution`'s `Tweak` (EA mutation, SA Brownian move) and
`CombineWith` (EA crossover) draw from a small generator whose state travels with the solution when it
is cloned, seeded from the search seed and the candidate's number. Two runs with the same seed take the
same path, which SIM-59 found broken for the Ivotion operators (wall-clock `new Random()`).
**Revised by step 4:** a clone *splits* the generator (`SplitMix64.Split`, seeded from the original's next
draw) instead of copying its state. SA proposes every move on a fresh clone of its current point, so with
a copied state a rejected move was proposed again from the unchanged point: a cold run made 2 distinct
proposals in 300. Runs stay reproducible from their seed.

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
| 3 `optimize` (SIM-69) | `f128c0f` | 1290 |
| 4a `IVectorSolution`, PSO through it | `3f16519` | 1292 |
| SIM-59 SA ends under any schedule | `8ac899f` | 1297 |
| SIM-59 EA breeds from the best, mutates copies | `97e3478` | 1300 |
| SIM-59 seeded operators (`SplitMix64`) | `51039d8` | 1309 |
| 4b `sa`, `ea`, `pso` in `optimize`; clones split | `cf8fba5` | 1322 |
| 4c search seed apart from the replications' | `000b49b` | 1325 |
| 4d "auto" runs the EA on large spaces | this commit | 1325 |

The clinic acceptance, measured: `optimize` with `exam.servers` from 4 to 12, minimise it, `wait_exam < 15`,
540 minutes after 60 of warm-up, 5 replications per point: strategy grid, 9 evaluations, 71 ms, 922
characters; best 7 rooms, wait 3.18 [0.92, 5.43], utilisation 0.815, then 8, 9, 10 and 11 rooms. The time
limit evaluates at least one point, so a search always has a best point to report.

### Step 4: what SIM-59 changed, and which strategy "auto" should run

SIM-59's defects, each found failing by a test first:

- **PSO searched nothing on a real problem.** It read a property named `Parameters` through reflection,
  which only the test helper had, and otherwise treated the fitness as a one-dimensional position. It now
  reads `IVectorSolution` and refuses a candidate without a vector.
- **SA never ended under a geometric schedule.** It looped while `T >= 0`, which `T * a` never leaves, and
  `Stop()`, which set T to 0, could not end such a run either. It now ends at T = 0, at
  `NumberOfIterations` (ignored before) or on `Stop`, which a schedule cannot undo.
- **The EA bred from the worst.** The generation was sorted by `ISolution.CompareTo` (fitness ascending)
  while the default parent selector takes the first µ. On the sphere the population fell to -43 against an
  optimum of 0; it now reaches -0.05. Mutation now changes a copy, the best is kept as a copy, and
  `Expand`'s fillers are considered for the best (they were not). The Ivotion EA path uses this default,
  so its searches change.
- **The operators were not seeded** (Ivotion's `Tweak` and `CombineWith` used a wall-clock `Random`).

The strategies, measured: the clinic topology, replications fixed at seed 7 (5 per point, 540 minutes
after 60 of warm-up), search seeds 1 to 15, `Optimizer.Run(..., seed: 7, searchSeed: s)`. "Optimum" counts
the runs whose best point equals the grid's, the exact optimum under common random numbers; the mean is of
the best objective found. Harness and raw output: `docs/plans/2026-11-25-sim-68-step4/`.

| space (points) | budget | random | sa | ea | pso |
|---|---|---|---|---|---|
| P1 fewest exam rooms, wait < 15; triage 1-6 x exam 4-12 (54) | 20 | 4/15 | 7/15 | 4/15 | 7/15 |
| | 40 | 10/15 | 15/15 (21 evaluations) | 15/15 (30) | 10/15 |
| P2 max exam utilisation, wait < 5; triage 1-10 x exam 4-20 (170) | 20 | 0/15, 0.713 | 1/15, 0.623 | 0/15, 0.712 | 2/15, 0.732 |
| | 40 | 1/15, 0.743 | 4/15, 0.764 | 9/15, 0.800 | 6/15, 0.776 |
| | 50 | 2/15, 0.751 | 3/15, 0.785 | **12/15, 0.824** | 10/15, 0.815 |
| P3 as P2; exam 4-12 x arrival mean 3-10 step 0.25 (261) | 20 | 1/15, 0.775 | 1/15, 0.733 | 1/15, 0.768 | 2/15, 0.776 |
| | 40 | 4/15, 0.828 | 2/15, 0.770 | 3/15, 0.821 | 3/15, 0.809 |
| P4 as P3, arrival mean continuous | 50 | mean 0.831, worst 0.736 | 0.771, 0.681 | **0.836, 0.792** | 0.833, 0.766 |

SA, D6's planned default, has the worst runs on every larger space (worst 0.38 on P2 at 20): one
trajectory of one-step moves covers little ground in 20 to 50 evaluations. The EA is best or tied at the
default budget of 50 and has the best worst case; at 20 evaluations nothing beats random by much, because
the EA's first generation of 15 is random. Hence "auto" runs the EA (D6, revised). Two observations: the
grid's best for P1 and P2 is one triage server, whose overload throttles arrivals to the exam rooms, which
is a true optimum of the model as stated; UN-015's headcount limits would rule it out, but a constraint can
only be put on a metric, not on the variables (SIM-140); and the measurement first ran with the search seed equal to the replications' seed, which
evaluated each run's points under different noise and let runs "beat" the grid, hence `searchSeed`.
