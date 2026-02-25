# SimOpt — Simulation-Optimization Framework

.NET 9 framework coupling a discrete-event simulation engine with pluggable metaheuristic optimizers.

## Knowledge Loading

| Domain | Files | Load when... |
|--------|-------|-------------|
| Software Development | `~/.claude/domains/software-development/tdd-protocol.md` | Writing or modifying code |

## Key Files

| File | Purpose |
|------|---------|
| `session-context.md` | Current session state — **read first** |
| `backlog.md` | Prioritized backlog — read when active TODOs are done |
| `SimOpt.slnx` | Solution file (.NET 9, slnx format) |
| `documentation/CodingConventions.odt` | Project style guide |

## Cross-Project

| File | Purpose |
|------|---------|
| `~/claude-config/cross-project/inbox.md` | One-off cross-project tasks |

## Project Structure

| Path | Purpose |
|------|---------|
| `src/SimOpt.Basics` | Data structures, interfaces, geometry primitives |
| `src/SimOpt.Mathematics` | Probability distributions, RNGs, matrix operations |
| `src/SimOpt.Statistics` | Statistical analysis and output processing |
| `src/SimOpt.Simulation` | Discrete-event engine: entities, events, network, templates |
| `src/SimOpt.Optimization` | Strategy interfaces + EA, SA, PSO, random search implementations |
| `src/SimOpt.Learning` | Machine learning utilities |
| `src/SimOpt.Logging` | Cross-platform simulation logging |
| `src/SimOpt.Logging.Desktop` | Desktop logging sink |
| `tests/SimOpt.Tests` | xUnit test project (Basics, Simulation, Optimization) |
| `examples/` | Complete runnable models (SQSS, ED, SupplyChain, Warehouse, etc.) |
| `design/` | UML class diagrams |
| `documentation/` | Dissertation, tutorials, coding conventions |
| `data/` | Data files for models |
| `archive/` | Legacy code |

## Build & Test

```bash
# Build
dotnet build SimOpt.slnx

# Run tests
dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj

# Run an example
dotnet run --project examples/SimOpt.Examples.SQSS
```

## Architecture Notes

Three-layer design: `SimOpt.Simulation` (DES engine) + `SimOpt.Optimization` (strategy loop) + coupling via `IProblem.Evaluate()` which resets and runs the model to compute fitness. The three core optimization interfaces are `IStrategy`, `IProblem`, and `ISolution`. GitHub: [JeltzProstetnic/simopt](https://github.com/JeltzProstetnic/simopt).
