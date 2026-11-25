# SIM-110, G2: one model path for the viewer

2026-11-25 (restart #5, unattended), WSL. Design brief; building waits for MG's answer to
**Q-VIEW-PATH** (`docs/pending-sim-122-continuation.md`). Part of slice V1 of
`docs/plans/2026-11-03-sim-105-visualization-uplift.md`.

## Where SIM-110 stands

Done the same night, test-first: the layout survives cycles (SIM-108, `c52c0a1`), Space pauses and
resumes (SIM-107, `0157dd2`), and every number on screen is the engine's own measure, with "–" where
nothing is measured and no invented capacity (SIM-92 and defect 6, `ea287b1`). Left: one model path
shared with the MCP head, topology JSON and MCP models opening in the viewer, and the "warming up"
state. All three hinge on how the viewer builds its model, which is this brief.

## Today: two builders that disagree

| | viewer (`SimulationModel`, from `VizTopology`) | MCP head (`ModelRegistry.TryBuild`, schema v1) |
|---|---|---|
| arrivals | Gaussian, spread 15 % of the mean by default | any of seven distribution families |
| service | constant `service_time` | any distribution, `servers: c` |
| rejects | `SimpleRejectServer`, a `System.Random` seeded with `topology.Seed + node.Id.GetHashCode()` (SIM-99: changes on every process start) | a probabilistic `router` on its own seeded stream |
| batch printer | `roland` (`RolandPrinter`, Ivotion only) | none |
| server → server | pushed directly | refused: a station is fed by a queue (SIM-135) |
| layout | `X`, `Y`, `Width`, `Height`, `Label`, `Color` on each node | none |

UN-004 asks for one model representation, and UN-009 for the same numbers after a restart, which the
viewer's reject server breaks. furkansim's app builds its glass line as a `VizTopology` (source,
buffer, server and sink only, with `mean_interval`, `capacity` and `service_time`) and hands it to
`SimulationCanvas.StartSimulation`.

## Recommended option of Q-VIEW-PATH

1. **The topology layer becomes a library.** `SimOpt.Topology` takes the schema types
   (`TopologyDefinition`, `ParamValue`, `DistributionSpec`, `SchemaCatalog`), the builder,
   `DistributionFactory`, the lint, the offered-load check and the patcher out of `SimOpt.McpServer`;
   the server, the viewer, the tests and the coming desktop app (SIM-71, which per the readiness review
   calls the tools in process) reference it. The namespaces become `SimOpt.Topology.*`, a mechanical
   change to the `using` lines of 18 test files and 16 source files. Nothing in the JSON or the MCP
   tools changes.
2. **Every model the viewer shows is built by that builder.** `VizTopology` stays as the input
   furkansim and the presets use, and converts to a schema document plus a layout overlay (position,
   size, label, colour by node id). The conversion maps `buffer` to a queue, `server` to a station with
   a `constant` service, `reject_rate` to a station followed by a probabilistic router (so a reject
   follows the model seed, which closes the viewer half of SIM-99), and a direct server → server push
   to a queue between them, as the schema requires.
3. **Two additions to the schema, both small:** a `normal` distribution family, truncated at zero, so
   the presets keep their Gaussian arrivals; and `batch` on a station (`batch: n`, the Roland printer's
   batches of n with a time per piece), so the Ivotion preset converts too. Each comes with the
   goodness-of-fit tests SIM-65 requires of a new sampler.
4. **File → Open** reads a topology JSON (what `export_model` and `save_model` write, SIM-67) and shows
   its validation problems on the offending nodes (UN-008). A document may carry an optional top-level
   `layout` block that the builder ignores, so a model saved from the viewer reopens where it was drawn.
5. **"Warming up until t = …"** appears when the document declares a warm-up; until then the collectors
   hold no data and the screen shows "–".

**What MG will see change:** the presets' reject decisions follow the seed (the same run every time,
and different numbers from today's); a server that fed another server directly now has a queue drawn
between them; the Ivotion preset's printer is a station with batches. furkansim's app keeps compiling
(`VizTopology` and the canvas keep their members), but its glass line's numbers move with the
reject routing and the inserted queues, so it gets an inbox item with the commit.

## Alternatives in Q-VIEW-PATH

- **The viewer references `SimOpt.McpServer` directly** instead of a new library: a one-line change,
  but the MCP SDK and the generic host ride into the viewer and, through it, into furkansim's app, and
  the desktop app later needs the split anyway.
- **Two paths:** keep `SimulationModel` for `VizTopology` and add JSON and MCP models through the
  schema builder. The least change to what the presets show; the viewer's reject server keeps its
  per-process seed (SIM-99 stays open) and UN-004's single representation stays unmet.

## Build plan after the answer (test-first)

1. Extract `SimOpt.Topology`; the suite passes unchanged apart from `using` lines, and
   `EngineFingerprintTests` stays as it is.
2. `normal` and `batch` with their goodness-of-fit and batch tests.
3. `VizTopology` → schema document plus layout; each preset builds through the schema builder, and a
   test runs each preset under both paths before the old one is deleted, reporting where the numbers
   differ and why (reject stream, inserted queues).
4. The canvas on the shared builder; `SimulationModel` deleted; File → Open with validation problems on
   the nodes; the `layout` block; the warm-up state.
5. furkansim inbox item with the commit; SIM-99's viewer half closed.
