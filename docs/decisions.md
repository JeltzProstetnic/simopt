# SimOpt Decisions

Last reviewed: 2026-11-03 (archive: docs/decisions-archive.md)

Topic-organized record of important decisions and design rationale.
NOT a rule sheet (that's CLAUDE.md). NOT session state (that's session-context.md).

---

## Topology schema v1 — shape and deliberate exclusions (2026-08-26, SIM-65)

Full statements and the evidence behind each: `docs/plans/2026-08-26-sim-65-topology-schema-v1.md`
(D1–D7). The ones that constrain future work:

- **`buffer` and `server` remain accepted aliases for `queue` and `station`.** The v1 names are the
  only ones advertised by `get_schema` and `list_templates`, but the old ones keep parsing. The
  reason is not politeness to old files: the pre-SIM-65 tests are the only instrument that can say
  the schema change broke nothing, and renaming the vocabulary out from under them destroys it.
- **A multi-capacity `station(servers: c)` is built as a fan-out of c `SimpleServer`s behind a
  shortest-idle dispatcher, not as a new c-channel `Server`.** The fan-out is the exact construction
  SIM-64's analytic battery already validates against Erlang-C on both Wq and Lq. A generalised
  engine class is the better end state and will have to earn the same evidence before it replaces
  this.
  *Built 2026-11-25 (step 3):* the dispatcher wakes the first idle server, exactly as the battery's
  `RunMMc` does, and the builder's pool passes the same Erlang-C check (`TopologyMultiServerTests`).
  A one-server station keeps its node id as its server id, so no existing seed or result moves.
- **Every station takes its work from a queue (D3, corrected 2026-11-25).** D3 said a pool needs a
  queue in front and `servers: 1` keeps working unqueued. The second half was never true over MCP: a
  `SimpleServer` refuses a push (`PushAllowed` is false), the builder ignored the `false` that
  `ConnectTo` returned, and a source or station wired straight to a station built without error and
  delivered nothing (SIM-135). Allowing the push is no fix either: a busy server gathers what is
  pushed meanwhile and starts its next job with all of it as one batch, of which `createProduct`
  keeps the first. So a push into any station is refused, naming the queue to insert.
- **A shortest-queue router counts the items waiting plus those in service behind each queue
  (2026-11-25, step 4).** Counting only the waiting items made a queue in front of a busy server and
  one in front of an idle server tie, the tie went to the first, and an item waited while a server
  idled: two equal stations got 56 % and 44 % (measured). With the in-service count they share the
  work evenly. Ties still go to the connection listed first, so the routing stays deterministic.
- **Router weights live on the connection, not the node.** A weight describes an edge, and putting
  it there keeps the node parameter bag free of nested maps.
- **The router is a new template rather than an extension of `SimpleRejectServer`,** because that
  class draws its routing decisions from a `System.Random` outside both the model seed and
  `Model.Reset` (SIM-99). Inheriting from it would inherit the defect.
- **Entity classes, `by_attribute` routing and time-varying arrival schedules are out of v1 and
  filed** (SIM-100, SIM-101) rather than half-built. Nothing in the product's own walkthrough needs
  them, and arrival schedules additionally invalidate the steady-state warm-up assumption SIM-63's
  replication runner makes — that is a design question, not a parser.
- **One catalogue is the source of truth for the vocabulary,** read by the builder, `get_schema` and
  `list_templates` alike, with a test asserting they agree. A client LLM self-correcting against a
  schema that has drifted from the parser is worse off than one with no schema at all.

---

## FSIM-03 — glass code extracted to furkansim (2026-06-18)
- **simopt is framework-only and PUBLIC.** Ivoclar/glass-ceramics-specific code, calibration and
  customer data live in the private `IvoclarR-D-AIOrg/furkansim` and are never mirrored to simopt:
  confidential Ivoclar production data and customer-specific topology — real station names,
  timing, operators — cannot live in a public repo. The glass base-mass digital twin and optimizer
  moved there (simopt b1c2fc8, furkansim f573ee5) before any calibration against real data.
- **Only the glass code moved.** The 11 `Glass*.cs` (namespace kept as `SimOpt.Glass` to make the
  move mechanical) left `SimOpt.Ivotion`; the Ivotion denture-packing domain **stays public** (D-04
  names the trigger that would change that).
- **furkansim consumes the framework by sibling ProjectReference** into `../simopt/src/SimOpt.*`
  (both repos checked out side by side), chosen over a submodule or NuGet packages for solo-dev
  simplicity and instant rebuilds. It reuses simopt's generic viz primitives (`SimulationCanvas`,
  `VizTopology`, `AutoLayout`, `ViewModelBase`).

---

## Optimization Showcase (Ivoclar Ivotion follow-up demo)

**Decided:** 2026-04-24 — after a successful spontaneous customer demo
**Plan:** `docs/plans/ivotion-optimization-showcase.md`

### Strategy lineup
Ship all four strategies: Random, Evolutionary, Particle Swarm and Sweep (exhaustive enumeration).
The 216-combination search space makes both iterative and exhaustive search viable, and showing all
four shows the framework's breadth.

### Multi-objective approach
A true non-dominated set (trade-off curve), NOT a scalarized weighted sum: the owner chose the more
compute-expensive but more honest approach when offered the trade-off.

### Cost model (US defaults, configurable)
- Roland LEF capital: $30k → $3/hr amortized over 5 yr × 2000 hr/yr
- Skilled production operator: $32/hr fully loaded
Owner-confirmed US numbers for the demo audience; the operator wage is exposed in the UI for
what-if exploration.

### Operator parallelism model
`effective_service_time = base_service_time / operator_count` — simple, honest, demoable. A real
`ResourceManager` operator pool with shared queues is a v3 enhancement, deferred; disclose it if asked.

### "Apply best solution to viz" semantics
Load the topology with the optimized parameters but do NOT auto-start; the user presses Space when
ready, which gives the presenter a moment to point at the new layout before motion starts.

### UI default objective
`MinimizeCostPerPiece` (SIM-37 Phase B, 2026-04-24). `MaximizeThroughput` is monotone in operator
count and Roland count over the 216-combo space, so the EA converges in generation 1 and draws a flat
fitness line that makes the optimization look trivial; cost-per-piece creates a real
throughput-vs-labor trade-off. All five objectives stay selectable; this is only the default.

### SkiaSharp native/managed pin (Avalonia + ScottPlot)
A csproj that uses `ScottPlot.Avalonia` (managed SkiaSharp 3.119.0) alongside `Avalonia.Skia`
(which transitively pins `SkiaSharp.NativeAssets.Linux` at 2.88.9) needs an explicit
`<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.119.0" />`, or Linux startup
crashes with `native libSkiaSharp 88.1 incompatible with managed SkiaSharp [119.0, 120.0)`. Keep the
pin while Avalonia 11.3.x is in use; revisit on an Avalonia upgrade.

### Charting library
ScottPlot.Avalonia for live fitness curves and trade-off plots: mature, fast, simple API, free for
any use, lighter than LiveCharts, less work than hand-rolled Canvas drawing.

---

## RolandPrinter Domain Class (built spontaneously during customer demo)

**Decided:** 2026-04-24

### Emit semantics
One representative entity per completed batch, not all N: the batch-cycle relationship keeps the
throughput math honest, and per-piece fan-out needs invasive surgery into the base Server's event
pipeline. Fan-out is deferred to SIM-44.

### Inheritance vs composition
Subclass `SimpleServer` rather than wrap it: it inherits all the StateMachine event plumbing, and the
only additions are `checkMaterialComplete` (batch threshold) and the `BatchSize` / `PerPieceTime` /
`CycleTime` properties.

---

## Spontaneous Customer Demos as the Killer Pitch

**Established:** 2026-04-24 (validated against a real Ivoclar colleague)

The framework's strongest selling point is building a working production-line model DURING a
meeting from cycle times the customer states verbally: live-build the topology preset (~3–5 min),
run it in the viz, point at the bottleneck, and if the customer mentions a domain-specific quirk
(batching, etc.), write, test and ship a domain class for it on the spot.

This is a deliberate capability to maintain: keep the topology DSL declarative, keep the Server
template extensible, keep test scaffolding ready to validate spontaneous additions.

---

## Agent-Driven Sim-Opt Positioning

**Decided:** 2026-04-24

SimOpt is not aimed at enterprise sim-opt with rigid data contracts and PhD-level expert operators.
It targets **non-experts using Claude Code or agent fleet with the SimOpt skill** to build
preliminary or even final simulation-optimization systems — production use, research pre-study,
and throwaway what-if analysis.

### Consequences

- **No standardized import schemas.** CSV/JSON import layers re-introduce the expert-configuration
  problem; when a user has a spreadsheet, the LLM agent reads it and parameterizes the builder
  directly. This retired SIM-39; resist the urge to re-add it.
- **Topology DSL stays declarative and easy to emit:** anything an LLM assembles is one flat
  JSON/record-style object, not a multi-step builder API with hidden required calls.
- **Domain classes (like `RolandPrinter`) belong with the skill's memory, not as customer-facing
  options.** The agent recognizes "it batches 15 pieces at a time" and picks the class; avoid
  "pick your server subtype" UX.
- ~~**Validation lives in tests, not in constructor parameter matrices.** A fast test suite saying
  whether an unusual topology runs to completion is cheaper than schema validation and catches the
  failures that actually matter.~~ **Superseded 2026-11-25 (MG, Q-VALID)** by D-02's validation layer (UN-008) and
  the SIM-65/66 design: a published schema, `validate_model` with every fault at once, structural lint, offered load
  and a trial run.
- **The MCP server (SIM-19/20) is first-class surface area,** not a side feature: it is how the
  agent reaches the framework.

---

## Agent-Driven Sim-Opt Positioning (continued)

### What this does NOT change

- Core engine invariants (discrete-event only, Server→Buffer patterns, etc.) remain authoritative;
  agent flexibility stops at the framework's structural guarantees.
- Test-driven discipline stays mandatory, even for a throwaway scratchpad: tests are faster than
  debugging and double as "this is what I built" evidence.

---

## FMT Architectural Validation Gridworld

**Decided:** 2026-05-29. **Plan:** `docs/plans/2026-05-29-fmt-gridworld.md`

### Sub-project placement
Lives inside SimOpt, not a separate repo: SimOpt gains agent-based gridworld as a new simulation
paradigm, and FMT gets SimOpt's math/stats/stochastic infrastructure for free.

### Two-project split
`SimOpt.GridWorld` (generic agent-based engine, reusable beyond FMT) + `SimOpt.FMT` (FMT-specific
architectures, depends on GridWorld).

### .NET/C# over Python
The spec recommended Python + gymnasium; the owner chose .NET to keep everything in the SimOpt
ecosystem. Reservoir computing, Q-learning and grid simulation are straightforward in C#.

### Topology-agnostic architecture
`ITopology<TCoord>` with `Coord2D`/`Coord3D`/`HexCoord`; agents generic over `TCoord`; convenience
`Grid2D`, `Grid3D`, `HexGrid`. Richer connectivity (hex, 3D) strengthens the causal-structure vs
flat-association distinction for FMT experiments.

### MoveTo/Reset separation
`MoveTo(TCoord)` for tick-by-tick movement, `Reset(TCoord)` for episode initialization, because
using Reset for movement would corrupt any agent that clears learned state on reset.

### CellInfo metadata for causal mechanisms
`CellInfo(Type, HazardFamily, CausalMechanism)` stored sparsely alongside `CellType`, enabling FMT
Prediction 1 (causal transfer): lava (thermal) and deep water (submersion) share the death outcome
but differ in mechanism.

### Terminal reward before death
`OnStepComplete(HazardReward)` is called before `OnDeath()` in the same tick, so RL agents receive
the terminal transition signal.

### Environment meta-optimization (SIM-53)
Use SimOpt's own IProblem/IStrategy to find the environment configuration that maximizes effect
size between FMT and comparison agents, constrained by simpler-agent viability. The optimizer
optimizes the environment, not the agent.

---

## 2026-06-18 — Glass production-line digital-twin demo

- **Public-repo content boundary:** simopt is a public GitHub repo, so customer/pilot
  specifics never go into committed source — new demo code uses generic "Glass" naming.
  Tracked+public files (decisions, backlog, pending, next-session-task) stay generic;
  gitignored files (session-context, session-log, session-history) may hold specifics.

---

## 2026-07-05 — Critical-subsystem code review (Opus 4.8, 3 parallel agents)

- **Scope = the 3 correctness-critical cores only** (DES engine, optimizer+coupling, math/stats),
  because those are the places where a silent bug poisons every result.
- **Verdict:** the kernels are healthy; the correctness debt sits in the **stochastic layer** and
  the **strategy↔solution operator seam**. Findings and fixes: `docs/2026-07-05-critical-code-review.md`;
  tracked SIM-56..60.
- **PSO status clarified:** `ParticleSwarmOptimization` is a real implementation (the `[Obsolete]`
  `SwarmingAlgorithm` is the stub), but coupled via reflection to a settable `double[] Parameters`
  that only `TestSolution` provides → zero search on real problems. SIM-36's "PSO is a stub" note
  needs reconciling.

---

## Commercial pivot — the four founding decisions (2026-08-25)

Owner decisions taken by MG in one sitting on two independent review passes
(`docs/commercial/2026-08-25-architecture-readiness-review.md`,
`docs/commercial/2026-08-25-business-strategy.md`). They settle the open needs UN-031 and UN-032
and govern everything downstream.

### D-01 · Licensing: FSL-1.1-ALv2 on the engine, proprietary on the app

**Decided.** The public repository is licensed **FSL-1.1-ALv2** (`LICENSE.md`). The desktop
application, its embedded assistant and the commercial infrastructure are separate works in a
separate private repository, and are not covered by it.

- **Why FSL rather than Apache-2.0:** the engine is the trust layer that the whole product claim
  rests on, and a one-person shop cannot outrun a fork. FSL blocks *competition* while permitting
  everything else.
- **The accurate reading the decision stands on:** FSL permits **any purpose except a Competing
  Use**, so commercial internal use and consultants' client work are free (it is not PolyForm's
  "free for non-commercial, paid for commercial" model). The revenue therefore has to come from the
  closed application layer, not from licensing the engine.
- **The two-year Apache conversion is a feature, not a concession:** it keeps the dissertation
  lineage citable, inspectable and eventually fully open, the cheapest credibility available to
  this product.
- **Provenance:** closed by D-05 — the code is MG's own, no institutional claim (SIM-85).

### D-02 · Model access: local-first, cloud optional

**Decided.** The default path is a **locally-hosted model** (Ollama / LM Studio). Commercial
providers remain fully supported but are opt-in, not the happy path. Resolves UN-032.

**Why:** bring-your-own-key assumed an API key that the primary persona — a plant engineer at an
SMB manufacturer — does not have and will not obtain. Local-first makes confidentiality a headline
feature (a factory's layout never leaves the building, which no cloud-based competitor can match in
EU manufacturing) and gives zero marginal inference cost to both sides.

Consequences, all of which are requirements rather than options:

- **Constrained decoding** to the JSON schema, so topology output is **syntactically valid by
  construction** — a capability local inference has and several cloud APIs do not. SIM-83.
- **The honest risk:** a small local model is materially weaker at *inferring* a correct model from
  a vague description, even when its JSON is well-formed. So the read-back-and-confirm loop (UN-002)
  is load-bearing, and the validation layer (UN-008) must catch semantic nonsense the model cannot.
- **A stated hardware floor** (roughly 8–16 GB of VRAM or comparable system memory), checked at
  first run rather than discovered at first failure, with a cloud fallback for machines below it.
  SIM-82.
- **A named reference model is a product artefact:** "works with local models" is not a claim until
  a specific model passes a specific benchmark. SIM-84, which is also the Month-3 kill-gate
  measurement.

### D-03 · Backlog priorities as proposed

**Decided.** SIM-62/63/64/68/69 at P0; SIM-67/70/71/79/80 at P1; remainder P2. No changes
requested.

### D-04 · Employer boundary handled before revenue, not after

**Decided.** Document the dissertation provenance, obtain a written Nebentätigkeit acknowledgment
worded generically ("simulation software", no dental specifics), and do not lead the public
positioning with dental. Tracked as SIM-79. D-05 closed the provenance and employer-boundary parts
(SIM-85, SIM-79); the positioning part stands.

**Reconciled against FSIM-03, which is NOT reversed.** FSIM-03 moved the *glass* code out because
it carried real Ivoclar station names, timings and operators, and kept the **Ivotion
denture-packing domain public** because it carries no confidential data. The business review's
"keep Ivotion material internal" is about *marketing positioning*, not code location. Both hold
simultaneously:

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
- **SIM-79 (employer boundary): CLOSED, and on a stronger footing than the one proposed.** MG holds
  an existing, separately constituted professional practice as a **gerichtlich beeideter
  Sachverständiger**, and software he creates falls within it. SimOpt is a tool of that practice,
  not a new venture beside the employment. Commercialisation is an extension of an existing
  business, exercised at his discretion.
- The conflict-of-interest framing in `docs/commercial/2026-08-25-business-strategy.md` §D assumed
  the ordinary employee-side-business structure and is therefore **superseded**.

### What this reveals — the origin use case was never recorded

The product exists to answer a forensic question: *how did this computer system behave when it met
the real world, and can that be demonstrated?* Its requirements were added as UN-033..UN-036.

**The forensic bar is strictly higher than the commercial one.** Everything Slice 0 fixed — a
random source that honours its contract, a triangular sampler that is not silently truncated, a
reset path that does not diverge between runs, a seed derivation that survives a process restart —
is precisely what an opposing expert would attack first. Reproducibility (UN-009) stops being good
engineering practice and becomes an evidentiary property: a result that cannot be reproduced on
another machine, months later, by someone else, is not evidence.

The beachhead question this raised is answered by D-06.

---

## D-06 — Beachhead resolved: forensic as proving ground, manufacturing as the public face (2026-08-26)

**Owner decision, 2026-08-26.** Presented as SIM-87, answered: **option C, sequenced B-then-A.**
Raised by D-05's disclosure that the product's origin use case is forensic — an instrument of MG's
Gutachtertätigkeit as a gerichtlich beeideter Sachverständiger — which was not on the table when
the 2026-08-25 beachhead was chosen.

### What was decided

**Forensic casework is the proving ground now. Manufacturing remains the public positioning.
A deliberate forensic go-to-market is revisited only once the engine has survived real case use.**

Concretely:

| | Effect |
|---|---|
| Public positioning, landing page, content pipeline (SIM-77, SIM-78) | Unchanged — general discrete manufacturing via independent consultants. |
| Proving ground / customer zero | MG's own casework, starting immediately. It costs nothing extra because that work is happening anyway. |
| Month-3 kill gate | Unchanged as written (chat→correct-model ≥50% unassisted). Real case use is an *additional* signal, not a replacement gate. |
| Slice 7 (SIM-86 provenance, SIM-88 tamper-evidence) | Retained and justified — they serve the proving ground directly, and they strengthen the commercial deliverable independently. |
| Evidentiary claims in marketing | **None, until the admissibility basis is researched.** No public material may state or imply that SimOpt output is court-admissible. |

### Why this shape rather than a switch

The forensic market scores better on nearly every axis that matters to a solo owner with a few
hours a week — existing standing, existing network, far higher willingness to pay, essentially no
competition, owner is customer zero, and what it demands (correctness, reproducibility) is exactly
what Slice 0 built. But it is genuinely small, relationship-led rather than product-led, and cannot
be sold by an agent writing LinkedIn posts. Sequencing runs the fastest validation loop immediately
at zero marketing cost, defers the repositioning cost until there is evidence to justify it, and
makes no evidentiary claim before its basis exists.

### What this obliges

1. **Use it on real casework.** The decision is only worth what the proving ground actually
   produces. A session that reports "no case use yet" three months running has falsified the
   premise, not merely failed to act on it.
2. **Do not re-litigate the beachhead** on the strength of the forensic origin alone. That
   argument has now been made and answered.
3. **Research admissibility before any claim.** Unpriced work, deliberately unscheduled — it
   becomes urgent the moment marketing wants to say the word "evidence".

Closes SIM-87. Supersedes the "Open strategic question" section of D-05 (now in
`docs/decisions-archive.md`).

---

## 2026-08-26 — Slice 1 engineering findings (SIM-63, SIM-89, SIM-91)

Recorded as rationale rather than as rules. Mechanical detail: `backlog.md`, the commit messages
and `docs/plans/2026-08-26-sim-63-statistics-subsystem.md`.

### A catch-all that turns exceptions into data hides total breakage

`SimulationTools` wraps every call in a catch-all that serialises the exception into an `error`
field, so the MCP head — which had never been able to build or run a single model (SIM-89) —
returned well-formed JSON that reads, to a caller, exactly like a working tool rejecting a bad
model.

**Consequence adopted as practice:** any test against a surface that converts exceptions into data
must assert on the **absence** of the error field and on a real post-condition — a non-zero count, a
changed state — never on the call completing or the payload parsing. And the absence of any test
against a surface is itself the finding.

### Two wrong answers that disagreed, not one wrong answer

SIM-91's first-run divergence had two wrong behaviours and no right one, and their disagreement was
the only reason either bug was visible; each alone would have been a plausible-looking number that
survived review indefinitely. When two runs of the same thing disagree, do not just decide which
figure is right: the correct answer is often a third one.

### Statistics belong to the engine, and there were three of them

**Adopted:** the engine owns every reported measure; the UI displays them and computes none
(the UN-018 presentation boundary). The three disjoint implementations found (visualization,
Ivotion, MCP payload) were none time-weighted, warm-up-truncated or replicated, and several were
wrong rather than imprecise — inventory: `docs/2026-08-26-statistics-inventory.md`. Utilisation
sampled once per render tick is not a precision compromise: its answer moves with the frame rate
and it is unreachable from a headless run.

### Where "defensible" actually bites

Three decisions in the statistics subsystem were taken specifically because the product is used as
expert evidence, and each cost more than the obvious alternative:

- **A t-quantile by root-finding rather than a lookup table.** A table is silently wrong at every
  degree of freedom it does not contain. The table is kept as a regression test *against* the
  root-finder, since a root-finder with no pinned values is merely untested.
- **Fixed warm-up truncation over MSER-5 or Welch.** Both are better statistics, but both make the
  truncation point a data-dependent random variable or a human judgement on a plot, and "W was
  declared in advance and is printed in the provenance record" is stronger testimony than either.
- **A null half-width at one replication, never zero.** Zero reads as infinite precision. It is the
  single most dangerous number the subsystem could emit.

## 2026-08-26 — the analytic battery (SIM-64), and three rulings

Every measured number: `docs/plans/2026-08-26-sim-64-analytic-battery.md`.

### A 95% interval is the wrong thing to gate a build on

Fourteen containment assertions at 95% each fail at least once for a **correct** engine on fresh
seeds about half the time (0.95¹⁴ ≈ 0.49), and every legitimate change to the random number stream
re-rolls all fourteen.

**Adopted:** the reported interval stays at 95% and is asserted to be *narrow* (half-width ≤ 5% of
the analytic value); containment is gated at 99.9%, where the fourteen pass together 98.6% of the
time. Nothing is loosened except the threshold at which the build goes red, and the tightness
assertion is what stops a high-variance engine hiding inside a wide interval.

### Means cannot see a queue discipline, and this was measured rather than argued

Switching every queue from FIFO to LIFO leaves the engine work-conserving, so every mean-based
assertion still passed; only the M/M/1 sojourn-distribution check failed.

**Consequence:** a benchmark suite made only of closed-form *means* is not a verification of a
discrete-event engine, however many systems it covers. At least one whole-distribution check is
structural, not a nicety.

### A goodness-of-fit test on autocorrelated data is not a goodness-of-fit test

Consecutive sojourn times from one run are heavily autocorrelated (about 82 customers' worth at
ρ = 0.8) and a KS test assumes independence, so run naively it rejects a correct engine. The check
therefore runs at ρ = 0.5 (autocorrelation time ≈ 10) and keeps one completion in 50. The
distributional claim is exact at every stable ρ, so the weaker-looking design gives up nothing.

### Owner rulings, 2026-08-26

- **The Ivotion optimisation surface is fixed, not retired** (SIM-93, P1). Its tab is currently
  unreachable from `MainWindow.axaml`, so the KPIs are repaired on a surface nobody can open today
  — accepted deliberately, so that re-enabling the Ivoclar demo is a wiring change rather than a
  rebuild against engine statistics that will have moved again by then.

## 2026-08-26 — SIM-81, and what a conformance fix is allowed to claim

The class named `MersenneTwister` was not one (non-reference seeding, no tempering); it is now the
reference algorithm, pinned to the published seed-5489 vector. Every measured number:
`docs/plans/2026-08-26-sim-81-mersenne-twister.md`.

**Adopted:** describe this as a **conformance fix, not as the repair of a wrong answer.** The
first-block defect is real and large, but at n = 1,000,000 both generators pass uniformity and the
whole analytic battery passes with either. A product sold partly on the defensibility of its numbers
cannot ship a class that claims a named, published algorithm and implements something else — that
is the argument, and it is sufficient on its own. An inflated claim of measured improvement would be
the more damaging thing to have written down, because it is the kind of statement an opposing
expert can check.

**Open, against SIM-96:** the old generator's mean error across the battery barely moves between
seed sets while the new one's varies as sampling noise should. A quantity that ought to fluctuate
and doesn't suggests a systematic component — plausibly cross-stream correlation, since per-node
seeds are XOR-derived from a single base seed. Four seed sets cannot settle it; it stays open rather
than being resolved by assertion in either direction.

**Ruling addendum, same day:** **SIM-98 raised to P2** (proposed P3). The other three random sources
carry no reference vector and no audit of any kind; SIM-81 established that "passes the contract
tests" is compatible with not being the algorithm on the label, and that argument applies to them
unchanged. Not P1 only because none of them is the default.

## 2026-11-03 — a downstream integrator's platform (SIM-105), and how we work with it

A colleague's private manufacturing platform embeds SimOpt, built unchanged at a pinned commit. He
shared it with the owner. This repository is public and his is private, so nothing here names him
or his repository.

**Knowledge split (owner, verbatim):** *"generic knowledge which we could also have gotten from
literature into public agent fleet, too specific things into cfg."* Literature-level
manufacturing-simulation knowledge was staged in `docs/staged/` for the public fleet template; it now lives in
the fleet (`global/knowledge/`, CFG-894), and simopt's copies were deleted on 2026-11-25 (Q-STAGED).
Anything specific to his platform stays out of this repository and goes to the fleet's private
configuration.

**We contribute (owner):** *"post issues to his repo where you found or will find any. we
contribute. he is a friend."* Defects found in his code are reported to him as issues. Defects his
work exposed in SimOpt are reproduced here independently, fixed test-first and filed as our own
items (SIM-106, SIM-115..120). His contributed tests and patch inform our fixes, and his code is
not copied into this repository.

**Visualization target:** SimOpt's viewer is to reach the level of his flow map "in general". The
plan (`docs/plans/2026-11-03-sim-105-visualization-uplift.md`) reimplements ideas, not code, under
UN-018's rule that the viewer presents and never computes a reported measure.

**Later, not here:** a joint solution with him for Ivoclar's use cases belongs to the ivoclar
project, not to this public repository.

## 2026-11-25 — compete as simulation-optimisation, close the SimPy gap first (SIM-122)

**Positioning.** SIM-122 measured SimOpt against SimPy 4.1.2. As a modelling library for Python
programmers SimOpt does not win: SimPy is MIT, widely used, faster on a small hand-written model,
and any behaviour is a short process function. As simulation-optimisation with sound statistics that
agents can drive, SimPy is not in the race: it has no random-number management, no intervals and no
optimiser, and a downstream integrator rebuilt all three by hand on top of it. Precision is equal
once the engine is correct (±1.9 % vs ±2.0 % on the same model). So SimOpt competes on that layer.

**Owner ruling (MG, 2026-11-25):** *"these are top priority"*, for the six gaps named in the SIM-122
brief: process-style modelling (SIM-126, which resolves Q-PROC as "plan it now"), a stock block
(SIM-127), events at the current instant (SIM-128), seed IDs by default (SIM-125), speed per event
(SIM-129) and a clean way in from Python over the SIM-65 schema and MCP (SIM-130). They head the
backlog above the commercial track. Build order by dependency: SIM-125, SIM-128, SIM-127, SIM-126,
SIM-129, with SIM-130 following SIM-65/66.

## 2026-11-25 (night) — speed target, process streams, order of work (SIM-129, SIM-126, SIM-65)

- **Target per event (SIM-129):** at most 250 ns per event for template models on the WSL box, measured
  by `benchmarks/SimOpt.Benchmarks`; time is machine- and load-dependent, so no test holds it. Bytes
  per event are deterministic and are held by `EngineAllocationBudgetTests` (260 for template models).
  Speed work must keep every result byte-identical; `EngineFingerprintTests` pins a hash of every
  result, and a change meant to move results updates those hashes in the same commit and says so.
  Evidence: `docs/plans/2026-11-25-sim-129-speed-per-event.md`.
- **A process's random streams are seeded by name** (`p.Random(name, distribution)`, the stable hash
  of the name on the process's seed), not by registration order as the model's own streams are
  (SIM-131). Why: a body makes its streams while it runs, and order-based seeds would tie each stream
  to the ones made before it and make a reset model draw other numbers than a rebuilt one.
- **SIM-65 before SIM-110.** The handover listed SIM-65 step 1 and then SIM-110; SIM-65 continued past
  step 1 because it is P0 and SIM-130 (P0, the Python way in) depends on it, while SIM-110 is P1.
  Unattended choice by the session; MG may reorder.

## 2026-11-25 (night, restart #3) — validation, experiments and the optimiser (SIM-66, SIM-68, SIM-69)

- **One rule set: the builder collects faults instead of throwing.** `validate_model` does not repeat the
  builder's checks; the builder reports every fault per node, connection, router and metric, and
  `create_model` registers only a clean build. Why: two rule sets drift, and a client fixing a model one
  fault per call wastes rounds (UN-008). Evidence: `docs/plans/2026-11-25-sim-66-validate-patch-experiment.md`.
- **A node wired to two nodes without a router is refused.** A source, station or delay pushes each item
  into every node connected after it, so such a model duplicated every item (shown by an engine-level
  test). A router chooses one branch.
- **Scenarios run at common random numbers, compared replication by replication.** `run_experiment`
  gives every scenario the same base seed, so a node a patch leaves alone draws the same numbers, and
  `significant` comes from the paired 95 % interval of the differences (UN-013).
- **An optimiser constraint is kept only when the metric's whole 95 % interval keeps it.** Measured on
  the clinic (mean exam wait under 15 minutes, one 8-hour day): on the mean, 5 replications passed 6 rooms
  (12.3 ± 8.0) that 20 replications failed (19.7 ± 6.6); on the interval both give 7. Evidence:
  `docs/plans/2026-11-25-sim-68-69-topology-problem-optimize.md`. Unattended choice; MG may prefer the
  mean as an option.
- **`optimize` offers auto, grid and random only until SIM-59 lands.** SA, EA and PSO carry SIM-59's
  defects (unguarded temperature loop, missing clones, PSO's reflection), so they are not exposed yet.
  *Superseded the same night (below).*

## 2026-11-25 (night, restart #4) — SIM-59 and SIM-68 step 4

- **`optimize` runs sa, ea and pso, and "auto" runs the EA when the space exceeds the budget.** D6 had
  planned SA. Measured under fixed replications with 15 search seeds, SA came last on every larger space
  and the EA was best or tied at the default budget with the best worst case (a 170-point space: optimum
  in 12 of 15 runs against random search's 2). Evidence: the table in
  `docs/plans/2026-11-25-sim-68-69-topology-problem-optimize.md`. Unattended choice; MG may prefer
  another default (Q-AUTO).
- **A solution's clone splits its move generator instead of copying it.** With a copied state, SA (which
  proposes every move on a clone of its unchanged current point) proposed a rejected move again and froze:
  2 distinct proposals in 300. Runs stay reproducible from their seed; D8 is revised accordingly.
- **A saved model is its topology file (SIM-67).** `save_model` writes one version as schema v1 JSON, the
  document `create_model` takes, so the file is portable and runs on any later server; the version history
  stays in the session. No automatic store: writing every model to a directory and reloading it at start
  would change what a plain server does on disk (Q-STORE). Unattended choice.
- **The Python package talks MCP itself (SIM-130).** About a hundred lines of synchronous JSON-RPC instead of
  the `mcp` SDK, so the package has no required dependency and no async API; pythonnet in process needs the
  .NET 10 runtime (measured) and is documented as the alternative. Unattended choice.
- **The EA's generation is sorted best first.** `ISolution.CompareTo` orders ascending, and the default
  parent selector takes the first µ, so the EA bred from its worst members. Fixed in the algorithm, not
  in the selector, because `ProcessGeneration` documents its input as best first.

## 2026-11-25 (restart #5) — standing rules kept from the commercial-track handoff

`docs/pending-commercial-track.md` (the work order of 2026-08-25) tracked SIM-63..69 and SIM-87, all
closed, and was deleted. Its standing rules for unattended work that still apply:

- **A review finding is a hypothesis until a failing test confirms it**, and a deterministic test beats a
  sampled one (the MersenneTwister defect was a 2⁻³¹ event no sampling loop would gate; extracting the
  pure mapping made it a boundary test).
- **Re-run the full suite after every change to a random stream; commit per backlog item**, with the
  finding and its confirmation in the message.
- **Expose nothing to users ahead of its correctness fix.** A sampler or feature wired into the schema
  before it is right industrialises the bug (SIM-65 found SIM-102 and SIM-103 this way).
- **The FMT/GridWorld track (SIM-47..54) is not touched from here**: crucible ported that domain to
  Python (SIM-61).
- The beachhead obligations (no claim of court-admissibility, no re-opening of the beachhead) stand as
  written under D-06 above. Its rule "no UI work before Slice 1 is green" is spent: Slice 1 (SIM-63, SIM-64)
  is done, and the visualization work is SIM-110.

## 2026-11-25 (morning) — four owner decisions after restart #6 (MG at the console, 06:5x)

- **Q-PROC-SCHEMA → a process node with a closed step list** (SIM-126's last part). `resource` and `stock` become
  nodes; a `process` node runs each item through `delay`, `seize`, `release`, `take`, `put` and `wait_until`; an unfed
  process repeats its steps beside the flow. No loops or conditions inside a process; an item leaving frees every unit
  it holds; patience on `seize`/`take` exits by a named outlet; step values are drawn on entry; schema version stays
  "1". Why: it keeps the offered-load pre-check, a plain read-back and SIM-83's grammar, which flowchart blocks and
  scripted processes would each lose part of. Brief: `docs/plans/2026-11-25-sim-126-process-schema.md`.
- **Q-VIEW-PATH → one model path through a shared library** (the rest of SIM-110). The topology layer moves out of
  the MCP server into `SimOpt.Engine.Api` (SIM-70's first half); every viewer model goes through the schema builder;
  SIM-72's reverse mapping is dropped; presets and furkansim's glass line become schema documents plus a layout
  overlay; the schema gains `normal` arrivals and a `batch` station. Accepted consequences: reject decisions follow
  the seed, a queue is drawn where a server fed a server, furkansim's glass-line numbers move (inbox item when it lands).
  Brief: `docs/plans/2026-11-25-sim-110-one-model-path.md`.
- **Q-IVO-STATS → 960 minutes (two shifts) with a 120-minute warm-up** as the Ivotion optimiser's defaults (SIM-93).
  The baseline then scores 10.71 pieces/h on all 20 seeds, off by at most one batch; each evaluation takes about eight
  times as long as the old 120 minutes.
- **Q-ENGINE-MOVES → SIM-117, SIM-95 and SIM-119 wait for the colleague's SIM-122 results.** They change runs with
  failures or antithetic sampling, the cases his comparison measures; fixing them first would leave a rerun at the
  latest commit differing for two reasons at once.
- **Q-PROV-REPORT → a Markdown appendix** (SIM-86 slice C): a `report_run` tool renders a run record as Markdown,
  English or German by argument (provenance table, what was assumed, the limits of schema v1, results with intervals,
  how to repeat it); MG turns it into PDF or Word in his own toolchain. No rendering dependency in the server.
- **Q-AUTO → the EA stays** what `optimize`'s "auto" runs on spaces larger than the budget (measured best or tied at
  the default budget with the best worst case; D6 had planned SA, measured last).
- **Q-STORE → not now.** `save_model`/`load_model` cover UN-005; a model store is revisited with the desktop app
  (SIM-71).
- **Q-SRC-START → first item at t = 0 on both paths** (SIM-139): a freshly built model matches what every replicated
  and MCP run already does after its reset, so no replicated result and none of his frozen runs move.
- **Q-PRIO → accepted as proposed**: SIM-122 P2, SIM-123 P1, SIM-132 P2, SIM-133 P2, SIM-134 P1, SIM-135 P1, SIM-136
  P2, SIM-137 P1, SIM-138 P3, SIM-139 P1, SIM-140 P2.
- **Q-PRIV-RULE → private material is filed as a cfg inbox attachment** (SIM-123, Meta-Rules consent given). cfg adds
  `inbox-file.sh --attach <file>` (copies into `cross-project/attachments/<project>/`); once it ships, simopt's
  `.claude/CLAUDE.md` gains, after "SimOpt owns simulation expertise": *"**Private material leaves the same session.**
  File every note, review or index you write about a colleague's private material as a cfg-agent-fleet inbox attachment
  in the session you write it, never into `tmp/` or any other untracked path."*, with two edits that remove the
  conflicts: "stage to `tmp/staged-*.md`" becomes "stage it to `docs/staged/`", and the `tmp/` line becomes "`tmp/`:
  gitignored and swept after 14 days; it holds only re-obtainable copies of third-party material, such as a colleague's
  repository for SIM-105."
- **Q-STAGED → simopt's staged copies deleted**; the fleet copies (byte-identical) are canonical.
- **Q-CIW → filed as SIM-141 at P3.**
- **SIM-105 carry-overs, all as recommended:** **Q-SFM9** → §9 "Role-based views" of the shopfloor file stays
  public (re-verified: already in the public agent-fleet template; ordinary dashboard practice, phrased generically).
  **Q-PRIVHOME** → the private colleague material stays in cfg (`cross-project/simopt-private/`), confirming
  2026-11-03. **Q-FMT** → the FMT Gridworld entry stays live until SIM-47..54 are triaged. **Q-VALID** → the
  "validation lives in tests" bullet is superseded (marked in place).
- **Q-ASYNC → iterators only** (SIM-126): processes are written with C# iterators; an `async` front end over the
  same waits stays possible later.

## 2026-11-25 (morning, after the decisions) — implementation choices the briefs left open

- **A step's `amount` has no default** (SIM-126): `take` and `put` say how much, so no step field is ever "assumed" in a
  run record.
- **A process nothing feeds that repeats its steps 100,000 times at one instant stops the run** naming itself
  (`ProcessNode.MaxCyclesPerInstant`, SIM-126 slice 4): rule 4's "a step that can wait" holds for every valid list, so
  only a runtime guard keeps `validate_model` from hanging the server on a `[seize, release]` loop.
- **`batch: n` is the time of the whole batch, on one server, and passes every item on** (SIM-110 step 2): the Roland
  printer it replaces passed one representative per batch; several servers are refused because a part-filled batch
  would compete for arrivals; `batch` has no listed default, so no model's `assumed` list changed.
- **`STOCK_RUNS_OUT` kept** after measuring it on the car wash (reorder level 5, warned: empty 0.199 of the day; 20, not
  warned: 0.0046).

