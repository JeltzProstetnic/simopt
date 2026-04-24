# SimOpt Decisions

Topic-organized record of important decisions and design rationale.
NOT a rule sheet (that's CLAUDE.md). NOT session state (that's session-context.md).

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

### What this does NOT change

- Core engine invariants (discrete-event only, Server→Buffer patterns, etc.)
  remain authoritative. Agent flexibility stops at the framework's
  structural guarantees.
- Test-driven discipline stays mandatory — even when the agent is
  assembling a throwaway scratchpad, it writes tests. Faster than
  debugging, and the tests double as "this is what I built" evidence.
