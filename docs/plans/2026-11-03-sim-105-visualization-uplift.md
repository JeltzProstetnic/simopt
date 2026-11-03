# SIM-105: bringing SimOpt's visualization up to a reference flow map

**Date:** 2026-11-03 · **Status:** plan, nothing built yet · **Serves:** UN-018, UN-019, UN-010–013, UN-008, UN-009

The owner asked for SimOpt's visualization to reach the level of a production flow map he was
shown (backlog SIM-105). That reference was reviewed privately. What follows is SimOpt's own
design. It reimplements ideas in C# and Avalonia and ports no outside code.

## The ideas worth taking

- **Moving dots that obey Little's law.** Items leave an edge at 1/throughput and take the mean wait
  to cross it, so the number of dots visible on an edge equals the WIP there. A viewer reads
  congestion without a number.
- **Edge width shows measured flow,** relative to the busiest edge, with a cap.
- **Health has three levels, a parent takes its worst child's level, and every colour carries its
  reason** ("utilisation 0.97 · queue +3/h"). Colour appears only where a user-declared threshold or
  an analytic overload flag (ρ ≥ 1) justifies it, never from a built-in guess.
- **A breakdown looks like "value = formula, its parts, n, warm-up".** Any number on the map opens
  to that.
- **The layout follows the material flow,** in layers, with crossings reduced and back-edges drawn as
  rework arcs instead of crashing the layout.
- **Drill-down zooms into the clicked node,** and the level of detail follows the available width
  rather than shrinking text.
- **Replay.** A seeded run is deterministic, so a recorded event log lets the user scrub, seek to an
  incident, or step to the next event exactly.
- **Missing is not zero.** "–" means not measured, "warming up until t = …" means the statistics
  have not started, and an unbounded buffer shows its count with no invented capacity.
- **Accessibility and motion.** Nodes are keyboard-reachable and exposed to screen readers, a
  reduced-motion setting turns animation into fades, and every state is shown by colour plus an
  icon or pattern.

## SimOpt's visualization today (measured 2026-11-03)

One custom-drawn Avalonia control, `Controls/SimulationCanvas.cs` (1,358 lines), repaints
everything every frame, in a schematic mode and a "realistic" mode (floor tiles, belts, racks). A
`DispatcherTimer` calls `SimulationModel.Step()`, which advances a fixed 0.1 time units per tick,
and the canvas then polls node counts. It subscribes to no engine event and reads no statistics
collector. Topologies are five hard-coded presets. Layout is DFS longest-path layering with no
crossing reduction. The Ivotion optimisation view exists but is not mounted in `MainWindow.axaml`.

**Defects found by this review, each checked against the source:**

| # | Defect | Where | Item |
|---|---|---|---|
| 1 | Space ("pause") calls `Stop()`. The next `Start()` rebuilds the model from t = 0, so pause actually restarts the run. | `ViewModels/MainWindowViewModel.cs:100-124` | SIM-107 |
| 2 | `AssignLayer` recurses with an ever-growing depth on any cycle. A rework loop overflows the stack and kills the process. | `Models/AutoLayout.cs:120-126` | SIM-108 |
| 3 | Dots are inferred from per-node count deltas. One pull from a buffer spawns a dot on every outgoing edge. An arrival and a pull that cancel within one tick spawn none. | `Controls/SimulationCanvas.cs:406-423` | SIM-109 |
| 4 | Utilisation, both bottleneck rankings and dot spawning filter on `Type == "server"`, so the Ivotion `roland` station, the line's bottleneck, shows no state at all. | `SimulationCanvas.cs:303, 354, 406-423, 832` | SIM-109 |
| 5 | The five unsound statistics of SIM-92. The polling bias depends on the fixed 0.1 time step, not on the frame rate as SIM-92 states. | see SIM-92 | SIM-92 |
| 6 | Realistic racks treat an unbounded buffer as 20 slots, an invented denominator like SIM-92's 100. | `SimulationCanvas.cs:1174` | Slice V1 |
| 7 | A second model builder diverges from the MCP head (Gaussian arrivals and constant service vs negative-exponential), and its reject server is seeded with `string.GetHashCode()`. This conflicts with UN-004 (one model representation) and UN-009. | `Models/SimulationModel.cs:107, 122, 128` | SIM-99, Slice V1 |
| 8 | The legend and scale bar are drawn twice per frame. | `SimulationCanvas.cs:509-514`, `794-795` | Slice V2 |
| 9 | The window and canvas are hard-coded dark while `App.axaml` follows the system theme. | `MainWindow.axaml:15, 25`; `App.axaml:5` | Slice V2 |
| 10 | Time is shown as a bare "t = 12.3", with no units, warm-up, replications or intervals. | canvas header | Slice V3 |

**What it cannot show at all:** a model built by the user or over MCP, history or scrubbing,
drill-down, hover or selection, zoom or pan, time-series charts, state timelines, time in system,
wait distributions, uncertainty, scenario comparison, validation problems on the map, export,
keyboard or screen-reader access, and a light theme.

## Gaps, with the engine seams each needs

| Gap | What | Engine seam | Size |
|---|---|---|---|
| G1 | Numbers read from the engine collectors (`Instrumentation`), attached by template type rather than type string. NaN is shown as "–". Closes SIM-92. | new `ObserveThroughput(sink)` and `ObserveTimeInSystem(source → sink)`, keyed by entity (possible since SIM-90) | M |
| G2 | One model path: a shared topology builder in `SimOpt.Simulation` returning id → entity and collectors, with `SimulationModel` deleted. File → Open for topology JSON (SIM-65/67) and an MCP "show" hand-off. Cycle-safe layering. | — | M |
| G3 | Motion driven by events and a real clock. A recorder subscribes to the source, buffer, server and sink events and logs `(t, kind, from, to, entity)`. Speed is time per wall-second. Pause, resume and single-step keep the model alive. | new `Server.ItemAcceptedEvent`, naming which parallel server pulled the item | M |
| G4 | Congestion encoding: width = measured flow, Little's-law dots, tint only from declared thresholds or SIM-66's ρ ≥ 1 flag, each with a reason line, and a worst-of roll-up for grouped stations. | per-connection flow counter | M |
| G5 | Layered layout: cycle breaking, longest-path layers, barycentre crossing reduction, port-aware routing, parallel servers as lanes in one station box, and detail by width. Physical layout stays as an override. | — | M/L |
| G6 | Replay with a scrubber over the G3 log: prefix sums with periodic checkpoints, incident marks (buffer full, failures), seek, next event, and "jump to the queue-length peak". | `Server.StateChangedEvent` (idle / working / blocked / damaged / recovering) | M/L |
| G7 | Station drill-down panel (ScottPlot is already referenced): state Gantt, queue step chart with its time average, wait histogram, a definition line, and hover tooltips with provenance. | histogram or quantiles on `TallyCollector` | M |
| G8 | Replications and intervals on screen: `ReplicationRunner` in the background, tiles showing mean ± half-width with n, and the animation labelled "replication k/N, seed s". | progress and cancellation on `ReplicationRunner` | M |
| G9 | Scenario comparison: two synchronised maps under common random numbers (`ReplicationRunner.SeedFor`), and a delta map tinted only where the paired-t interval excludes 0. Ivotion "Apply" opens current vs best. | paired-difference CI helper | L |
| G10 | Visual tokens and themes: theme-variant `ResourceDictionary`s, colour plus icon or pattern, a colour-blind-safe palette, reduced motion, and cached pens and brushes. | — | S/M |
| G11 | Accessibility: an `AutomationPeer` exposing nodes (name, state, value), Tab and arrow-key traversal, and Enter to drill down. | — | M |
| G12 | Empty and invalid states: "warming up until t = …", "–" for no data, and UN-008 problems drawn on the offending node. | — | S |
| G13 | Export: PNG of the map (`RenderTargetBitmap`), CSV of collector results, and an assumptions footer. | — | S/M |

## Slices, each shipping something visible

| Slice | Contents | Item |
|---|---|---|
| **V1 Honest live map** | G1 + G2 + the pause/clock part of G3 + G12, plus defects 1, 2, 6 and 7. Every number on screen is measured by the engine, any JSON or MCP model opens, pause works, and cycles don't crash. | SIM-110 |
| **V2 Flow you can read** | The rest of G3, plus G4, G5 and G10, and defects 3, 4, 8 and 9. True dots, width and Little's-law dots, a visible bottleneck, a clean layout, light and dark themes. | SIM-111 |
| **V3 Drill-down and evidence** | G7, G8, a KPI strip (throughput, WIP, time in system, bottleneck utilisation, each opening its breakdown), G11, and defect 10. | SIM-112 |
| **V4 Rewind** | G6 | SIM-113 |
| **V5 Compare and hand over** | G9, the Ivotion tab re-mounted with SIM-93 fixed, and G13 | SIM-114 |

Key files: `src/SimOpt.Visualization/Controls/SimulationCanvas.cs`,
`Models/SimulationModel.cs` (to be replaced; compare `src/SimOpt.McpServer/Simulation/ModelRegistry.cs`),
`src/SimOpt.Simulation/Statistics/Instrumentation.cs`, `Models/AutoLayout.cs`,
`ViewModels/MainWindowViewModel.cs`.
