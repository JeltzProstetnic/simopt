# SimOpt

A simulation-optimization framework for .NET that couples a discrete-event simulation engine with metaheuristic optimization strategies. Built for modelling stochastic systems — queuing networks, supply chains, emergency departments, warehouses, factory floors — and automatically searching for configurations that optimize their performance.

## Vision

Most real-world systems are too complex for closed-form solutions. SimOpt bridges that gap: you build a simulation model of your system, define what "good" means via a fitness function, and let an optimization strategy search the parameter space by running the simulation hundreds or thousands of times. The framework handles the coupling so you can focus on the model.

The long-term goal is a batteries-included toolkit for simulation-based optimization research and practice on .NET, with:

- A flexible discrete-event simulation engine with reusable building blocks
- Pluggable metaheuristic optimizers (evolutionary algorithms, simulated annealing, PSO, and more)
- Live 2D visualization with schematic and realistic rendering modes
- Clean interfaces that make it straightforward to add new strategies, models, and analysis tools
- Cross-platform support (.NET 9)

## Architecture

SimOpt is organized in three layers:

**1. Discrete-Event Simulation** (`SimOpt.Simulation`)
Next-event time-advance engine. Models are built from entities (sources, buffers, servers, sinks, conveyors, state machines) connected into networks. The `IModel` interface manages the event calendar, entity registry, and simulation lifecycle.

**2. Optimization** (`SimOpt.Optimization`)
Strategy-agnostic optimization built around three interfaces:
- `IStrategy` — the optimizer (EA, SA, PSO, random search)
- `IProblem` — defines candidate generation, validation, and evaluation
- `ISolution` — a candidate solution with fitness

**3. Sim-Opt Coupling**
The outer optimization loop calls `IProblem.Evaluate()`, which resets and runs the simulation model to compute fitness. This decoupled design means any simulation model can be optimized by any strategy without either side knowing the other's internals.

### Visualization (`SimOpt.Visualization`)

Avalonia-based 2D live rendering with two modes:

- **Schematic** — industrial SCADA-style: gradient-filled nodes, animated conveyor chevrons, colored entity diamonds, glow effects on active machines, drop shadows
- **Realistic** — factory floor view: concrete tile flooring, metallic machine casings with LED status panels, storage rack grids, loading/shipping dock bays, belt conveyors with animated rollers, isometric 3D entity boxes, vignette lighting

Both modes include a live statistics panel (server utilization, WIP levels, bottleneck detection, throughput) that can be detached to a separate window for multi-monitor setups.

**Controls:**
| Key | Action |
|-----|--------|
| Space | Start / Pause |
| - / + | Speed faster / slower |
| R | Toggle Schematic / Realistic |
| S | Toggle statistics panel |
| F | Fullscreen |
| D | Detach controls + stats to floating windows |
| Esc | Exit fullscreen |

### Supporting Libraries

| Library | Purpose |
|---------|---------|
| `SimOpt.Basics` | Data structures, interfaces, geometry primitives |
| `SimOpt.Mathematics` | Probability distributions, RNGs, matrix operations |
| `SimOpt.Statistics` | Statistical analysis and output processing |
| `SimOpt.Logging` | Cross-platform simulation logging |
| `SimOpt.Learning` | Machine learning utilities |

### Simulation Templates

| Template | Purpose |
|----------|---------|
| `SimpleSource` | Generates entities at stochastic intervals |
| `SimpleBuffer` | FIFO/LIFO/Priority queue with capacity |
| `SimpleServer` | Processes entities with stochastic service time |
| `SimpleRejectServer` | Server with probabilistic reject/pass routing |
| `SimpleSink` | Counts and removes finished entities |

## Examples

The `examples/` directory contains complete models:

- **SQSS** — Single-queue, single-server model (textbook M/M/1 queue)
- **SimOptDemo** — Simulation-optimization coupling demonstration
- **EmergencyDepartment** — Hospital ED patient flow model
- **SupplyChain** — Multi-echelon supply chain simulation
- **WarehouseSimulator** — Warehouse operations with mobile entities
- **DiningPhilosophers** — Classic concurrency problem as a DES model

### Built-in Visualization Presets

| Preset | Topology |
|--------|----------|
| SQSS | Source → Buffer(15) → Server → Sink |
| Parallel Servers | Source → Buffer(20) → 2×Server → Sink |
| Production Line | Source → Buf → Stage1 → Buf → Stage2 → Sink |
| Factory Floor | 2×Source → Inspect(5% reject→Waste) → 3×Assembly → QC → Pack → Ship |

## Building

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet build SimOpt.slnx
```

## Testing

551 tests across simulation, optimization, mathematics, and statistics (xUnit + FluentAssertions):

```bash
dotnet test SimOpt.slnx
```

## Running the Visualization

```bash
dotnet run --project src/SimOpt.Visualization/SimOpt.Visualization.csproj
```

Select a topology preset, press Start, then use keyboard shortcuts to control the simulation.

## Project Status

SimOpt is under active development. The codebase originated as an academic framework (dissertation research) and has been modernized for .NET 9.

Completed:
- .NET 9 SDK-style migration
- Discrete-event engine with 6 template types including probabilistic routing
- 4 optimization strategies (EA, SA, PSO, Random Search)
- 2D visualization with dual render modes (schematic + realistic)
- Live statistics with utilization tracking and bottleneck detection
- Detachable controls and stats for multi-monitor setups
- 551 tests with full CI/CD via GitHub Actions

Current focus:
- Optimization showcase (before/after comparison, fitness charts)
- Realistic renderer v2 (building features, organic layouts)
- Human agent entity (mobile worker with repair tasks)

## Documentation

- [Dissertation (full)](documentation/Dissertation.Full.pdf) — theoretical foundations and design rationale
- [Dissertation (abridged)](documentation/Dissertation.Abridgment.pdf) — condensed overview
- [SQSS Tutorial](documentation/SQSSTutorial.odt) — walkthrough of the single-queue/single-server example
- [Coding Conventions](documentation/CodingConventions.odt) — project style guide
- [Logging Guide](documentation/MatthiasToolbox.Logging.pdf) — logging subsystem reference
- [Class Diagrams](design/) — UML diagrams for core subsystems
