# SimOpt Decisions

Topic-organized record of important decisions and design rationale.
NOT a rule sheet (that's CLAUDE.md). NOT session state (that's session-context.md).

---

## FSIM-03 — glass code extracted to furkansim (2026-06-18)
- **simopt is now framework-only.** The Ivoclar glass base-mass digital twin + optimizer was
  moved out of public simopt into the private `IvoclarR-D-AIOrg/furkansim` (commits: simopt
  b1c2fc8, furkansim f573ee5). Rationale: simopt is PUBLIC; real Ivoclar station names/timing/
  operators (coming in Phase B calibration) cannot live there.
- **Extraction shape:** only the 11 `Glass*.cs` (namespace `SimOpt.Glass`) moved — they lived
  inside `SimOpt.Ivotion` alongside the Ivotion denture-packing domain, which STAYS public.
  furkansim reuses simopt's generic viz primitives (`SimulationCanvas`, `VizTopology`,
  `AutoLayout`, `ViewModelBase`) via **sibling ProjectReference** into `../simopt/src/SimOpt.*`.
- **Namespace kept as `SimOpt.Glass`** in furkansim deliberately — makes the move mechanical
  (no churn on using-statements); renaming to a furkansim namespace is a separate later task.
- **furkansim is an agent-fleet project now:** has `.claude/` (so `af` CWD-detects it) and is in
  cfg `registry.md`/`dashboard-cache.md`. Cross-project edits (cfg, furkansim) were made from
  this simopt session at the user's explicit request rather than via separate sessions.

---

## Glass digital-twin → dual-repo split (furkansim)

**Decided:** 2026-06-18 — after the glass base-mass demo for Furkan Bolat (Value Stream Manager Glass, Ivoclar) was delivered and validated.

- **Dual repo.** `simopt` stays the generic, reusable simulation-optimization framework and is **PUBLIC**. A new repo **`furkansim`** (private, `IvoclarR-D-AIOrg`) holds all Ivoclar/glass-ceramics-specific code, calibration, and customer data. Rationale: keep confidential Ivoclar production data and customer-specific topology out of the public framework; let the framework stay clean and reusable.
- **Confidential data committed into the private repo by design.** Furkan's SAP export (`WBZ_DLZ_Analyse_2026.xlsx`) lives in `furkansim/data/`; calibration reference in `furkansim/docs/`. Never to be mirrored to public `simopt`.
- **Framework dependency = sibling ProjectReference.** furkansim `.csproj` references `../simopt/src/SimOpt.*.csproj` directly (both repos checked out side-by-side). Chosen over git submodule / NuGet packages for solo-dev simplicity and instant rebuilds against framework changes.
- **Calibration must precede in-data work because simopt is public:** the Glass code is extracted from simopt into furkansim *before* calibrating with real station names/timing/operators (calibrating in-place would leak to the public repo).
- **Realism direction:** calibrate sim + optimizer from Furkan's real data (stations, value-stream routings, per-step dwell, ~2.6 orders/day arrivals, ~178 KG batches, DLZ median ≈11 working days), then add the 10 real operators as mobile resources walking/carrying between station clusters, then integrate a real floor plan (background image + geometry/distance source) when Furkan provides it. Full plan: `furkansim/docs/furkan-data-calibration.md`, `furkansim/backlog.md`.

---

## Optimization Showcase (Ivoclar Ivotion follow-up demo)

**Decided:** 2026-04-24 — after a successful spontaneous customer demo
**Plan:** `docs/plans/ivotion-optimization-showcase.md`

### Strategy lineup
Ship all four optimization strategies in the demo: Random, Evolutionary,
Particle Swarm (full implementation — currently a stub), and Sweep
(exhaustive enumeration). Rationale: search space is small enough (216
combinations) that both iterative and exhaustive approaches are viable;
showing all four lets the colleague see the framework's breadth.

### Multi-objective approach
True non-dominated set (trade-off curve), NOT scalarized weighted-sum.
Rationale: user explicitly chose the more compute-expensive but more
honest approach when offered the trade-off. Machine can afford it.

### Cost model (US defaults, configurable)
- Roland LEF capital: $30k → $3/hr amortized over 5 yr × 2000 hr/yr
- Skilled production operator: $32/hr fully loaded
Rationale: user-confirmed US numbers for the demo audience. Operator
wage will be exposed in the UI for "what-if" exploration.

### Operator parallelism model
`effective_service_time = base_service_time / operator_count`.
Rationale: simple, honest, demoable. A real `ResourceManager` operator
pool with shared queues is a v3 enhancement, not v1. Deferred without
shame; will disclose if asked.

### "Apply best solution to viz" semantics
Load the topology with optimized parameters but do NOT auto-start the
simulation. User hits Space when ready. Rationale: gives the presenter
a moment to point at the new layout and narrate before motion starts.

### UI default objective
Shipped as `MinimizeCostPerPiece` (SIM-37 Phase B, 2026-04-24). Rationale:
`MaximizeThroughput` is monotone in operator count + Roland count on a 216-combo
search space — EA converges in generation 1 and draws a flat fitness line,
making the optimization look trivial in a demo. Cost-per-piece creates a real
throughput-vs-labor tradeoff so the line visibly climbs. The user still picks
any of the five objectives from the dropdown; the default is just the most
demo-friendly starting point.

### SkiaSharp native/managed pin (Avalonia + ScottPlot)
When adding `ScottPlot.Avalonia` (managed SkiaSharp 3.119.0) alongside
`Avalonia.Skia` (transitively pins `SkiaSharp.NativeAssets.Linux` at 2.88.9),
Linux startup crashes with `native libSkiaSharp 88.1 incompatible with managed
SkiaSharp [119.0, 120.0)`. Fix: explicit `<PackageReference
Include="SkiaSharp.NativeAssets.Linux" Version="3.119.0" />` in the consuming
csproj. Avalonia's Skia bundle handles Win32/macOS at 3.119 but leaves the
Linux native at 2.88. Keep this pin while Avalonia 11.3.x is in use; revisit
if we upgrade Avalonia.

### Charting library
ScottPlot.Avalonia (NuGet) for live fitness curves and trade-off plots.
Rationale: mature, fast, simple API, free for any use; lighter weight
than LiveCharts; less work than hand-rolling Canvas drawing.

---

## RolandPrinter Domain Class (built spontaneously during customer demo)

**Decided:** 2026-04-24

### Emit semantics
RolandPrinter emits one representative entity per completed batch (not all
N). Rationale: throughput math is honest because the batch-cycle relationship
holds. Per-piece fan-out (release all 15) requires invasive surgery into
the base Server's event pipeline. Deferred to SIM-44 (P3, time-permitting).

### Inheritance vs composition
RolandPrinter subclasses SimpleServer rather than wrapping it. Rationale:
inherits all the StateMachine event plumbing for free; the only addition
is `checkMaterialComplete` (batch threshold) and the `BatchSize` /
`PerPieceTime` / `CycleTime` first-class properties.

---

## Spontaneous Customer Demos as the Killer Pitch

**Established:** 2026-04-24 (validated against a real Ivoclar colleague)

The framework's strongest selling point is the ability to spec and build
a working production-line model DURING a meeting from cycle times the
customer mentions verbally. Used twice in one session (v1 IvotionPacking
preset + v2 RolandPrinter class). The demo flow:

1. Customer states the problem and gives rough numbers
2. Live-build the topology preset (~3-5 min)
3. Run it in viz, point at the bottleneck
4. If the customer mentions a domain-specific quirk (batching, etc.),
   write a domain class for it and ship + test it on the spot

This is a deliberate capability to maintain. Future demos should preserve
it: keep the topology DSL declarative, keep the Server template extensible,
keep test scaffolding ready to validate spontaneous additions.

---

## Agent-Driven Sim-Opt Positioning

**Decided:** 2026-04-24

SimOpt is not aimed at enterprise sim-opt with rigid data contracts and
PhD-level expert operators. It targets **non-experts using Claude Code or
agent fleet with the SimOpt skill** to build preliminary or even final
simulation-optimization systems. Use cases span:

- **Production use** — the output system is good enough to run for real.
- **Research pre-study** — scouts the problem before a deeper / more rigorous
  commissioned project.
- **What-if scratchpad** — throwaway analysis that dies with the conversation.

### Consequences

- **No standardized import schemas.** CSV/JSON import layers are the wrong
  abstraction — they re-introduce the expert-configuration problem we're
  trying to remove. When a user has a spreadsheet, the LLM agent reads it and
  parameterizes the builder directly in natural language. This retired
  SIM-39 mid-planning; resist the urge to re-add it.
- **Topology DSL stays declarative and easy to emit.** Anything an LLM has
  to assemble should be one flat JSON/record-style object, not a multi-step
  builder API with hidden required calls.
- **Domain classes (like `RolandPrinter`) belong with the skill's memory,
  not as customer-facing options.** The user says "it batches 15 pieces at
  a time"; the agent recognizes the pattern and picks the class. Avoid
  exposing "pick your server subtype" UX.
- **Validation lives in tests, not in constructor parameter matrices.**
  When a user (or agent) proposes an unusual topology, a fast test suite
  says whether it runs to completion. That is cheaper than schema
  validation and catches the failures that actually matter.
- **MCP server (SIM-19/20) is first-class surface area**, not a side
  feature. It's how the agent reaches the framework.

### What this implies for the Ivoclar demo

The colleague's "real data next week" doesn't require a schema. When the
numbers arrive — in whatever form — the agent takes them, updates the
`IvotionTopologyBuilder` baseline constants (or constructs the solution
vector directly), and reruns. The UI shows the result. No import screen.

---

## FMT Architectural Validation Gridworld

**Decided:** 2026-05-29

### Sub-project placement
FMT gridworld lives inside SimOpt (not a separate repo). Bidirectional value:
SimOpt gains agent-based gridworld as a new simulation paradigm; FMT gets
SimOpt's math/stats/stochastic infrastructure for free.

### Two-project split
`SimOpt.GridWorld` (generic agent-based engine) + `SimOpt.FMT` (FMT-specific
architectures). GridWorld is reusable beyond FMT; FMT depends on GridWorld.

### .NET/C# over Python
Spec recommended Python + gymnasium. User chose .NET to keep everything in
the SimOpt ecosystem. Reservoir computing, Q-learning, and grid simulation
are straightforward to implement in C#.

### Topology-agnostic architecture
`ITopology<TCoord>` with `Coord2D`/`Coord3D`/`HexCoord` coordinate types.
Agents generic over `TCoord`. Concrete convenience: `Grid2D`, `Grid3D`,
`HexGrid`. Richer connectivity (hex, 3D) strengthens the causal-structure
vs flat-association distinction for FMT experiments.

### MoveTo/Reset separation
`MoveTo(TCoord)` for tick-by-tick movement, `Reset(TCoord)` for episode
lifecycle initialization. Motivated by round-1 review finding that using
Reset for movement would corrupt any agent that clears learned state on reset.

### CellInfo metadata for causal mechanisms
`CellInfo(Type, HazardFamily, CausalMechanism)` stored sparsely alongside
`CellType`. Enables FMT Prediction 1 (causal transfer): lava (thermal) vs
deep water (submersion) share death outcome but differ in mechanism.

### Terminal reward before death
`OnStepComplete(HazardReward)` is called before `OnDeath()` in the same tick,
so RL agents receive the terminal transition signal. Round-2 review finding.

### Environment meta-optimization (SIM-53)
Use SimOpt's own optimization framework (IProblem/IStrategy) to find the
environment configuration that maximizes effect size between FMT and comparison
agents, constrained by simpler-agent viability. The optimizer optimizes the
environment, not the agent.

---

## Agent-Driven Sim-Opt Positioning (continued)

### What this does NOT change

- Core engine invariants (discrete-event only, Server→Buffer patterns, etc.)
  remain authoritative. Agent flexibility stops at the framework's
  structural guarantees.
- Test-driven discipline stays mandatory — even when the agent is
  assembling a throwaway scratchpad, it writes tests. Faster than
  debugging, and the tests double as "this is what I built" evidence.

## 2026-06-18 — Glass production-line digital-twin demo

- **Public-repo content boundary:** simopt is a public GitHub repo, so customer/pilot
  specifics never go into committed source — new demo code uses generic "Glass" naming.
  Tracked+public files (decisions, backlog, pending, next-session-task) stay generic;
  gitignored files (session-context, session-log, session-history) may hold specifics.
- **Reuse over clone:** the glass optimization reuses the generic EA/Random optimizer;
  only the domain (solution/problem/topology/KPIs) is new. Domain classes were placed in
  the existing SimOpt.Ivotion assembly (namespace SimOpt.Glass) to avoid the broken .slnx
  project-add path — flagged as tech debt (SIM-55).
- **Twin authoring workflow validated end-to-end:** a plain-language line description maps
  to a `VizTopology` factory method that the visualization auto-renders, and the optimizer
  applies its best solution back to the live twin — the "semantics + data → ad-hoc,
  optimizable digital twin" loop.

## 2026-07-05 — Critical-subsystem code review (Opus 4.8, 3 parallel agents)

- **Fable availability:** the injected agent roster still tags Fable 5 as geo-blocked — that's stale.
  Per MG (2026-07-05) Fable is re-enabled for EU (temporary, revocable); already tracked in the
  cross-project inbox for cfg-agent-fleet to fix the agent description + `fleet-capabilities.md`.
  This review ran on **Opus 4.8** (the reliable fallback; Fable's content-gate has been observed
  flapping). No re-run on Fable — the Opus findings stand.
- **Scope = the 3 correctness-critical cores only** (DES engine, optimizer+coupling, math/stats),
  not viz/learning/logging — the places where a silent bug poisons every result.
- **Verdict:** kernels are healthy (deterministic scheduler, comprehensive deterministic reset,
  consistent maximization, fixed linear-algebra + RandomStrategy). The correctness debt sits in
  (a) the **stochastic layer** — the default MersenneTwister violates its `[0,1)` contract and
  crashes on `int.MinValue`, cascading into +∞ draws from every log-based distribution;
  Triangular/Gamma/Median are independently wrong — and (b) the **strategy↔solution operator seam**
  — wall-clock RNG breaks reproducibility, EA aliases its best-so-far, PSO's reflection contract
  fits only the unit-test solution. Full findings + fixes: `docs/2026-07-05-critical-code-review.md`;
  tracked SIM-56..60.
- **PSO status clarified:** `ParticleSwarmOptimization` is a real implementation (the `[Obsolete]`
  `SwarmingAlgorithm` is the stub), but coupled via reflection to a settable `double[] Parameters`
  that only `TestSolution` provides → zero search on real problems. SIM-36's "PSO is a stub" note
  needs reconciling.

---

## Commercial pivot — the four founding decisions (2026-08-25)

Decided by MG in one sitting after two independent Fable review passes
(`docs/commercial/2026-08-25-architecture-readiness-review.md`,
`docs/commercial/2026-08-25-business-strategy.md`). These four settle questions that were
previously carried as open needs (UN-031, UN-032) and they govern everything downstream.

### D-01 · Licensing: FSL-1.1-ALv2 on the engine, proprietary on the app

**Decided.** The public repository is licensed **FSL-1.1-ALv2** (`LICENSE.md`). The desktop
application, its embedded assistant and the commercial infrastructure are separate works in a
separate private repository, and are not covered by it.

- **The prior state was the asset.** Public repo with *no* licence file meant all rights reserved
  by default: nothing had been given away, so every option was still open. It was spent
  deliberately rather than drifted out of.
- **Why FSL rather than Apache-2.0.** The business review's argument, accepted: the engine is the
  trust layer that the whole product claim rests on, and a one-person shop cannot outrun a fork.
  FSL blocks *competition* while permitting everything else.
- **Correcting an earlier mis-description.** FSL was initially presented to MG as "free for
  non-commercial use, paid for commercial use". That is PolyForm's model, not FSL's. FSL permits
  **any purpose except a Competing Use** — so commercial internal use, and consultants' client
  work, are free. The revenue therefore has to come from the closed application layer, not from
  licensing the engine. MG's decision stands with the accurate reading; it is in fact closer to
  the open-core he originally chose.
- **Two-year Apache conversion is a feature, not a concession.** It preserves the dissertation
  lineage's academic credibility — citable, inspectable, eventually fully open — which is the
  cheapest credibility available to this product.
- **Still owed:** confirm the dissertation-era code provenance carries no institutional claim.
  Austrian universities normally leave dissertation copyright with the author; verify rather than
  assume. Tracked as SIM-73.

### D-02 · Model access: local-first, cloud optional

**Decided.** The default path is a **locally-hosted model** (Ollama / LM Studio). Commercial
providers remain fully supported but are opt-in, not the happy path.

This resolves UN-032, and it resolves it in the direction that removes the product's sharpest
contradiction: bring-your-own-key gave ~95% gross margin but assumed an API key that the primary
persona — a plant engineer at an SMB manufacturer — does not have and will not obtain.

Consequences, all of which are now requirements rather than options:

- **Confidentiality becomes a headline feature, not a compliance footnote.** A factory's layout
  never leaves the building. In EU manufacturing — the chosen beachhead — this is a selling point
  that no cloud-based competitor can match.
- **Zero marginal inference cost for both sides.** Neither we nor the customer pays per token.
- **Constrained decoding becomes the mechanism that makes this viable.** llama.cpp and Ollama can
  constrain generation to a JSON schema, so topology output can be made **syntactically valid by
  construction** rather than by hoping the model behaves. This is a capability local inference has
  and several cloud APIs do not — it converts the main weakness of small models into a
  non-problem for the structural half of the task. Tracked as SIM-83.
- **The honest risk.** A small local model is materially weaker at *inferring* a correct model
  from a vague description, even when its JSON is well-formed. Two mitigations, both already in
  the needs: the read-back-and-confirm loop (UN-002) becomes load-bearing rather than a nicety,
  and the validation layer (UN-008) must catch semantic nonsense the model cannot.
- **A hardware floor now exists and must be stated.** A useful model needs roughly 8–16 GB of
  VRAM or comparable system memory. Some target-persona laptops will not clear it; those users
  fall back to a cloud provider. This must be checked at first run, not discovered at first
  failure. Tracked as SIM-82.
- **A named reference model is now a product artefact.** We must pick one, ship against it, and
  measure it — "works with local models" is not a claim until a specific model passes a specific
  benchmark. Tracked as SIM-84, which is also the Month-3 kill-gate measurement.

### D-03 · Backlog priorities as proposed

**Decided.** SIM-62/63/64/68/69 at P0; SIM-67/70/71/79/80 at P1; remainder P2. No changes
requested.

### D-04 · Employer boundary handled before revenue, not after

**Decided.** Document the dissertation provenance, obtain a written Nebentätigkeit acknowledgment
worded generically ("simulation software", no dental specifics), and do not lead the public
positioning with dental. Tracked as SIM-79.

**Reconciled against FSIM-03, which already ruled on this and is NOT reversed.** FSIM-03
(2026-06-18) moved the *glass* code to the private `furkansim` because it carried real Ivoclar
station names, timings and operators, and deliberately kept the **Ivotion denture-packing domain
public** because it carries no confidential data. The business review's "keep Ivotion material
internal" is about *marketing positioning* — do not make dental the public beachhead — not about
code location. Both hold simultaneously:

- **Code:** `SimOpt.Ivotion` stays public, per FSIM-03. The trigger that would change this is real
  Ivoclar production data entering it — the same trigger that moved glass out.
- **Positioning:** the public beachhead is general discrete manufacturing sold through independent
  consultants. Ivotion is a showcase vertical, not the story we lead with.

### What these four decide together

The product is: an FSL-licensed engine anyone can read and use, a proprietary application that
runs a local model by default so nothing leaves the customer's building, sold into general
discrete manufacturing through consultants, with correctness proven against closed-form results
before any of it is offered for money.

---

## D-05 — SimOpt is an instrument of the Gutachtertätigkeit (2026-08-25)

**Stated by MG on 2026-08-25, closing SIM-79 and SIM-85 and reframing the venture's basis:**

> "Already have right to work as Gutachter. Any software I create would be part of my
> Gutachtertätigkeit. I need to be able to simulate the interaction of computer systems with the
> real world to prove cases, this is why I have that product. Selling it as a side earner is a
> simple extension of that business at my discretion."

### What this settles

- **SIM-85 (dissertation provenance): CLOSED.** The code is MG's own; no institutional claim.
- **SIM-79 (employer boundary): CLOSED, and on a stronger footing than the one proposed.** The
  business review assumed the standard structure — employee starts a side software business, needs
  a Nebentätigkeit acknowledgment. That is not the situation. MG holds an existing, separately
  constituted professional practice as a **gerichtlich beeideter Sachverständiger**, and software
  he creates falls within it. SimOpt is a tool of that practice, not a new venture beside the
  employment. Commercialisation is an extension of an existing business, exercised at his
  discretion.
- The conflict-of-interest framing in `docs/commercial/2026-08-25-business-strategy.md` §D is
  therefore **superseded**. Its advice was sound for the structure it assumed; the structure is
  different.

### What this reveals — the origin use case was never recorded

The product exists to answer a forensic question: *how did this computer system behave when it met
the real world, and can that be demonstrated?* That is a different discipline from capacity
planning, and it carries requirements the User Needs did not capture, because nobody had written
down why the tool was built. Added as UN-033..UN-036.

The important consequence is that **the forensic bar is strictly higher than the commercial one,
and we have already been building to it by accident.** Everything Slice 0 fixed — a random source
that honours its contract, a triangular sampler that is not silently truncated, a reset path that
does not diverge between runs, a seed derivation that survives a process restart — is precisely
what an opposing expert would attack first. Reproducibility (UN-009) stops being good engineering
practice and becomes an evidentiary property: a result that cannot be reproduced on another
machine, months later, by someone else, is not evidence.

### Open strategic question — deliberately NOT decided here

The beachhead decision of 2026-08-25 (general discrete manufacturing, sold through independent
consultants) was taken without this information on the table. It is worth revisiting, because on
the criteria that matter to a solo owner with no marketing time, the forensic/Sachverständiger
market scores better on almost every axis:

| | Manufacturing consultants | Sachverständige / forensic |
|---|---|---|
| Owner's standing in the market | none yet | **already a practitioner** |
| Distribution | must be built | existing professional network |
| Willingness to pay | €790/yr is a considered purchase | expert-witness work bills at multiples of that |
| Competition | AnyLogic/FlexSim/Simio are strongest here | **essentially unserved** |
| What the product must be excellent at | breadth of features | **correctness and reproducibility — what we just built** |
| Owner can be customer zero | no | **yes** |

Against it: the market is far smaller, sales are relationship-led rather than product-led, and
"simulation as evidence" has an admissibility literature that would need real work before any
claim is made. It may be the better *wedge* while general manufacturing remains the better
*volume* market — the two are not exclusive, and the engine is the same.

**Do not act on this unilaterally.** It is an owner decision, recorded here so the next session
presents it rather than rediscovering it. Tracked as SIM-87.
