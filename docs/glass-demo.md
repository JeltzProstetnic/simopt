# Glass Production-Line Demo — run guide

Live discrete-event **digital twin** of a 5-step glass base-mass (Grundmasse) line, with an
**evolutionary optimizer** that tunes the line and applies the best configuration back to the
live view. Built on the generic SimOpt engine (`SimOpt.Simulation` + `SimOpt.Optimization`).

## Run it

```bash
# dotnet 9 SDK required. On WSL it lives at ~/.dotnet (not on PATH):
export PATH="$HOME/.dotnet:$PATH"     # skip if `dotnet` is already on PATH

# Live visual twin (Avalonia desktop):
dotnet run --project src/SimOpt.Visualization/SimOpt.Visualization.csproj

# Headless per-stage / bottleneck report (console, no GUI):
dotnet run --project examples/SimOpt.Examples.ProductionLine
```

## In the window

- The topology dropdown defaults to **"Glass Production Line (Base Mass)"**. Press **Space** (or Start) to run.
- Flow: `Rohmateriallager → Materialpuffer → Mischen/Aufbereitung → Zwischenlager → Weiterverarbeitung → Fertigmeldung`.
- *Weiterverarbeitung* is the deliberate **bottleneck** (3.0 min vs 2.5 min feed) — watch work-in-process pile up in the **Zwischenlager** buffer (live Engpass-Früherkennung).
- Controls: **Space** play/pause · **−/+** speed · **F** fullscreen · **D** detach controls · render-mode toggle (Schematic/Realistic) · live stats panel.

## "Optimization (Glass)" tab

1. Pick an **objective** (Maximize throughput / Minimize total cost / Minimize cost-per-piece / Minimize labor) and **strategy** (Evolutionary or Random); set iterations / population.
2. **Run** → the *best-so-far fitness* chart converges live (ScottPlot).
3. **Apply to Viz** → loads the optimized configuration into the live twin and switches to it.

Decision variables the optimizer searches (144 configs):
`NumberOfMixers {1,2,3}`, `NumberOfProcessingLines {1,2}`, `IntermediateBufferCapacity {4,8,12,20}`, `OperatorsQuality {1,2,3}`, `OperatorsPacking {1,2}`.

## Adapting the twin (the workflow)

The line is defined by `VizTopology.GlassLine()` in `src/SimOpt.Visualization/Models/VizTopology.cs`
(nodes = source/buffer/server/sink with `Params`, `Label`, `Color`, physical `X/Y/W/H`; connections by id).
Edit that method (service times, steps, labels) → rebuild → the canvas auto-renders the change.
New named twins: add a `VizTopology` factory method + register it in `MainWindowViewModel`
(`TopologyNames` + `GetSelectedTopology`).
