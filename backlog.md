# SimOpt Backlog

## P0 — Commercial Track (added 2026-08-25)

SimOpt is now being built as a **commercial product**, not a research framework. Governing documents:

| Document | Purpose |
|---|---|
| `docs/needs/01_User-Needs.md` | Vision + 32 user needs (UN-001..032), roles, coverage matrix — **read before building** |
| `docs/commercial/2026-08-25-architecture-readiness-review.md` | Technical gap analysis + slice plan (§E) + MCP tool design (§C) |
| `docs/commercial/2026-08-25-business-strategy.md` | Market, pricing, financial model, GTM, kill gates |

**Owner decisions 2026-08-25:** open-core licensing (instrument still open, UN-031) · beachhead is
general discrete manufacturing sold through independent consultants · one engine with two heads
(MCP + desktop app), usable with or without either · ambition is *prove it earns anything first*,
so time-to-first-sale outranks completeness everywhere.

**Slice 0 is the existing SIM-56..58 critical-review debt.** It is not optional and it is not
deferrable: the product's entire claim is "trust these numbers", and today the default RNG violates
its contract, every triangular draw is mathematically wrong, and the reset path silently diverges
across evaluations. Both the architecture and the business review independently placed this at the
top of the commercial critical path, ahead of all UI work.

### Slice 0 — Trustworthy engine (blocks everything)
- [x] **SIM-56** RNG contract fixes — see P1 section below (size: M) — **P0** — done 2026-08-25
- [x] **SIM-57** Distribution/median math — see P1 section below (size: M) — **P0** — done 2026-08-25
- [ ] **SIM-81** MersenneTwister is not a Mersenne Twister (P2): it omits the tempering step and pre-seeds its 624-word buffer from `System.Random.Next()`, so its first 624 outputs *are* `System.Random` and its high bit is never set in that window. Flagged as Medium by the 2026-07-05 review and confirmed during SIM-56/57 — it is the reason the raw-word mapping must mask rather than shift (`UniformMapping.TryMapToInteger`). Replace with the reference initialisation (`mt[i] = 1812433253 * (mt[i-1] ^ (mt[i-1] >> 30)) + i`) plus tempering. Changes every stream, so it needs the SIM-64 analytic battery in place first to prove the change is an improvement rather than just a difference. (size: M) — **P2**
- [ ] **SIM-58** DES reset-path fixes — see P1 section below (size: M) — **P0**
- [ ] **SIM-62** Stable per-node seed derivation in `ModelRegistry` — `node.Id.GetHashCode()` (`ModelRegistry.cs:76,117`) uses .NET's per-process randomized string hashing, so the same topology + seed yields different streams after every process restart. Replace with FNV-1a over UTF-8 or an ordinal node index. Breaks UN-009 (reproducibility). Found by the 2026-08-25 architecture review. (size: S) — **P0**

### Slice 1 — Output statistics + analytic verification
- [ ] **SIM-63** Engine output-statistics subsystem: event-driven tallies (wait time, cycle time), time-weighted collectors (queue length, utilisation), warm-up truncation, replication runner with confidence intervals. **Must live in the engine, not the UI** — utilisation is currently computed by polling in `SimulationCanvas.cs:297-332` and is unreachable from headless/MCP runs. Serves UN-011, UN-012. Largest genuinely new engineering item in the product. (size: L) — **P0**
- [ ] **SIM-64** Analytic benchmark battery as a CI gate: M/M/1, M/M/c, M/G/1 (Pollaczek-Khinchine), small Jackson network — simulated results must fall within CI of closed-form answers. Plus distribution goodness-of-fit tests on large samples. Serves UN-007. This is simultaneously the quality gate and the strongest marketing asset the product has. (size: M) — **P0**

### Slice 2 — Model schema v1 + experiment tools
- [ ] **SIM-65** Topology schema v1: multi-capacity `station(servers: c)`, distribution objects (exponential/triangular/uniform/lognormal/gamma/constant/empirical), routers (probabilistic/shortest-queue/by-attribute), declared metrics, `schema_version` field, published JSON Schema via `get_schema`. Today's schema has 4 node types, exponential only, single-capacity servers — **it cannot express the product's own flagship example**. Serves UN-001, UN-004. Blocked by SIM-56/57 (wiring Triangular before the fix would industrialise the bug). (size: L) — **P0**
- [ ] **SIM-66** MCP tools `validate_model` / `patch_model` / `run_experiment`: three-layer validation (schema → semantic lint with analytic ρ pre-checks → 1s trial replication), all errors structured and returned at once; scenario arrays evaluated in one call; result payloads under ~400 tokens. Serves UN-003, UN-008, UN-013, UN-023. (size: L) — **P0**
- [ ] **SIM-67** Model persistence: save/load/export topology JSON, re-hydrate on reconnect. Models currently die with the process (in-memory `ConcurrentDictionary`, `ModelRegistry.cs:20`). Serves UN-005. (size: S) — **P1**
- [ ] **SIM-20** MCP server integration testing — **promoted to P0.** Zero tests exist against the MCP surface today. Serves UN-023, UN-025.

### Slice 3 — Generic optimisation
- [ ] **SIM-68** `TopologyProblem`: a generic `IProblem` whose genome is decision variables declared as references into the topology JSON, applied as patches, evaluated over N replications. Eliminates hand-written per-domain `IProblem`s and gives EA/SA/PSO a uniform non-reflection vector contract — fixes SIM-59's PSO defect by design. **The keystone component of the whole product.** Serves UN-015. (size: L) — **P0**
- [ ] **SIM-69** MCP `optimize` tool: variables, objective, constraints, budget, replications-per-eval, strategy selection. The "opt" in SimOpt is currently absent from the MCP surface entirely. Serves UN-015. (size: M) — **P0**
- [ ] **SIM-59** Optimizer integrity — see P1 section below. Required before optimisation results can be trusted or reproduced. (size: L) — **P0**

> **v0.9 "Pro" ships at the end of Slice 3** — MCP head only, driven from Claude Code/Cursor/any MCP
> client, zero UI code. This is the earliest possible revenue and the fastest test of whether anyone
> pays, which is exactly the owner's stated ambition. Architecture review estimate: ~6–7 agent-weeks.

### Slice 4 — Desktop application with embedded chat
- [ ] **SIM-70** Extract `SimulationTools` + `ModelRegistry` + experiment/optimize layer into a shared `SimOpt.Engine.Api` library with two heads: the existing stdio MCP server, and in-process consumption by the desktop app. Contract test asserts both heads expose an identical tool list. **Structural prerequisite for UN-025 (capability parity).** (size: M) — **P1**
- [ ] **SIM-71** `SimOpt.App`: Avalonia desktop app hosting the agent loop in-process via `Microsoft.Extensions.AI` `IChatClient` (Anthropic/OpenAI/Ollama/LM Studio). Streaming chat pane, model read-back panel, results table. OS-native credential storage only (DPAPI/Keychain/libsecret) — never a settings file. Serves UN-001, UN-002, UN-020, UN-021. (size: XL) — **P1**
- [ ] **SIM-72** Visualization ↔ MCP unification: map `TopologyDefinition` → `VizTopology` (AutoLayout exists) so MCP-built models are watchable. Two disjoint topology representations exist today. Serves UN-018. High demo value. (size: M) — **P2**

### Slice 5 — Commercial plumbing
- [ ] **SIM-73** **Licensing decision + LICENSE file** (UN-031). Repo is public with no licence → all rights reserved by default. Owner has chosen open-core in principle; instrument undecided (permissive Apache-2.0 vs source-available FSL/PolyForm converting to permissive). Business review recommends against permissive on the engine. **Owner decision required — blocks public launch.** Also: confirm dissertation-era provenance carries no institutional claim. (size: S, but blocking) — **P0 (decision)**
- [ ] **SIM-74** Signed installer + update channel: OV/EV code-signing certificate (unsigned installers kill conversion via SmartScreen), Windows first, macOS notarization later. Serves UN-026. (size: M) — **P2**
- [ ] **SIM-75** Licence keys + merchant-of-record checkout (Paddle or Lemon Squeezy — MoR owns all EU/global VAT). Serves UN-029. (size: M) — **P2**
- [ ] **SIM-76** Privacy statement for LLM egress + free-tier limits + first-run wizard (key entry or Ollama auto-detection). Serves UN-021, UN-022, UN-026, UN-028. (size: M) — **P2**

### Slice 6 — Go-to-market (runs in parallel, not after)
- [ ] **SIM-77** Landing page + waitlist + canonical 3-minute demo video. Cannot start before a recordable demo exists (end of Slice 2 at the earliest). (size: M) — **P2**
- [ ] **SIM-78** Content pipeline: LinkedIn cadence (2–3/week, agent-drafted, owner-approved, each a short screen capture of a sim answering a real question), YouTube demo series, agent-generated SEO comparison pages. Owner time budget ~2h/week total. (size: M, ongoing) — **P2**
- [ ] **SIM-79** Employer-boundary hygiene before first sale: document dissertation provenance, obtain written Nebentätigkeit acknowledgment (generic "simulation software", no dental specifics), keep Ivotion material internal and **out of the public beachhead**. Structural, not moral — do it before revenue exists, not after. (size: S) — **P1**
- [ ] **SIM-80** Resolve model-access economics (UN-032): BYO-key margin vs. non-expert accessibility. Options are pro-first (defer the keyless persona), bundled metered key at a premium tier, or local-model-as-default. **Owner decision required before Slice 4 onboarding is designed.** (size: S, decision) — **P1 (decision)**

### Kill gates (from the business review — measurable, dated from commercial build start)
| When | Gate | If missed |
|---|---|---|
| Month 3 | Chat→correct-model succeeds unassisted ≥50% of attempts; SIM-56..59 closed | Core thesis fails → pivot to consultant-operated tool, or stop |
| Month 6 | 100+ free activations, ≥10 paying, ≥1 paying **stranger** (not network) | Distribution thesis broken → one more channel experiment, then stop |
| Month 12 | €500+ MRR-equivalent, first-cohort churn <50%/yr | Wind down to maintenance, or open-source for reputation value |
| Month 24 | €2,000+ MRR and a repeatable channel | Hold deliberately as a lifestyle product, or sell the asset |

---

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

- [x] **SIM-56** RNG contract fixes (P1): MersenneTwister `[0,1)` + `Math.Abs(int.MinValue)` overflow; R250_521 buffer overrun (`!=520`); LCG `NextInteger` always-0; exp-family `U=0` +∞ guards (NegExponential/Erlang/Gamma). Highest leverage — upstream of SIM-57 exp-family + `Extensions` crashes. (size: M) — **DONE 2026-08-25.** All four findings confirmed by failing tests first. Extracted the shared raw-draw→contract-range fold into `UniformMapping` (new, boundary-tested exhaustively) because three generators duplicated it and two got it wrong; MT's defect is a 2⁻³¹ event and is *not* reachable by sampling, so the pure function is the only honest gate. Exp-family guards use the `1−U` inverse-transform form rather than rejection — distributionally identical, no singularity, and no infinite loop on a degenerate source. 42 new tests in `RandomSourceContractTests`; 736 pass / 0 regressions.
- [x] **SIM-57** Distribution/median math (P1): Triangular sampler wrong CDF (**CRITICAL** — every triangular draw invalid); Gamma integer `1/3`; `Uniform*.Mean` `(max-min)/2`→`(min+max)/2`; `MMath.Median` off-by-one + N≤2 crash. (size: M) — **DONE 2026-08-25.** Triangular confirmed exactly as predicted: Tri(10,12,20) capped at 14.4721 = 10+2√5 against a true max of 20, so the entire upper tail was missing. Median was worse than reported — the five `List<T>` extension overloads carry the same odd-N off-by-one independently of the array version. **Two findings not in any review:** (a) `UniformDoubleDistribution.Initialize(seed, antithetic)` and `Initialize(IRandomSource)` never set `interval`, so it stayed 0 and `Next()` returned a **constant** for the instance's whole life — `GammaDistribution` builds its internal uniform through exactly that path, so every seed-constructed Gamma was drawing against a degenerate source; (b) that same degeneracy turned into an **infinite loop** in Gamma's rejection sampler once SIM-56 changed which endpoint the uniform collapsed to — a silent wrong answer became a visible hang, which is strictly the better failure. 54 new tests; 790 pass / 0 regressions.
- [ ] **SIM-58** DES reset-path fixes (P1): `Delay.Reset` NRE + no re-schedule (silent cross-eval divergence); `ResourceManager.Reset` snapshot aliasing; `Server.ClearCurrentMaterial` clears wrong list; `EventScheduler.timeOfNextScheduledEvent` sentinel/stale; `Model.RemoveEvent` same-time guard. (size: M)
- [ ] **SIM-59** Optimizer integrity (P2): seed the solution operators (Ivotion `Tweak`/`CombineWith` use wall-clock `new Random()` — reproducibility broken); EA clone `BestSolution`; EA clone-before-mutate (elitism/parent corruption); SA `while(T>0)` + T==0 guard; PSO explicit vector contract (reflection fits only `TestSolution` → zero search on real problems). Reconcile with SIM-36 (PSO is implemented, not a stub, but uncoupled). (size: L)
- [ ] **SIM-60** Lower-severity math/stats (P3): `Complex.Divide`/`Sqrt`/`Arg`/`Phase`; `MatthewsCorrelationCoefficient` int overflow; `Extensions` roulette off-by-one + wrong RNG method; antithetic `U<0`; `LogNormal(mean,stddev)` ctor NRE; ROC `x=1` endpoint; PCA eigenvalue scaling; `MMath.Factorial`/`Falling`. (size: M)

### Inbox intake 2026-08-24 — delivered by cfg-agent-fleet

1 item(s) moved verbatim from `cross-project/inbox.md` while this project had no active session. Preambles name the original source session and date.

- [ ] [P3] **SIM-61: FMT domain logic forked to Python in `~/crucible`.** (P3 — provenance, from aIware/crucible WSL session 2026-07-06): **FMT domain logic forked to Python in `~/crucible`.** Per aIware S242 decision #7 ("simopt: fork, don't bridge"), crucible now ports simopt's FMT-specific domain logic to Python (single-language, `pip install`, peer-review reproducible). **PORTED:** `SimOpt.GridWorld` — Grid + `CellInfo(HazardFamily, CausalMechanism)`, `IGridAgent` ABC, `GridObservation`, `AgentEvent` death-broadcast, `GridSimulation.tick()`, QLearning/Random baselines. **NOT ported (deliberate):** the DES engine (step-based env, unneeded), RNG/matrix/stats (→ numpy/scipy; and simopt's MersenneTwister/distributions have documented bugs per `~/simopt/docs/2026-07-05-critical-code-review.md`), Avalonia viz (→ matplotlib/pygame). `SimOpt.FMT` (empty C# scaffold) is REWRITTEN fresh in numpy, not ported. The ESN survives in crucible as a rate-reservoir **baseline control** (for the "spiking self-organized criticality vs spectral-radius-by-fiat" ablation). No action needed on simopt beyond awareness; crucible credits simopt in its README. Source: crucible repurpose, aIware slave session 2026-07-06.
