# Ivotion Optimization Showcase — Plan

**Audience:** Ivoclar colleague (Bartl R&D peer-level), follow-up demo next week.
**Today's outcome:** v1 viz preset + dedicated `RolandPrinter` class. Colleague impressed; will bring real data and wants to see optimization.
**Goal:** Deliver a credible multi-objective sim-opt demo with switchable objectives and a UI that lets the user (and future user) drive it.

---

## Decision Variables (degrees of freedom)

| Variable                | Range / Type           | Notes                                                    |
|-------------------------|-----------------------|----------------------------------------------------------|
| `roland_count`          | {1, 2}                | Each costs $30k capital                                  |
| `operators_inspect`     | {1, 2, 3}             | Operator parallelism at inspection                       |
| `operators_pack`        | {1, 2, 3}             | Operator parallelism at packing                          |
| `operators_label`       | {1, 2}                | Smaller search range (already fast station)              |
| `operators_ssb`         | {1, 2}                | Smaller search range (3s/pc, rarely a bottleneck)        |
| `roland_batch_size`     | {10, 15, 20}          | Three batch sizes — affects WIP and changeover           |

Total search-space size: 2 × 3 × 3 × 2 × 2 × 3 = **216 combinations**.
Small enough to enumerate exhaustively in seconds → enables a "sweep" mode AND iterative EA mode.

## Objectives (switchable)

| Objective       | Direction | Formula                                                      |
|-----------------|-----------|--------------------------------------------------------------|
| Throughput      | maximize  | pieces shipped / sim hours                                   |
| Total cost      | minimize  | (capital_per_hr × roland_count) + (labor_rate × Σ operators) |
| Labor hours     | minimize  | Σ operators × shift_hours                                    |
| Floor space     | minimize  | Σ node footprint area (m²)                                   |
| Cost per piece  | minimize  | total_cost_per_hr / throughput_per_hr                        |
| Weighted blend  | maximize  | normalized linear combo, user-set weights via sliders        |

**Cost assumptions (US, demo defaults):**
- Roland LEF capital: $30k → amortize over 5 yr × 2000 hr = **$3/hr per machine**
- Skilled production operator: **$32/hr** fully loaded (US-typical)
- Floor space: opportunity cost ignored in baseline; reported as raw m²

## Operator Parallelism Model (simplification)

Each manual station's effective service time = `base_service_time / operator_count`.
Real model would use a `ResourceManager` with M operators sharing a queue, but
for demo this is honest enough — captures "more bodies = faster line" without
wiring resource pools. Disclosable as a v3 enhancement.

## Multi-Roland Topology

When `roland_count == 2`:
- Two `RolandPrinter` nodes in parallel after the print queue
- Print queue connects to both (existing parallel-server pattern from `ParallelServers` preset)
- Both connect to the packing buffer

Layout: stack vertically — `roland_a` at y=3, `roland_b` at y=7.

## Architecture

```
SimOpt.Optimization (existing) ──┐
                                 ├──► IvotionProblem (new)
SimOpt.Simulation (existing) ────┘
                                          │
                                          ├── builds dynamic VizTopology
                                          ├── runs simulation, collects KPIs
                                          └── computes fitness per selected objective

SimOpt.Visualization (existing) ──┐
                                  ├──► OptimizationPanel (new XAML view)
                                  │       ├── objective selector
                                  │       ├── strategy selector (Random/EA/Sweep)
                                  │       ├── live fitness chart (ScottPlot.Avalonia)
                                  │       ├── best solution display
                                  │       └── "Apply to viz" button
                                  └──► IvotionTopologyBuilder (parameterized)
```

## Charting Library

**Choice:** `ScottPlot.Avalonia` (NuGet). Mature, fast, simple API. Free for any use.
**Why not LiveCharts:** heavier, slower setup, more dependencies.
**Why not hand-roll:** time cost outweighs the dependency.

## Phases & Deliverables

### Phase A — Core problem (no UI) — TDD
- [ ] `IvotionProblem.cs` (implements `IProblem`)
- [ ] `IvotionSolution.cs` (implements `ISolution`, `ITweakable`, `ICombinable`)
- [ ] `IvotionTopologyBuilder.cs` (parameterized topology factory)
- [ ] KPI extraction from `SimulationModel` (throughput, cost, labor, space)
- [ ] Multi-Roland wiring support in `SimulationModel.CreateEntity` (already partially there)
- [ ] Tests:
  - Decision variable validation
  - `Evaluate` produces consistent fitness for known seed
  - `roland_count == 2` produces higher throughput than 1 at high arrival rate
  - Each objective returns sensible value bounds
  - EA over IvotionProblem improves best-fitness across generations

### Phase B — Optimization UI
- [ ] Add `ScottPlot.Avalonia` NuGet to Visualization project
- [ ] New `OptimizationPanel.axaml` view, accessible via menu / "O" key
- [ ] Objective selector (radio buttons with weights for blended)
- [ ] Strategy selector dropdown (Random Search / Evolutionary / Sweep)
- [ ] Iterations / generations / population sliders
- [ ] Run / Stop buttons
- [ ] Live line chart: best-fitness vs iteration
- [ ] Best solution display: parameter values + KPIs side-by-side
- [ ] "Apply to viz" button: rebuild Ivotion topology with best parameters and reload main canvas

### Phase C — Polish (closer to demo)
- [ ] Pareto plot view for cost vs throughput tradeoff
- [ ] Baseline-vs-optimized side-by-side viz
- [ ] CSV export of all evaluated solutions
- [ ] Demo cheatsheet refresh

## User decisions (locked in)

1. Operator wage → exposed in UI (slider or numeric input)
2. Strategies shipped: **all four** — Random, Evolutionary, Particle Swarm (full impl, not stub), Sweep
3. Multi-objective: **true trade-off curve** (set of non-dominated designs); machine can afford the compute cost
4. "Apply best solution to viz" → load topology only, do NOT auto-start. User hits Space when ready.

## Phase D — high-value additions (post-core, before demo)

These were proposed and approved as part of "maximize value":

- **CSV import for real cycle times** — colleague brings data next week. Schema:
  one row per station with cycle_time_auto, cycle_time_manual, [optional]
  arrival_rate. Live recalibration on load.
- **Replications with error bars** — every fitness eval = N=10 sim runs with
  different seeds; chart shows mean ± std. Default N configurable.
- **Constraint handling** — UI panel to set hard limits (max capital, max
  floor m²). Solutions violating constraints excluded from results.
- **Bottleneck heatmap overlay** — per-station utilization rendered as
  color (green ≤60%, yellow 60–85%, red >85%) on the live topology.
- **ROI calculator panel** — input current vs proposed config + monthly
  demand forecast → output payback period in months and 5-year NPV delta.

## Phases (revised)

### Phase A — Core problem (no UI) — TDD
(unchanged: `IvotionProblem`, `IvotionSolution`, topology builder, KPI extraction,
multi-Roland wiring, tests)
**Plus:** complete Particle Swarm implementation (currently a stub per memory).

### Phase B — Optimization UI
(unchanged, plus: operator wage input, hard constraint panel, replications-N input)

### Phase C — Trade-off curve view
- Per-pair multi-objective plot (e.g., cost vs throughput): each evaluated
  design is a dot; non-dominated set highlighted as the curve.
- Click any dot → preview KPIs + parameters; "Apply to viz" loads it.
- Baseline-vs-optimized side-by-side topology view.
- CSV export of all evaluated solutions.

### Phase D — Value-adds (sequenced for biggest payoff first)
- D1: CSV import for cycle times (priority — needed before colleague's data drops)
- D2: Replications with error bars
- D3: Bottleneck heatmap overlay
- D4: Constraint handling
- D5: ROI calculator panel

## Backlog Items to Add (priorities for your review)

- SIM-35 — Phase A: Ivotion problem + multi-Roland topology + tests (P1)
- SIM-36 — Phase A.2: Particle Swarm full implementation (P1)
- SIM-37 — Phase B: Optimization UI panel with live fitness chart (P1)
- SIM-38 — Phase C: Trade-off curve view + apply-to-viz workflow (P1)
- SIM-39 — Phase D1: CSV cycle-time import (P1 — colleague data dependency)
- SIM-40 — Phase D2: Replications + error bars on fitness chart (P2)
- SIM-41 — Phase D3: Bottleneck heatmap overlay on live topology (P2)
- SIM-42 — Phase D4: Constraint panel (max capital / max space) (P2)
- SIM-43 — Phase D5: ROI calculator panel (P2)
- SIM-44 — RolandPrinter v2.1: per-piece fan-out emit semantics (P3)

## Tech Risks

- **ScottPlot.Avalonia version compatibility** — verify against Avalonia 11.x in use
- **Sim wall-time per evaluation** — with N=10 replications must stay < 1s
  per eval for live chart responsiveness; may need to cap sim duration or
  parallelize replications across CPU cores
- **Threading** — optimization runs on background thread; UI updates via
  `Dispatcher.UIThread.Post`. Avalonia threading model is strict.
- **Particle Swarm verification** — needs reference benchmark (Sphere/Rosenbrock)
  to validate convergence behavior before plugging into Ivotion problem
