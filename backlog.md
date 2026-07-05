# SimOpt Backlog

## P1 — High Priority

- [x] **SIM-01** Fix EventScheduler.Remove last event (size: S) — Phase 3
- [x] **SIM-02** Fix RandomStrategy same-seed-every-iteration (size: S) — Phase 3
- [x] **SIM-03** Fix Vector.Equals NaN Z comparison bug (size: S) — Phase 3
- [x] **SIM-04** Implement PSO (Particle Swarm Optimization) as IStrategy (size: L) — Phase 4: ParticleSwarmOptimization with IParticleSwarmConfiguration, 11 tests
- [x] **SIM-05** Fix EventScheduler.Remove duplicate-priority edge case (size: M) — Phase 4: clone Priority keys + reverse index for identity-based removal, 3 tests

## P2 — Medium Priority

- [x] **SIM-06** Expand test coverage: Buffer template (size: M) — Phase 4
- [x] **SIM-07** Expand test coverage: Server template (size: L) — Phase 4 (integration tests deferred: ConnectTo type constraints)
- [x] **SIM-08** Expand test coverage: Conveyor template (size: M) — Phase 4
- [x] **SIM-09** Expand test coverage: more distributions (size: M) — Phase 4: Gaussian, NegExponential, Constant
- [x] **SIM-10** Expand test coverage: graph/pathfinding algorithms (size: L) — Phase 4: Dijkstra, Floyd-Warshall, adjacency
- [x] **SIM-11** Expand test coverage: matrix decompositions (size: M) — Phase 4: Cholesky, LU, QR roundtrip
- [x] **SIM-12** Add XML doc comments to public APIs (size: L) — Phase 5: IStrategy, ISolution, IProblem, EventScheduler, Priority, PSO, templates

## P2.5 — Platform (new)

- [x] **SIM-18** Avalonia 2D visualization — SQSS demo with live rendering, entity animations, speed controls
- [x] **SIM-19** MCP server scaffold — create_model, run_simulation, get_status, list_templates tools
- [ ] **SIM-20** MCP server integration testing — verify tools work end-to-end with Claude Code
- [x] **SIM-21** Agent fleet knowledge file — Phase 5: simopt-ops.md with detection patterns, workflow, presets, constraints
- [x] **SIM-22** 3D visualization research — Phase 5: Raylib-cs for prototype, Stride for production
- [x] **SIM-23** Generalize visualization — Phase 5: VizTopology JSON, auto-layout, topology selector, 3 presets

## P3 — Low Priority / Future

- [ ] **SIM-13** Nullable annotation cleanup (size: XL) — ~1690 warnings, systematic conversion
- [x] **SIM-14** PCA/KPCA tests (size: M) — Phase 5: 47 tests (transform/revert, variance, kernels)
- [x] **SIM-15** ROC curve analysis tests (size: M) — Phase 5: 28 tests (AUC, thresholds, DeLong compare)
- [x] **SIM-16** Example project modernization (size: L) — Phase 5: SDK-style csproj, SimOpt.* namespaces, console apps
- [x] **SIM-24** Fix CholeskyDecomposition.Solve() forward substitution bug — Phase 5
- [x] **SIM-25** Fix EigenvalueDecomposition null guard (throws wrong exception) — Phase 5
- [ ] **SIM-17** NuGet packaging (size: M) — publish core libraries as packages (deprioritized)
- [x] **SIM-26** Factory showcase Phase B: professional rendering (gradients, shadows, conveyor animation, SCADA look) (size: L) — Phase B: SCADA palette, SimpleRejectServer, 5 connection patterns, 4 headless tests
- [x] **SIM-27** Factory showcase Phase C: detachable controls, multi-monitor, keyboard shortcuts (size: M)
- [ ] **SIM-28** Factory showcase Phase D: optimization showcase (before/after, fitness chart, sensitivity heatmap) (size: L)
- [ ] **SIM-29** Ivoclar dental manufacturing showcase (size: L)
- [ ] **SIM-30** Icon/graphics library for domain-specific visuals (size: L)
- [x] **SIM-31** Statistics panel + polish: utilization bars per station, throughput, WIP, bottleneck highlighting, detachable stats window, S-key toggle (size: L)
- [x] **SIM-32** Realistic factory floor renderer: toggle between schematic and realistic 2D view (size: XL) — concrete floor, metallic machines, rack buffers, dock bays, belt conveyors, iso entities, R-key toggle
- [ ] **SIM-33** Realistic renderer v2: organic factory layout (non-grid), realistic node sizes, building features (walls, doors, corridors, pillars), walkways between stations (size: L)
- [ ] **SIM-34** Human agent entity: mobile worker that walks between stations, repairs damaged machines, unsticks conveyors, pulls items from machines/belts. Extends simulation primitives with pathfinding + task queue (size: XL)

## Ivotion Optimization Showcase (Ivoclar follow-up demo, next week)

Plan: `docs/plans/ivotion-optimization-showcase.md`
Driver: colleague impressed by today's spontaneous Ivotion v1+v2 demo;
will bring real production data next week and wants to see optimization.

Build order (agile reorder 2026-04-24 — ship a vertical slice before polishing strategies):

1. **SIM-37** Phase B — UI panel (EA + Random selectable; PSO greyed out "coming")
2. **SIM-36** Phase A.2 — PSO full impl, unlocks the PSO dropdown entry
3. **SIM-38** Phase C — trade-off curve
4. **SIM-40..43** — replications, heatmap, constraints, ROI
5. **SIM-44** — RolandPrinter fan-out

- [x] **SIM-35** Phase A: IvotionProblem + IvotionSolution + multi-Roland topology builder + KPI extraction (throughput / cost / labor / space) + tests (size: L) — P1: new `SimOpt.Ivotion` library; 37 tests (solution/builder/KPIs/problem); 594 pass, 0 regressions
- [x] **SIM-37** Phase B: Optimization UI panel — objective selector, strategy selector (Random/EA/PSO/Sweep), live fitness chart via ScottPlot.Avalonia 5.1.58, operator-wage input, iterations/population sliders, run/stop, "Apply best to viz" (loads Ivotion preset paused, user presses Space) (size: L) — P1: engine (Random + EA), view-model, AXAML, MainWindow TabControl; 21 new tests (615 pass, 0 regressions). Known limitation: Apply-to-viz uses static Ivotion preset — parametric viz rebuild (roland_count / batch_size) deferred to Phase D/SIM-44
- [ ] **SIM-36** Phase A.2: Particle Swarm full implementation (currently a stub) + benchmark validation against Sphere/Rosenbrock (size: M) — P1
- [ ] **SIM-38** Phase C: Multi-objective trade-off curve view — non-dominated set highlighted, click-to-preview, baseline-vs-optimized side-by-side, CSV export (size: L) — P1
- [~] **SIM-39** ~~Phase D1: CSV cycle-time import~~ — **RETIRED 2026-04-24**. Positioning clarified: SimOpt is agent-driven sim-opt for non-experts. Parameterization goes through an LLM agent (Claude Code / agent fleet), not through a fixed CSV schema. Import-layer would solve a problem this product class doesn't have. See `docs/decisions.md` "Agent-Driven Sim-Opt Positioning".
- [ ] **SIM-40** Phase D2: Replications with error bars — N=10 sim runs per fitness evaluation, mean ± std on chart, configurable N (size: M) — P2
- [ ] **SIM-41** Phase D3: Bottleneck heatmap overlay on live topology — green/yellow/red by utilization (size: M) — P2
- [ ] **SIM-42** Phase D4: Constraint handling — UI for max capital / max floor m²; solutions violating constraints excluded (size: M) — P2
- [ ] **SIM-43** Phase D5: ROI calculator panel — current vs proposed config + demand forecast → payback period + 5-yr NPV delta (size: M) — P2
- [ ] **SIM-44** RolandPrinter v2.1: per-piece fan-out emit semantics (currently emits 1 representative entity per batch; should release all N) (size: S) — P3

## FMT Architectural Validation (formalization paper Phase 4)

Plan: `docs/plans/2026-05-29-fmt-gridworld.md`
Spec: `~/aIware/docs/simopt-fmt-gridworld-spec.md`
Paper: `~/aIware/paper/fmt_formal/fmt-formalization.md`
Driver: FMT formalization paper needs computational validation of three sharp predictions.
Results integrate directly into paper before publication.

- [ ] **SIM-47** FMT gridworld — Phase 1: SimOpt.GridWorld generic agent-based engine (Grid, agents, step simulation, Q-learning baseline) (size: L) — P2
- [ ] **SIM-48** FMT gridworld — Phase 2: SimOpt.FMT (ESN reservoir, IWM/ISM/EWM/ESM, permeability gates, FMT agent, comparison agents) (size: XL) — P2
- [ ] **SIM-49** FMT gridworld — Phase 3: Prediction 1 — ESM ablation experiment + causal transfer test (size: L) — P2
- [ ] **SIM-50** FMT gridworld — Phase 3: Prediction 2 — criticality phase transition sweep (spectral radius vs. learning) (size: M) — P2
- [ ] **SIM-51** FMT gridworld — Phase 3: Prediction 3 — EWM coverage ablation vs. ESM self-prediction accuracy (size: M) — P2
- [ ] **SIM-52** FMT gridworld — Phase 3: Results reporting + paper-ready figures + statistical analysis (size: M) — P2
- [ ] **SIM-53** FMT gridworld — Meta-optimization: EnvironmentProblem (IProblem) that optimizes hazard placement/types/density to maximize Cohen's d between FMT and comparison agents, constrained by simpler-agent viability threshold (size: L) — P2
- [ ] **SIM-54** FMT gridworld — GridWorld Avalonia visualization: tile renderer (rect/hex), agent dots with architecture-colored markers, death flash animations, observation cones, learning-event indicators, per-agent stats overlay, speed controls. Extends existing SimOpt.Visualization (size: XL) — P2

## Platform follow-ups (captured 2026-04-24)

- [ ] **SIM-45** More complex factory floor topology — richer than FactoryFloor preset (walls, corridors, multi-bay layout, more realistic process chain; feeds into SIM-33 realistic renderer v2) (size: L) — **P2 proposed**
- [ ] **SIM-46** Live before/after viz compare — side-by-side canvases running baseline vs. optimized topology with synchronized clock; lets demo audience see throughput delta in real time (size: L) — **P2 proposed**
- [ ] **SIM-55** Glass production-line demo polish & follow-ups: extract `SimOpt.Glass` to its own project; richer glass floor layout; live before/after compare; resolve pre-existing Ivotion VM default test mismatch (`IvotionOptimizationViewModelTests.Defaults_MatchLockedInDecisions`). See `docs/pending-glass-demo.md` (size: M) — **P2 proposed**

## Critical-Subsystem Review Follow-ups (2026-07-05)

Source: `docs/2026-07-05-critical-code-review.md` (3 parallel Opus 4.8 review agents over DES engine, optimization+coupling, math/stats). Findings are hypotheses — fix each **test-first** (the failing test confirms the bug). Root-cause: SIM-56 (RNG) is upstream of several others; do it first.

- [ ] **SIM-56** RNG contract fixes (P1): MersenneTwister `[0,1)` + `Math.Abs(int.MinValue)` overflow; R250_521 buffer overrun (`!=520`); LCG `NextInteger` always-0; exp-family `U=0` +∞ guards (NegExponential/Erlang/Gamma). Highest leverage — upstream of SIM-57 exp-family + `Extensions` crashes. (size: M)
- [ ] **SIM-57** Distribution/median math (P1): Triangular sampler wrong CDF (**CRITICAL** — every triangular draw invalid); Gamma integer `1/3`; `Uniform*.Mean` `(max-min)/2`→`(min+max)/2`; `MMath.Median` off-by-one + N≤2 crash. (size: M)
- [ ] **SIM-58** DES reset-path fixes (P1): `Delay.Reset` NRE + no re-schedule (silent cross-eval divergence); `ResourceManager.Reset` snapshot aliasing; `Server.ClearCurrentMaterial` clears wrong list; `EventScheduler.timeOfNextScheduledEvent` sentinel/stale; `Model.RemoveEvent` same-time guard. (size: M)
- [ ] **SIM-59** Optimizer integrity (P2): seed the solution operators (Ivotion `Tweak`/`CombineWith` use wall-clock `new Random()` — reproducibility broken); EA clone `BestSolution`; EA clone-before-mutate (elitism/parent corruption); SA `while(T>0)` + T==0 guard; PSO explicit vector contract (reflection fits only `TestSolution` → zero search on real problems). Reconcile with SIM-36 (PSO is implemented, not a stub, but uncoupled). (size: L)
- [ ] **SIM-60** Lower-severity math/stats (P3): `Complex.Divide`/`Sqrt`/`Arg`/`Phase`; `MatthewsCorrelationCoefficient` int overflow; `Extensions` roulette off-by-one + wrong RNG method; antithetic `U<0`; `LogNormal(mean,stddev)` ctor NRE; ROC `x=1` endpoint; PCA eigenvalue scaling; `MMath.Factorial`/`Falling`. (size: M)
