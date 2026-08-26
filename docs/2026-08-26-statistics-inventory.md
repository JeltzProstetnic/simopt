<!-- Action: reference -->
<!-- Tracked-by: SIM-63, SIM-90, SIM-92, SIM-93, SIM-94 -->
# Inventory: every performance statistic the codebase computes today

Produced 2026-08-26 by a full read of `src/`, `examples/` and `tests/` at commit `134e70d`, to
make sure the SIM-63 subsystem **replaces** the existing computations rather than silently
contradicting them. Every claim is cited at file:line.

**Verdict in one line:** three disjoint statistics implementations exist — polled-per-render-tick
in the visualization, terminal-count arithmetic in Ivotion, and raw counts in the MCP `stats`
object — **none time-weighted, none warm-up-truncated, none replicated**, and several are
outright wrong rather than merely imprecise.

---

## 1. Visualization layer — five computations, all unsound

State fields: `SimulationCanvas.cs:40-43` (`_lastSinkTotal`, `_lastTime`, `_throughput`) and
`:59-61` (`_busyTime`, `_lastBusyChange`, `_wasBusy`), cleared at `:194-198` and `:241-245`.

| # | What | Where | Why it is wrong |
|---|---|---|---|
| i | **Throughput** — windowed finite difference | `SimulationCanvas.cs:283-290` | An instantaneous rate over the most recent ≥1-unit window, not a run statistic. Very noisy at small windows; the final partial window is silently discarded; no warm-up; no variance. The correct measure is `sinkCount / (T − W)`. |
| ii | **Server utilisation** — busy-flag polling | `SimulationCanvas.cs:297-318`, read at `:325-332` | `ns.Working` is sampled once per `DispatcherTimer` tick (0.1 sim-time per `SimulationModel.cs:61-66`). **(a)** Any busy interval shorter than the poll gap is invisible, and a busy→idle→busy transition between ticks reads as one continuous state — so the number moves with the *frame rate*. **(b)** Denominator is absolute `CurrentTime`: no warm-up, no run-start notion. **(c)** Structurally unreachable from headless, MCP or optimiser runs. The virtual close-on-read at `:329-331` is the one thing it gets right. |
| iii | **Bottleneck ranking** — argmax over (ii) | `SimulationCanvas.cs:850-856`, gate at `:871` | Inherits every error of (ii); ranks by a single noisy point estimate with no interval and no tie handling; the `> 0.5` display gate is arbitrary. |
| iv | **A second, independent bottleneck ranking** | `StatsPanel.cs:55-59`, `:64` | Re-derives the ranking from the snapshot and **keys on the display name, not the id**. Names come from `nodeDef?.Label?.Split('\n')[0]` (`SimulationCanvas.cs:231`) truncated to 12 chars (`StatsPanel.cs:63`), so **two servers sharing a label both render as the bottleneck**. A duplicate of (iii) that will drift away from it. |
| v | **WIP "fill"** — capacity-normalised occupancy | `StatsPanel.cs:85`, `SimulationCanvas.cs:899` | `int cap = buf.Capacity < int.MaxValue ? buf.Capacity : 100;` — **an unbounded buffer gets an invented denominator of 100** (also `SimulationCanvas.cs:244`). Instantaneous, not time-averaged, and not WIP. Any "WIP" figure a user reads off this panel is meaningless for unbounded buffers. |

Snapshot plumbing: `SimulationCanvas.cs:349-372` (`GetStatsSnapshot`), DTOs at `:1337-1350`.
Status bar duplicates throughput at `:505-506`.

## 2. Ivotion — KPI extraction (`IvotionKpis.cs:39-86`)

- `:69` **the only measured quantity**: `piecesPerHour = ShippedSink.Count * RolandBatchSize / simHours`.
  Uses the **requested** duration rather than `Model.CurrentTime`; no warm-up, so a line that starts
  empty under-reports steady state by roughly one pipeline fill; single replication; no interval.
- `:65-67` cost and `:71-74` floor space are pure arithmetic on the decision vector plus hardcoded
  constants (`:31-37`) — **never measured from the model**.
- `:83` **`LaborHoursPerSimHour: totalOperators`** — a *headcount* assigned to a field named
  "hours", displayed as "Labor hrs/hr" (`Views/IvotionOptimizationView.axaml.cs:121`). Only
  dimensionally correct if every operator is busy 100 % of the time, which is precisely the
  utilisation nobody measures. **A live mislabelled statistic in the UI.**
- Run driver `IvotionTopologyHandles.cs:60-71` is a fixed 0.1-step polling loop with a `maxSteps`
  cap; **a truncated run is reported as if complete**.
- `IvotionOptimizationEngine.cs:141-147` rebuilds and re-runs the winner a second time to extract
  display KPIs.

## 3. MCP — no time-based statistic at all

`Tools/SimulationTools.cs:97-125` reports only terminal counts and instantaneous flags. `stats` is
the extension point: `sinks` / `buffers` / `servers` / `sources` are the four buckets, and a fifth
`statistics` bucket keyed by `StatisticName` is the obvious shape. There is no typed result DTO —
every tool hand-serialises an anonymous object with default `JsonSerializer` options.

`Models/TopologyDefinition.cs:10-59` has **no place to declare a metric** (SIM-65 adds that).
`ActiveModel.cs:12-36` has **nowhere for collectors to live** — it needs a fifth dictionary or a
collector list.

## 4. Engine templates — raw counters only

`Sink.cs:36,43,49-52` is the only terminal count. `Server.cs:347,356` expose
`timeToFailure.Mean` / `timeToRecover.Mean`, which read the **distribution's declared mean (an
input)**, not a measurement — do not mistake them for output statistics. **No server anywhere
accumulates busy time**, which is why the visualization had to invent it.

## 5. Examples — nothing time-weighted, no warm-up, no replication, no utilisation

- **EmergencyDepartment** — the only sojourn-time measurement in the repo (`Model/Exit.cs:19-34`,
  birth stamped at `Model/PatientSource.cs:90`). Entity-indexed, so unweighted averaging is the
  statistically right form. But: no warm-up; single hardcoded seed; divide-by-zero if a type never
  occurs; `average` is a member field zeroed only in `OnReset()` so it double-accumulates if the
  block runs twice (`Exit.cs:45`); the std-dev at `:34,68-78` is a within-run spread over
  **autocorrelated** observations and therefore **cannot support a confidence interval**;
  `:35` prints the makespan but never divides by it, so no rate is ever computed.
  `Model/TreatmentPoint.cs:29,53-56` maintains a `busy` flag correctly and **never computes
  utilisation** — the single biggest gap.
- **ProductionLine** — `Program.cs:65-67` peak-WIP trackers (exact, but a single-run extreme is
  unbounded in horizon and heavy-tailed, so not a congestion measure); `:87` raw completed count
  never divided by the 480-minute horizon; `:90-94` **ranks the bottleneck by comparing two
  peak-WIP numbers with `>=`**, so an exact tie is silently attributed to "Process".
- **SQSS** — `Program.cs:19-20` prints the **queue** count under the label
  *"Sink received items"*; `sim.Sink.Count` exists (`Simulation.cs:51`) and is never printed.
  `Simulation.cs:88-98` has `QueueRejectedItem`/`QueueFull` handlers sitting exactly where blocking
  probability would be estimated, and they count nothing.
- **SimOptDemo** — `Model/Simulation.cs:143` makespan objective. **Sound**: the model is
  deterministic given a candidate, so one run is an exact evaluation. But `Optimizer/Problem.cs:65-72`
  has an event named `TenEvaluationsDone` that **fires every 20**.
- **SupplyChain** (legacy WPF, excluded from compilation) — `MainWindow.xaml.cs:1030` bakes a
  cosmetic `+ 0.1` into every reported value; `Optimizer/Solution.cs:33-37` makes the entire
  optimisation objective `rnd.NextDouble() * 3000` with an unseeded `Random`, so the convergence
  chart at `:1085` is meaningless.
- **DiningPhilosophers**, **WarehouseSimulator** — no statistics at all.

## 6. Near-misses that are NOT output statistics — do not replace these

- `Tools/AsyncModelRunner.cs:151-237` — `AverageSpeedSinceReset` and friends are **wall-clock
  runner telemetry** (how fast the host executes the sim), not system performance. High
  name-collision risk: pick collector names that cannot be confused with these.
- `EvolutionaryAlgorithm.cs:126` `CurrentGenerationAverageFitness` — optimiser-search statistic.
- `IvotionFitnessSample.cs:8-11` — best-so-far convergence trace, monotone by construction.
- `RolandPrinter.cs:27` `CycleTime => BatchSize * PerPieceTime` — a **model parameter**.
- `SimOpt.Statistics/*` — PCA / ROC / confusion matrix / SVM kernels; machine learning, unrelated.
  Note `Kernels/TStudent.cs` is an **SVM kernel, not a t-distribution**.
- `SimOpt.GridWorld`, `SimOpt.FMT`, `SimOpt.Learning`, `SimOpt.Logging` — zero statistics.

## 7. Tests that assert on the above and will need updating when it is replaced

`Ivotion/IvotionKpisTests.cs:73-110`, `IvotionTopologyBuilderTests.cs:80-99`,
`IvotionProblemTests.cs:139+`, `IvotionOptimizationEngineTests.cs:77,147-167`,
`Visualization/IvotionOptimizationViewModelTests.cs:52,77,191`,
`McpServer/ModelRegistrySmokeTests.cs:92-93`.

---

## Contract every new collector must honour

Distilled from the reset-path reading:

1. Derive from `Entity` so `Model.Reset` finds it and `Model.FindEntities` can discover it.
2. **Re-read live state from a probe on reset rather than assuming zero.** `Model.Reset` visits
   `items.Values` in dictionary order, so a collector may be reset *before or after* the thing it
   observes.
3. Preserve `WarmupTime` across reset — it is an experiment setting, not accumulated state.
4. Never assume ordering relative to the observed entity.
