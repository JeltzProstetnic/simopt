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
