Action: reference

# Ivotion Optimization Showcase — Plan Reference

Tracked-by: SIM-35 (done), SIM-36, SIM-37, SIM-38, SIM-39, SIM-40, SIM-41, SIM-42, SIM-43, SIM-44

## Status

Phase A (SIM-35) **complete** — see commit for the `SimOpt.Ivotion` library. The
customer-facing plan below is preserved as a record; subsequent phases remain
open in the backlog and proceed one SIM-ID at a time via next-session-task.md.

## Context (preserved)

Previous session (2026-04-24): unprepared customer demo went well.
- v1: Spontaneously built `IvotionPacking` viz preset from cycle times the user
  gave during the meeting (Inspect 10s → Roland 60s/pc batch=15 → Pack 20s →
  Label 4+8s → SSB 3s).
- v2: Built `RolandPrinter` domain class with batching semantics + 6 unit tests
  (1 integration test skipped due to documented null-Identifier wiring scaffold
  issue; live integration runs via the IvotionPacking viz preset).
- Ivoclar colleague was impressed; wants real-data optimization demo next week.

## User decisions already locked in (do NOT re-ask)

1. Operator wage → exposed in UI (slider/numeric input, default $32/hr)
2. Strategies → ship all four: Random, Evolutionary, Particle Swarm (full impl
   — currently stub), Sweep (exhaustive enumeration of the 216-combo space)
3. Multi-objective → true non-dominated set / trade-off curve, NOT scalarized
   weighted-sum (machine can afford the compute)
4. "Apply best solution to viz" → load topology only, do NOT auto-start.
   User hits Space when ready.

## Cost model defaults (user-confirmed US numbers)

- Roland LEF capital: $30k → $3/hr amortized (5 yr × 2000 hr/yr)
- Skilled operator: $32/hr fully loaded (US-typical)

## Decision variables (216-combo search space)

| Variable                | Range / Type           |
|-------------------------|-----------------------|
| `roland_count`          | {1, 2}                |
| `operators_inspect`     | {1, 2, 3}             |
| `operators_pack`        | {1, 2, 3}             |
| `operators_label`       | {1, 2}                |
| `operators_ssb`         | {1, 2}                |
| `roland_batch_size`     | {10, 15, 20}          |

## Build order (revised 2026-04-24 — agile vertical slice + positioning shift)

1. ~~**SIM-35** Phase A: Core problem + multi-Roland topology + KPI extraction + tests (P1)~~ ✓
2. **SIM-37** Phase B: Optimization UI + ScottPlot.Avalonia chart (P1) — **next up**
   - EA + Random wired; PSO dropdown entry greyed out "coming"
3. **SIM-36** Phase A.2: Particle Swarm full impl + benchmark validation (P1)
4. **SIM-38** Phase C: Trade-off curve view + apply-to-viz (P1)
5. **SIM-40..43** Phase D2-D5: replications, heatmap, constraints, ROI (P2)
6. **SIM-44** RolandPrinter v2.1 fan-out (P3, time-permitting)

~~SIM-39 Phase D1: CSV import~~ — **retired** 2026-04-24. SimOpt is agent-driven
sim-opt for non-experts; parameterization goes through Claude Code / agent fleet
dialogue, not a fixed CSV schema. No import layer needed. See
`docs/decisions.md` "Agent-Driven Sim-Opt Positioning".

## Tech choices already decided

- **Charting:** ScottPlot.Avalonia (NuGet) — verify version compat with Avalonia 11.x
- **Operator parallelism model:** `effective_service_time = base / operator_count`
  (simple, honest, disclosable; real ResourceManager pool is a v3 enhancement)
- **Multi-Roland layout:** stack vertically — `roland_a` y=3, `roland_b` y=7

## Phase A delivery notes (SIM-35)

- New library `src/SimOpt.Ivotion/` (depends on Basics, Mathematics, Simulation, Optimization).
- `IvotionSolution` — 6-dim int vector, implements ISolution + ITweakable +
  ICombinable<ISolution>. Clamps out-of-range values at construction.
- `IvotionTopologyBuilder` — pure headless (no Visualization dep). Returns
  `IvotionTopologyHandles` exposing Model, Source, ShippedSink, Rolands list,
  manual stations, buffers, effective service times.
- `IvotionKpis` — pure record struct. Extract(handles, simDurationMinutes)
  produces throughput, cost/hr, labor-hr, floor-m², cost/piece.
- `IvotionProblem` — IProblem with switchable Objective (MaximizeThroughput,
  MinimizeTotalCost, MinimizeCostPerPiece, MinimizeLaborHours, MinimizeFloorSpace).
- Known limitation inherited from `RolandPrinter`: one entity emitted per batch.
  KPI extractor multiplies by `RolandBatchSize` to report physical pieces/hour.
  SIM-44 will fix per-piece fan-out properly.
- Test coverage: 37 tests (11 solution + 7 topology + 7 KPIs + 12 problem).
  594 pass / 1 skip / 0 fail across the full suite.

## Next session task

SIM-37 Phase B (UI panel) shipped 2026-04-24. Next per agile reorder (line 50-62):
**SIM-36** — Particle Swarm full implementation, benchmarked against Sphere/Rosenbrock,
then unlock the greyed PSO dropdown entry by flipping
`IvotionStrategyInfo.IsEnabled(ParticleSwarm) => true` and removing the
`NotSupportedException` branch in `IvotionOptimizationEngine.Run`.
