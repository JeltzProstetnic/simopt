# SimOpt

A simulation-optimization framework for .NET that couples a discrete-event simulation engine with metaheuristic optimization strategies. Built for modelling stochastic systems — queuing networks, supply chains, emergency departments, warehouses — and automatically searching for configurations that optimize their performance.

## Vision

Most real-world systems are too complex for closed-form solutions. SimOpt bridges that gap: you build a simulation model of your system, define what "good" means via a fitness function, and let an optimization strategy search the parameter space by running the simulation hundreds or thousands of times. The framework handles the coupling so you can focus on the model.

The long-term goal is a batteries-included toolkit for simulation-based optimization research and practice on .NET, with:

- A flexible discrete-event simulation engine with reusable building blocks
- Pluggable metaheuristic optimizers (evolutionary algorithms, simulated annealing, and more)
- Clean interfaces that make it straightforward to add new strategies, models, and analysis tools
- Cross-platform support (.NET 9)

## Architecture

SimOpt is organized in three layers:

**1. Discrete-Event Simulation** (`SimOpt.Simulation`)
Next-event time-advance engine. Models are built from entities (sources, buffers, servers, sinks, conveyors, state machines) connected into networks. The `IModel` interface manages the event calendar, entity registry, and simulation lifecycle.

**2. Optimization** (`SimOpt.Optimization`)
Strategy-agnostic optimization built around three interfaces:
- `IStrategy` — the optimizer (EA, SA, random search, etc.)
- `IProblem` — defines candidate generation, validation, and evaluation
- `ISolution` — a candidate solution with fitness

**3. Sim-Opt Coupling**
The outer optimization loop calls `IProblem.Evaluate()`, which resets and runs the simulation model to compute fitness. This decoupled design means any simulation model can be optimized by any strategy without either side knowing the other's internals.

### Supporting Libraries

| Library | Purpose |
|---------|---------|
| `SimOpt.Basics` | Data structures, interfaces, geometry primitives |
| `SimOpt.Mathematics` | Probability distributions, RNGs, matrix operations |
| `SimOpt.Statistics` | Statistical analysis and output processing |
| `SimOpt.Logging` | Cross-platform simulation logging |
| `SimOpt.Learning` | Machine learning utilities |

## Examples

The `examples/` directory contains complete models:

- **SQSS** — Single-queue, single-server model (textbook M/M/1 queue)
- **SimOptDemo** — Simulation-optimization coupling demonstration
- **EmergencyDepartment** — Hospital ED patient flow model
- **SupplyChain** — Multi-echelon supply chain simulation
- **WarehouseSimulator** — Warehouse operations with mobile entities
- **DiningPhilosophers** — Classic concurrency problem as a DES model

## Building

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet build SimOpt.slnx
```

## Testing

151 tests across 7 domains (xUnit + FluentAssertions):

| Domain | Tests | Coverage |
|--------|-------|----------|
| Simulation Engine | 27 | EventScheduler, Model lifecycle |
| Simulation Templates | 5 | Source-Sink integration |
| Optimization Strategies | 22 | Random search, SA, EA |
| State Machine | 10 | State transitions |
| Mathematics | 30 | Complex numbers, distributions, distances, utilities |
| Statistics | 15 | ConfusionMatrix metrics |
| Basics/Geometry | 22 | Point, Vector arithmetic |

```bash
dotnet test SimOpt.slnx
```

## Project Status

SimOpt is under active modernization. The codebase originated as an academic framework (dissertation research) and is being updated for modern .NET.

Completed:
- .NET 9 SDK-style migration (Phase 1)
- xUnit test infrastructure with 151 tests (Phase 2-3)
- 5 bug fixes across Simulation, Optimization, and Basics
- CI/CD via GitHub Actions

Current focus:
- Additional optimization strategies (PSO)
- Nullable annotation cleanup
- Extended test coverage for Mathematics and template entities

## Documentation

- [Dissertation (full)](documentation/Dissertation.Full.pdf) — theoretical foundations and design rationale
- [Dissertation (abridged)](documentation/Dissertation.Abridgment.pdf) — condensed overview
- [SQSS Tutorial](documentation/SQSSTutorial.odt) — walkthrough of the single-queue/single-server example
- [Coding Conventions](documentation/CodingConventions.odt) — project style guide
- [Logging Guide](documentation/MatthiasToolbox.Logging.pdf) — logging subsystem reference
- [Class Diagrams](design/) — UML diagrams for core subsystems
