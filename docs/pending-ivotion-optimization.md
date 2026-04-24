Action: act

# Ivotion Optimization Showcase — Next Session Kickoff

Tracked-by: SIM-35, SIM-36, SIM-37, SIM-38, SIM-39, SIM-40, SIM-41, SIM-42, SIM-43, SIM-44

## Context

Previous session (2026-04-24): unprepared customer demo went well.
- v1: Spontaneously built `IvotionPacking` viz preset from cycle times the user
  gave during the meeting (Inspect 10s → Roland 60s/pc batch=15 → Pack 20s →
  Label 4+8s → SSB 3s).
- v2: Built `RolandPrinter` domain class with batching semantics + 6 unit tests
  (1 integration test skipped due to documented null-Identifier wiring scaffold
  issue; live integration runs via the IvotionPacking viz preset).
- Ivoclar colleague was impressed; wants real-data optimization demo next week.

## What this session does

Start Phase A of the optimization showcase. Full plan:
**`docs/plans/ivotion-optimization-showcase.md`** — read this first.

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

## Build order (priorities confirmed by user)

1. **SIM-35** Phase A: Core problem + multi-Roland topology + KPI extraction + tests (P1)
2. **SIM-36** Phase A.2: Particle Swarm full impl + benchmark validation (P1)
3. **SIM-39** Phase D1: CSV import (slot in early — colleague brings data) (P1)
4. **SIM-37** Phase B: Optimization UI + ScottPlot.Avalonia chart (P1)
5. **SIM-38** Phase C: Trade-off curve view + apply-to-viz (P1)
6. **SIM-40..43** Phase D2-D5: replications, heatmap, constraints, ROI (P2)
7. **SIM-44** RolandPrinter v2.1 fan-out (P3, time-permitting)

## TDD requirement

Project rule (`.claude/CLAUDE.md`): all new code must follow TDD. Write
failing tests first, then implement. Before any code changes, run the full
test suite to establish a regression baseline (currently 557 passed,
1 skipped, 0 failed). Re-run after each phase to verify zero regressions.

## Tech choices already decided

- **Charting:** ScottPlot.Avalonia (NuGet) — verify version compat with Avalonia 11.x
- **Operator parallelism model:** `effective_service_time = base / operator_count`
  (simple, honest, disclosable; real ResourceManager pool is a v3 enhancement)
- **Multi-Roland layout:** stack vertically — `roland_a` y=3, `roland_b` y=7

## Key files to read before starting

- `docs/plans/ivotion-optimization-showcase.md` — full plan
- `src/SimOpt.Visualization/Models/VizTopology.cs` — `IvotionPacking()` preset
- `src/SimOpt.Simulation/Templates/RolandPrinter.cs` — v2 domain class
- `src/SimOpt.Visualization/Models/SimulationModel.cs` — node→entity wiring
- `src/SimOpt.Optimization/Strategies/ParticleSwarm/` — current PSO stub
- `src/SimOpt.Optimization/Interfaces/IProblem.cs` — interface to implement
- `tests/SimOpt.Tests/Helpers/TestProblem.cs` + `TestSolution.cs` — pattern reference

## First concrete step

Run `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj -c Release` to confirm
the baseline of 557 passing tests. Then create
`tests/SimOpt.Tests/Optimization/IvotionProblemTests.cs` and start writing
the first failing test — probably "decision variables validate within range"
or "evaluate produces deterministic fitness for fixed seed".
