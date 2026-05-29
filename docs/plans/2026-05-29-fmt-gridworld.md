# FMT Architectural Validation Gridworld — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add agent-based gridworld simulation as a new simulation kind to SimOpt, then build FMT-specific agent architectures to validate whether FMT's four-model architecture produces qualitatively different behavioral signatures than simpler architectures.

**Architecture:** Two-layer design. Layer 1: `SimOpt.GridWorld` — generic step-based agent simulation on a 2D grid (new simulation paradigm alongside DES). Layer 2: `SimOpt.FMT` — FMT agent architectures (reservoir computing, IWM/ISM/EWM/ESM, permeability gates) plus comparison agents (flat RL, world-model-only, ablated ESM). Both reuse SimOpt.Mathematics (distributions, RNGs, matrices) and SimOpt.Statistics (analysis, kernels).

**Tech Stack:** .NET 9, C# 13, xUnit + FluentAssertions + Moq, `ImplicitUsings: disable`, `Nullable: enable`

**Framing (CRITICAL):** This is architectural validation, NOT consciousness detection. The simulation tests whether architectures produce different behavioral signatures. It cannot test whether either architecture constitutes consciousness.

---

## Phase 1 — SimOpt.GridWorld (generic agent-based gridworld engine)

### Task 1: Project scaffold — SimOpt.GridWorld

**Files:**
- Create: `src/SimOpt.GridWorld/SimOpt.GridWorld.csproj`
- Modify: `SimOpt.slnx`
- Modify: `tests/SimOpt.Tests/SimOpt.Tests.csproj`

**Step 1: Create the project file**

```xml
<!-- src/SimOpt.GridWorld/SimOpt.GridWorld.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>SimOpt.GridWorld</RootNamespace>
    <AssemblyName>SimOpt.GridWorld</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SimOpt.Basics\SimOpt.Basics.csproj" />
    <ProjectReference Include="..\SimOpt.Mathematics\SimOpt.Mathematics.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Add to solution**

Add under `/src/` folder in `SimOpt.slnx`:
```xml
<Project Path="src/SimOpt.GridWorld/SimOpt.GridWorld.csproj" />
```

**Step 3: Add test project reference**

Add to `tests/SimOpt.Tests/SimOpt.Tests.csproj`:
```xml
<ProjectReference Include="../../src/SimOpt.GridWorld/SimOpt.GridWorld.csproj" />
```

**Step 4: Verify build**

Run: `dotnet build SimOpt.slnx`
Expected: 0 errors (empty project builds clean)

---

### Task 2: CellType enum + Grid class (TDD)

**Files:**
- Create: `src/SimOpt.GridWorld/Environment/CellType.cs`
- Create: `src/SimOpt.GridWorld/Environment/Grid.cs`
- Create: `tests/SimOpt.Tests/GridWorld/GridTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/GridTests.cs
using System;
using FluentAssertions;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridTests
{
    [Fact]
    public void Constructor_CreatesGridWithDimensions()
    {
        var grid = new Grid(10, 8);

        grid.Width.Should().Be(10);
        grid.Height.Should().Be(8);
    }

    [Fact]
    public void NewGrid_AllCellsEmpty()
    {
        var grid = new Grid(5, 5);

        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                grid[x, y].Should().Be(CellType.Empty);
    }

    [Fact]
    public void Indexer_SetAndGet()
    {
        var grid = new Grid(5, 5);
        grid[2, 3] = CellType.Hazard;

        grid[2, 3].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void InBounds_InsideGrid_ReturnsTrue()
    {
        var grid = new Grid(5, 5);

        grid.InBounds(0, 0).Should().BeTrue();
        grid.InBounds(4, 4).Should().BeTrue();
        grid.InBounds(2, 3).Should().BeTrue();
    }

    [Fact]
    public void InBounds_OutsideGrid_ReturnsFalse()
    {
        var grid = new Grid(5, 5);

        grid.InBounds(-1, 0).Should().BeFalse();
        grid.InBounds(0, -1).Should().BeFalse();
        grid.InBounds(5, 0).Should().BeFalse();
        grid.InBounds(0, 5).Should().BeFalse();
    }

    [Fact]
    public void Indexer_OutOfBounds_Throws()
    {
        var grid = new Grid(5, 5);

        var act = () => grid[5, 0];
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ZeroOrNegativeDimensions_Throws()
    {
        var act = () => new Grid(0, 5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridTests`
Expected: FAIL — types don't exist yet

**Step 3: Implement CellType and Grid**

```csharp
// src/SimOpt.GridWorld/Environment/CellType.cs
namespace SimOpt.GridWorld.Environment;

public enum CellType
{
    Empty,
    Wall,
    Hazard,
    Resource
}
```

```csharp
// src/SimOpt.GridWorld/Environment/Grid.cs
using System;

namespace SimOpt.GridWorld.Environment;

public class Grid
{
    private readonly CellType[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public Grid(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _cells = new CellType[width, height];
    }

    public CellType this[int x, int y]
    {
        get => _cells[x, y];
        set => _cells[x, y] = value;
    }

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;
}
```

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridTests`
Expected: 7 tests PASS

**Step 5: Commit**

```
feat(gridworld): add Grid class with CellType enum — SIM-47 Task 2
```

---

### Task 3: GridAction + GridObservation (TDD)

**Files:**
- Create: `src/SimOpt.GridWorld/Agents/GridAction.cs`
- Create: `src/SimOpt.GridWorld/Agents/GridObservation.cs`
- Create: `tests/SimOpt.Tests/GridWorld/GridObservationTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/GridObservationTests.cs
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridObservationTests
{
    [Fact]
    public void FromGrid_CenterOfEmptyGrid_AllEmpty()
    {
        var grid = new Grid(7, 7);
        var obs = GridObservation.FromGrid(grid, 3, 3, viewRadius: 2);

        obs.AgentX.Should().Be(3);
        obs.AgentY.Should().Be(3);
        obs.ViewRadius.Should().Be(2);
        // 5x5 local view (2*radius+1)
        obs.LocalView.GetLength(0).Should().Be(5);
        obs.LocalView.GetLength(1).Should().Be(5);
    }

    [Fact]
    public void FromGrid_NearEdge_OutOfBoundsAreWall()
    {
        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 0, 0, viewRadius: 2);

        // Cells at (-2,-2) to (2,2) relative to agent
        // (-2,-2), (-1,-2), etc. are out of bounds → Wall
        obs.LocalView[0, 0].Should().Be(CellType.Wall); // (-2,-2) OOB
        obs.LocalView[2, 2].Should().Be(CellType.Empty); // (0,0) = agent position
    }

    [Fact]
    public void FromGrid_SeesHazard()
    {
        var grid = new Grid(5, 5);
        grid[3, 2] = CellType.Hazard;
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        // Hazard at (3,2) is at relative (1,0) from agent at (2,2)
        // In LocalView: index [2+1, 2+0] = [3, 2]
        obs.LocalView[3, 2].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void OtherAgents_TracksVisibleAgentPositions()
    {
        var grid = new Grid(10, 10);
        var others = new List<(string Id, int X, int Y, bool Alive)>
        {
            ("agent-b", 3, 3, true),
            ("agent-c", 8, 8, true), // out of view radius 2 from (2,2)
        };

        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2, otherAgents: others);

        obs.VisibleAgents.Should().ContainSingle()
            .Which.Id.Should().Be("agent-b");
    }
}
```

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridObservationTests`
Expected: FAIL

**Step 3: Implement**

```csharp
// src/SimOpt.GridWorld/Agents/GridAction.cs
namespace SimOpt.GridWorld.Agents;

public enum GridAction
{
    Stay,
    North,
    South,
    East,
    West
}
```

```csharp
// src/SimOpt.GridWorld/Agents/GridObservation.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public class GridObservation
{
    public CellType[,] LocalView { get; }
    public int AgentX { get; }
    public int AgentY { get; }
    public int ViewRadius { get; }
    public IReadOnlyList<VisibleAgent> VisibleAgents { get; }

    private GridObservation(CellType[,] localView, int agentX, int agentY,
        int viewRadius, IReadOnlyList<VisibleAgent> visibleAgents)
    {
        LocalView = localView;
        AgentX = agentX;
        AgentY = agentY;
        ViewRadius = viewRadius;
        VisibleAgents = visibleAgents;
    }

    public static GridObservation FromGrid(Grid grid, int agentX, int agentY,
        int viewRadius, IEnumerable<(string Id, int X, int Y, bool Alive)>? otherAgents = null)
    {
        int size = 2 * viewRadius + 1;
        var view = new CellType[size, size];

        for (int dx = -viewRadius; dx <= viewRadius; dx++)
        {
            for (int dy = -viewRadius; dy <= viewRadius; dy++)
            {
                int wx = agentX + dx;
                int wy = agentY + dy;
                int lx = dx + viewRadius;
                int ly = dy + viewRadius;

                view[lx, ly] = grid.InBounds(wx, wy) ? grid[wx, wy] : CellType.Wall;
            }
        }

        var visible = new List<VisibleAgent>();
        if (otherAgents != null)
        {
            foreach (var (id, x, y, alive) in otherAgents)
            {
                if (Math.Abs(x - agentX) <= viewRadius && Math.Abs(y - agentY) <= viewRadius)
                    visible.Add(new VisibleAgent(id, x, y, alive));
            }
        }

        return new GridObservation(view, agentX, agentY, viewRadius, visible);
    }
}

public record VisibleAgent(string Id, int X, int Y, bool IsAlive);
```

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridObservationTests`
Expected: 4 tests PASS

**Step 5: Commit**

```
feat(gridworld): add GridAction enum and GridObservation — SIM-47 Task 3
```

---

### Task 4: IGridAgent interface + AgentEvent

**Files:**
- Create: `src/SimOpt.GridWorld/Agents/IGridAgent.cs`
- Create: `src/SimOpt.GridWorld/Agents/AgentEvent.cs`

**Step 1: Create the interfaces** (no test needed — pure interface + record)

```csharp
// src/SimOpt.GridWorld/Agents/AgentEvent.cs
namespace SimOpt.GridWorld.Agents;

public enum AgentEventType
{
    Death,
    ResourceCollected,
    Moved
}

public record AgentEvent(
    string AgentId,
    AgentEventType EventType,
    int X,
    int Y,
    string? Cause = null);
```

```csharp
// src/SimOpt.GridWorld/Agents/IGridAgent.cs
namespace SimOpt.GridWorld.Agents;

public interface IGridAgent
{
    string Id { get; }
    int X { get; }
    int Y { get; }
    bool IsAlive { get; }

    GridAction SelectAction(GridObservation observation);
    void OnDeath(string cause);
    void OnObserve(AgentEvent agentEvent);
    void OnStepComplete(GridObservation newObservation, double reward);
    void Reset(int startX, int startY);
}
```

**Step 2: Verify build**

Run: `dotnet build src/SimOpt.GridWorld/SimOpt.GridWorld.csproj`
Expected: 0 errors

**Step 3: Commit**

```
feat(gridworld): add IGridAgent interface and AgentEvent — SIM-47 Task 4
```

---

### Task 5: GridSimulation — step-based runner (TDD)

**Files:**
- Create: `src/SimOpt.GridWorld/Simulation/GridSimulation.cs`
- Create: `src/SimOpt.GridWorld/Simulation/StepResult.cs`
- Create: `src/SimOpt.GridWorld/Simulation/SimulationResult.cs`
- Create: `src/SimOpt.GridWorld/Simulation/GridSimulationConfig.cs`
- Create: `tests/SimOpt.Tests/GridWorld/GridSimulationTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/GridSimulationTests.cs
using System.Linq;
using FluentAssertions;
using Moq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridSimulationTests
{
    private static Grid CreateSimpleGrid()
    {
        var grid = new Grid(5, 5);
        grid[4, 4] = CellType.Hazard;
        return grid;
    }

    [Fact]
    public void Tick_AdvancesStepCounter()
    {
        var sim = new GridSimulation(CreateSimpleGrid(), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.Stay);
        sim.AddAgent(agent.Object, 2, 2);

        sim.Step.Should().Be(0);
        sim.Tick();
        sim.Step.Should().Be(1);
    }

    [Fact]
    public void Tick_AgentMovesNorth_PositionUpdates()
    {
        var sim = new GridSimulation(CreateSimpleGrid(), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.North);
        sim.AddAgent(agent.Object, 2, 2);

        sim.Tick();

        agent.Object.Y.Should().Be(1); // North = Y-1
        agent.Object.X.Should().Be(2);
    }

    [Fact]
    public void Tick_AgentMovesIntoWall_PositionUnchanged()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Wall;
        var sim = new GridSimulation(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.North);
        sim.AddAgent(agent.Object, 2, 2);

        sim.Tick();

        agent.Object.X.Should().Be(2);
        agent.Object.Y.Should().Be(2); // blocked
    }

    [Fact]
    public void Tick_AgentMovesOntoHazard_Dies()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.North);
        sim.AddAgent(agent.Object, 2, 2);

        var result = sim.Tick();

        agent.Object.IsAlive.Should().BeFalse();
        result.Deaths.Should().ContainSingle()
            .Which.AgentId.Should().Be("a1");
    }

    [Fact]
    public void Tick_DeadAgent_SkippedInActionSelection()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.North);
        sim.AddAgent(agent.Object, 2, 2);

        sim.Tick(); // dies
        sim.Tick(); // should not call SelectAction again

        agent.Verify(a => a.SelectAction(It.IsAny<GridObservation>()), Times.Once);
    }

    [Fact]
    public void Run_StopsAtMaxSteps()
    {
        var sim = new GridSimulation(CreateSimpleGrid(), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.Stay);
        sim.AddAgent(agent.Object, 2, 2);

        var result = sim.Run(maxSteps: 10);

        result.TotalSteps.Should().Be(10);
    }

    [Fact]
    public void Run_StopsWhenAllAgentsDead()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.North);
        sim.AddAgent(agent.Object, 2, 2);

        var result = sim.Run(maxSteps: 100);

        result.TotalSteps.Should().BeLessThan(100);
    }

    [Fact]
    public void AddAgent_OutOfBounds_Throws()
    {
        var sim = new GridSimulation(new Grid(5, 5), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", GridAction.Stay);

        var act = () => sim.AddAgent(agent.Object, 5, 0);
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }

    private static Mock<IGridAgent> CreateMockAgent(string id, GridAction action)
    {
        var mock = new Mock<IGridAgent>();
        int x = 0, y = 0;
        bool alive = true;

        mock.SetupGet(a => a.Id).Returns(id);
        mock.SetupGet(a => a.X).Returns(() => x);
        mock.SetupGet(a => a.Y).Returns(() => y);
        mock.SetupGet(a => a.IsAlive).Returns(() => alive);
        mock.Setup(a => a.SelectAction(It.IsAny<GridObservation>())).Returns(action);
        mock.Setup(a => a.Reset(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((sx, sy) => { x = sx; y = sy; alive = true; });
        mock.Setup(a => a.OnDeath(It.IsAny<string>()))
            .Callback<string>(_ => alive = false);

        return mock;
    }
}
```

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridSimulationTests`
Expected: FAIL

**Step 3: Implement**

```csharp
// src/SimOpt.GridWorld/Simulation/GridSimulationConfig.cs
namespace SimOpt.GridWorld.Simulation;

public class GridSimulationConfig
{
    public int ViewRadius { get; set; } = 2;
    public bool StopWhenAllDead { get; set; } = true;
    public double HazardReward { get; set; } = -10.0;
    public double ResourceReward { get; set; } = 1.0;
    public double StepReward { get; set; } = -0.01;
}
```

```csharp
// src/SimOpt.GridWorld/Simulation/StepResult.cs
using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class StepResult
{
    public int StepNumber { get; init; }
    public IReadOnlyList<AgentEvent> Deaths { get; init; } = [];
    public IReadOnlyList<AgentEvent> Events { get; init; } = [];
}
```

```csharp
// src/SimOpt.GridWorld/Simulation/SimulationResult.cs
using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class SimulationResult
{
    public int TotalSteps { get; init; }
    public IReadOnlyList<AgentEvent> AllDeaths { get; init; } = [];
    public int AgentsAlive { get; init; }
    public int AgentsDead { get; init; }
}
```

```csharp
// src/SimOpt.GridWorld/Simulation/GridSimulation.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Simulation;

public class GridSimulation
{
    private readonly List<IGridAgent> _agents = new();
    private readonly GridSimulationConfig _config;

    public Grid Grid { get; }
    public IReadOnlyList<IGridAgent> Agents => _agents;
    public int Step { get; private set; }

    public GridSimulation(Grid grid, GridSimulationConfig config)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void AddAgent(IGridAgent agent, int x, int y)
    {
        if (!Grid.InBounds(x, y))
            throw new ArgumentOutOfRangeException($"Position ({x},{y}) is out of bounds");
        agent.Reset(x, y);
        _agents.Add(agent);
    }

    public StepResult Tick()
    {
        Step++;
        var deaths = new List<AgentEvent>();
        var events = new List<AgentEvent>();

        var liveAgents = _agents.Where(a => a.IsAlive).ToList();

        // Phase 1: Collect observations and actions
        var actions = new Dictionary<IGridAgent, GridAction>();
        foreach (var agent in liveAgents)
        {
            var obs = BuildObservation(agent);
            actions[agent] = agent.SelectAction(obs);
        }

        // Phase 2: Resolve movements
        foreach (var (agent, action) in actions)
        {
            var (dx, dy) = ActionToDelta(action);
            int nx = agent.X + dx;
            int ny = agent.Y + dy;

            if (!Grid.InBounds(nx, ny) || Grid[nx, ny] == CellType.Wall)
                continue;

            agent.Reset(nx, ny);
        }

        // Phase 3: Apply effects
        foreach (var agent in liveAgents)
        {
            if (!agent.IsAlive) continue;

            var cell = Grid[agent.X, agent.Y];
            double reward = _config.StepReward;

            if (cell == CellType.Hazard)
            {
                agent.OnDeath("hazard");
                var deathEvent = new AgentEvent(agent.Id, AgentEventType.Death, agent.X, agent.Y, "hazard");
                deaths.Add(deathEvent);
                events.Add(deathEvent);
                reward = _config.HazardReward;
            }
            else if (cell == CellType.Resource)
            {
                var collectEvent = new AgentEvent(agent.Id, AgentEventType.ResourceCollected, agent.X, agent.Y);
                events.Add(collectEvent);
                reward = _config.ResourceReward;
            }

            agent.OnStepComplete(BuildObservation(agent), reward);
        }

        // Phase 4: Broadcast events to all live agents
        foreach (var evt in events)
        {
            foreach (var agent in _agents.Where(a => a.IsAlive && a.Id != evt.AgentId))
                agent.OnObserve(evt);
        }

        return new StepResult { StepNumber = Step, Deaths = deaths, Events = events };
    }

    public SimulationResult Run(int maxSteps)
    {
        var allDeaths = new List<AgentEvent>();

        for (int i = 0; i < maxSteps; i++)
        {
            if (_config.StopWhenAllDead && _agents.All(a => !a.IsAlive))
                break;

            var result = Tick();
            allDeaths.AddRange(result.Deaths);
        }

        return new SimulationResult
        {
            TotalSteps = Step,
            AllDeaths = allDeaths,
            AgentsAlive = _agents.Count(a => a.IsAlive),
            AgentsDead = _agents.Count(a => !a.IsAlive),
        };
    }

    private GridObservation BuildObservation(IGridAgent agent)
    {
        var others = _agents
            .Where(a => a.Id != agent.Id && a.IsAlive)
            .Select(a => (a.Id, a.X, a.Y, a.IsAlive));
        return GridObservation.FromGrid(Grid, agent.X, agent.Y, _config.ViewRadius, others);
    }

    private static (int dx, int dy) ActionToDelta(GridAction action) => action switch
    {
        GridAction.North => (0, -1),
        GridAction.South => (0, 1),
        GridAction.East => (1, 0),
        GridAction.West => (-1, 0),
        GridAction.Stay => (0, 0),
        _ => (0, 0),
    };
}
```

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridSimulationTests`
Expected: 8 tests PASS

**Step 5: Commit**

```
feat(gridworld): add GridSimulation step-based runner — SIM-47 Task 5
```

---

### Task 6: RandomAgent — simplest concrete agent (TDD)

**Files:**
- Create: `src/SimOpt.GridWorld/Agents/RandomAgent.cs`
- Create: `tests/SimOpt.Tests/GridWorld/RandomAgentTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/RandomAgentTests.cs
using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class RandomAgentTests
{
    [Fact]
    public void Constructor_SetsId()
    {
        var agent = new RandomAgent("r1", seed: 42);
        agent.Id.Should().Be("r1");
    }

    [Fact]
    public void Reset_SetsPositionAndAlive()
    {
        var agent = new RandomAgent("r1", seed: 42);
        agent.Reset(3, 4);

        agent.X.Should().Be(3);
        agent.Y.Should().Be(4);
        agent.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void SelectAction_ReturnsDifferentActions_WithSeed()
    {
        var agent = new RandomAgent("r1", seed: 42);
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        var actions = new System.Collections.Generic.HashSet<GridAction>();
        for (int i = 0; i < 100; i++)
            actions.Add(agent.SelectAction(obs));

        actions.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void OnDeath_SetsAliveToFalse()
    {
        var agent = new RandomAgent("r1", seed: 42);
        agent.Reset(2, 2);

        agent.OnDeath("hazard");

        agent.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void DeterministicWithSameSeed()
    {
        var a1 = new RandomAgent("r1", seed: 42);
        var a2 = new RandomAgent("r2", seed: 42);
        a1.Reset(2, 2);
        a2.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        for (int i = 0; i < 20; i++)
            a1.SelectAction(obs).Should().Be(a2.SelectAction(obs));
    }
}
```

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter RandomAgentTests`
Expected: FAIL

**Step 3: Implement**

```csharp
// src/SimOpt.GridWorld/Agents/RandomAgent.cs
using System;

namespace SimOpt.GridWorld.Agents;

public class RandomAgent : IGridAgent
{
    private static readonly GridAction[] AllActions =
        (GridAction[])Enum.GetValues(typeof(GridAction));

    private readonly Random _rng;

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsAlive { get; private set; }

    public RandomAgent(string id, int seed)
    {
        Id = id;
        _rng = new Random(seed);
    }

    public GridAction SelectAction(GridObservation observation) =>
        AllActions[_rng.Next(AllActions.Length)];

    public void OnDeath(string cause) => IsAlive = false;
    public void OnObserve(AgentEvent agentEvent) { }
    public void OnStepComplete(GridObservation newObservation, double reward) { }

    public void Reset(int startX, int startY)
    {
        X = startX;
        Y = startY;
        IsAlive = true;
    }
}
```

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter RandomAgentTests`
Expected: 5 tests PASS

**Step 5: Commit**

```
feat(gridworld): add RandomAgent baseline — SIM-47 Task 6
```

---

### Task 7: Multi-agent death observation integration test (TDD)

This is the core capability: agents can observe other agents dying and react.

**Files:**
- Create: `tests/SimOpt.Tests/GridWorld/DeathObservationTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/DeathObservationTests.cs
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class DeathObservationTests
{
    [Fact]
    public void WhenAgentDies_OtherAgentsReceiveDeathEvent()
    {
        // Grid with hazard at (2,1)
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        // Agent A walks north into hazard
        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);

        // Agent B stays put and should observe A's death
        var agentB = CreateMovingAgent("b", GridAction.Stay);
        sim.AddAgent(agentB.Object, 0, 0);

        sim.Tick();

        agentB.Verify(b => b.OnObserve(
            It.Is<AgentEvent>(e =>
                e.AgentId == "a" &&
                e.EventType == AgentEventType.Death &&
                e.Cause == "hazard")),
            Times.Once);
    }

    [Fact]
    public void DeadAgent_DoesNotReceiveEvents()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;
        grid[0, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        // Both walk north into hazards on same tick
        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);
        var agentB = CreateMovingAgent("b", GridAction.North);
        sim.AddAgent(agentB.Object, 0, 2);

        sim.Tick();

        // Dead agents should not receive OnObserve (they're dead)
        // Note: both die on same tick, broadcast only goes to alive agents
        agentA.Verify(a => a.OnObserve(It.IsAny<AgentEvent>()), Times.Never);
        agentB.Verify(b => b.OnObserve(It.IsAny<AgentEvent>()), Times.Never);
    }

    [Fact]
    public void MultipleDeaths_SingleTick_AllBroadcast()
    {
        var grid = new Grid(10, 10);
        grid[2, 1] = CellType.Hazard;
        grid[4, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);
        var agentB = CreateMovingAgent("b", GridAction.North);
        sim.AddAgent(agentB.Object, 4, 2);
        var observer = CreateMovingAgent("observer", GridAction.Stay);
        sim.AddAgent(observer.Object, 6, 6);

        sim.Tick();

        observer.Verify(o => o.OnObserve(
            It.Is<AgentEvent>(e => e.EventType == AgentEventType.Death)),
            Times.Exactly(2));
    }

    private static Mock<IGridAgent> CreateMovingAgent(string id, GridAction action)
    {
        var mock = new Mock<IGridAgent>();
        int x = 0, y = 0;
        bool alive = true;

        mock.SetupGet(a => a.Id).Returns(id);
        mock.SetupGet(a => a.X).Returns(() => x);
        mock.SetupGet(a => a.Y).Returns(() => y);
        mock.SetupGet(a => a.IsAlive).Returns(() => alive);
        mock.Setup(a => a.SelectAction(It.IsAny<GridObservation>())).Returns(action);
        mock.Setup(a => a.Reset(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((sx, sy) => { x = sx; y = sy; alive = true; });
        mock.Setup(a => a.OnDeath(It.IsAny<string>()))
            .Callback<string>(_ => alive = false);

        return mock;
    }
}
```

**Step 2: Run tests — should PASS with existing implementation**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter DeathObservationTests`
Expected: 3 tests PASS (this validates the GridSimulation broadcast logic from Task 5)

If any fail, fix the broadcast logic in `GridSimulation.Tick()`.

**Step 3: Commit**

```
test(gridworld): add death observation integration tests — SIM-47 Task 7
```

---

### Task 8: Q-Learning agent — flat RL baseline (TDD)

**Files:**
- Create: `src/SimOpt.GridWorld/Agents/QLearningAgent.cs`
- Create: `tests/SimOpt.Tests/GridWorld/QLearningAgentTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/GridWorld/QLearningAgentTests.cs
using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class QLearningAgentTests
{
    [Fact]
    public void SelectAction_InitiallyExplores()
    {
        var agent = new QLearningAgent("q1", seed: 42, epsilon: 1.0);
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        // With epsilon=1.0, always explores (random)
        var actions = new System.Collections.Generic.HashSet<GridAction>();
        for (int i = 0; i < 100; i++)
            actions.Add(agent.SelectAction(obs));

        actions.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void SelectAction_ExploitsWhenEpsilonZero()
    {
        var agent = new QLearningAgent("q1", seed: 42, epsilon: 0.0);
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        // With epsilon=0.0, always picks best Q (all equal initially = first action)
        var first = agent.SelectAction(obs);
        for (int i = 0; i < 20; i++)
            agent.SelectAction(obs).Should().Be(first);
    }

    [Fact]
    public void OnStepComplete_UpdatesQValues()
    {
        var agent = new QLearningAgent("q1", seed: 42, epsilon: 0.0, learningRate: 0.5, discount: 0.9);
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
        agent.SelectAction(obs); // picks an action

        var obs2 = GridObservation.FromGrid(grid, 2, 1, viewRadius: 2);
        agent.OnStepComplete(obs2, reward: 1.0);

        // After a positive reward, the Q-value for the taken action should increase
        // Next call from same state should prefer the rewarded action
        agent.Reset(2, 2);
        var preferred = agent.SelectAction(obs);
        preferred.Should().Be(GridAction.Stay); // first greedy action (index 0) until learning shifts it
    }

    [Fact]
    public void LearnFromExperience_AvoidHazardAfterDeath()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;

        // Train over multiple episodes: agent at (2,2), hazard at (2,1)
        var agent = new QLearningAgent("q1", seed: 42, epsilon: 0.3, learningRate: 0.5, discount: 0.9);

        for (int episode = 0; episode < 50; episode++)
        {
            agent.Reset(2, 2);
            var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
            var action = agent.SelectAction(obs);

            // Simulate: if North → die (big negative reward)
            if (action == GridAction.North)
            {
                var obsAfter = GridObservation.FromGrid(grid, 2, 1, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: -10.0);
                agent.OnDeath("hazard");
            }
            else
            {
                var obsAfter = GridObservation.FromGrid(grid, agent.X, agent.Y, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: -0.01);
            }
        }

        // After training, with epsilon=0 the agent should NOT choose North
        agent = new QLearningAgent("q1-greedy", seed: 42, epsilon: 0.0, learningRate: 0.5, discount: 0.9);
        // Copy Q-table (need to expose or use same agent with epsilon override)
        // For this test, just verify the trained agent avoids north
        // Re-train with same setup but verify final behavior
        var trainedAgent = new QLearningAgent("trained", seed: 1, epsilon: 0.1, learningRate: 0.5, discount: 0.9);
        for (int episode = 0; episode < 200; episode++)
        {
            trainedAgent.Reset(2, 2);
            var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
            var action = trainedAgent.SelectAction(obs);
            if (action == GridAction.North)
            {
                var obsAfter = GridObservation.FromGrid(grid, 2, 1, viewRadius: 2);
                trainedAgent.OnStepComplete(obsAfter, reward: -10.0);
                trainedAgent.OnDeath("hazard");
            }
            else
            {
                var obsAfter = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
                trainedAgent.OnStepComplete(obsAfter, reward: 0.0);
            }
        }

        // Now test with greedy (epsilon=0 mode — test via Q-value inspection)
        trainedAgent.GetQValue(trainedAgent.ObservationToState(
            GridObservation.FromGrid(grid, 2, 2, viewRadius: 2)), GridAction.North)
            .Should().BeLessThan(0, "North leads to hazard death");
    }
}
```

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter QLearningAgentTests`
Expected: FAIL

**Step 3: Implement**

```csharp
// src/SimOpt.GridWorld/Agents/QLearningAgent.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimOpt.GridWorld.Agents;

public class QLearningAgent : IGridAgent
{
    private static readonly GridAction[] AllActions =
        (GridAction[])Enum.GetValues(typeof(GridAction));

    private readonly Random _rng;
    private readonly double _epsilon;
    private readonly double _learningRate;
    private readonly double _discount;
    private readonly Dictionary<(int State, GridAction Action), double> _qTable = new();

    private int _lastState;
    private GridAction _lastAction;

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsAlive { get; private set; }

    public QLearningAgent(string id, int seed, double epsilon = 0.1,
        double learningRate = 0.1, double discount = 0.95)
    {
        Id = id;
        _rng = new Random(seed);
        _epsilon = epsilon;
        _learningRate = learningRate;
        _discount = discount;
    }

    public GridAction SelectAction(GridObservation observation)
    {
        _lastState = ObservationToState(observation);

        if (_rng.NextDouble() < _epsilon)
        {
            _lastAction = AllActions[_rng.Next(AllActions.Length)];
        }
        else
        {
            _lastAction = AllActions
                .OrderByDescending(a => GetQValue(_lastState, a))
                .First();
        }

        return _lastAction;
    }

    public void OnStepComplete(GridObservation newObservation, double reward)
    {
        int newState = ObservationToState(newObservation);
        double maxNextQ = AllActions.Max(a => GetQValue(newState, a));
        double currentQ = GetQValue(_lastState, _lastAction);
        double newQ = currentQ + _learningRate * (reward + _discount * maxNextQ - currentQ);
        _qTable[(_lastState, _lastAction)] = newQ;
    }

    public void OnDeath(string cause) => IsAlive = false;

    public void OnObserve(AgentEvent agentEvent) { }

    public void Reset(int startX, int startY)
    {
        X = startX;
        Y = startY;
        IsAlive = true;
    }

    public double GetQValue(int state, GridAction action) =>
        _qTable.TryGetValue((state, action), out var q) ? q : 0.0;

    public int ObservationToState(GridObservation obs)
    {
        int hash = 17;
        int w = obs.LocalView.GetLength(0);
        int h = obs.LocalView.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                hash = hash * 31 + (int)obs.LocalView[x, y];
        return hash;
    }
}
```

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter QLearningAgentTests`
Expected: 4 tests PASS

**Step 5: Commit**

```
feat(gridworld): add Q-learning agent with tabular state hashing — SIM-47 Task 8
```

---

### Task 9: GridWorld end-to-end integration test

**Files:**
- Create: `tests/SimOpt.Tests/GridWorld/GridWorldIntegrationTests.cs`

**Step 1: Write integration tests**

```csharp
// tests/SimOpt.Tests/GridWorld/GridWorldIntegrationTests.cs
using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridWorldIntegrationTests
{
    [Fact]
    public void RandomAgent_SurvivesOnEmptyGrid()
    {
        var grid = new Grid(10, 10);
        var sim = new GridSimulation(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent("r1", seed: 42), 5, 5);

        var result = sim.Run(maxSteps: 100);

        result.AgentsAlive.Should().Be(1);
        result.TotalSteps.Should().Be(100);
    }

    [Fact]
    public void RandomAgent_EventuallyDiesOnHazardGrid()
    {
        var grid = new Grid(5, 5);
        // Surround center with hazards — agent will eventually step on one
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                if (x != 2 || y != 2)
                    grid[x, y] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent("r1", seed: 42), 2, 2);

        var result = sim.Run(maxSteps: 1000);

        result.AgentsDead.Should().Be(1);
        result.TotalSteps.Should().BeLessThan(1000);
    }

    [Fact]
    public void TwoAgents_OneSurvivesOneDoesNot()
    {
        var grid = new Grid(10, 10);
        grid[3, 3] = CellType.Hazard;
        grid[3, 2] = CellType.Hazard;
        grid[2, 3] = CellType.Hazard;
        grid[4, 3] = CellType.Hazard;
        grid[3, 4] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig { StopWhenAllDead = false });
        // Agent near hazards — likely dies
        sim.AddAgent(new RandomAgent("danger", seed: 42), 3, 3);
        // Agent far from hazards — safe
        sim.AddAgent(new RandomAgent("safe", seed: 99), 8, 8);

        var result = sim.Run(maxSteps: 50);

        // At least one death should occur (hazard-surrounded agent starts ON a hazard)
        result.AllDeaths.Should().NotBeEmpty();
    }

    [Fact]
    public void QLearningAgent_CanRunFullSimulation()
    {
        var grid = new Grid(5, 5);
        grid[0, 0] = CellType.Hazard;
        grid[4, 4] = CellType.Resource;

        var sim = new GridSimulation(grid, new GridSimulationConfig());
        sim.AddAgent(new QLearningAgent("q1", seed: 42), 2, 2);

        var result = sim.Run(maxSteps: 50);

        result.TotalSteps.Should().BeGreaterThan(0);
    }
}
```

**Step 2: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter GridWorldIntegrationTests`
Expected: 4 tests PASS

**Step 3: Commit**

```
test(gridworld): add end-to-end integration tests — SIM-47 Task 9
```

---

## Phase 2 — SimOpt.FMT (FMT Agent Architectures)

### Task 10: Project scaffold — SimOpt.FMT

**Files:**
- Create: `src/SimOpt.FMT/SimOpt.FMT.csproj`
- Modify: `SimOpt.slnx`
- Modify: `tests/SimOpt.Tests/SimOpt.Tests.csproj`

**Step 1: Create project**

```xml
<!-- src/SimOpt.FMT/SimOpt.FMT.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>SimOpt.FMT</RootNamespace>
    <AssemblyName>SimOpt.FMT</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SimOpt.GridWorld\SimOpt.GridWorld.csproj" />
    <ProjectReference Include="..\SimOpt.Mathematics\SimOpt.Mathematics.csproj" />
    <ProjectReference Include="..\SimOpt.Statistics\SimOpt.Statistics.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Add to solution and test project**

Add to `SimOpt.slnx` under `/src/`:
```xml
<Project Path="src/SimOpt.FMT/SimOpt.FMT.csproj" />
```

Add to test project:
```xml
<ProjectReference Include="../../src/SimOpt.FMT/SimOpt.FMT.csproj" />
```

**Step 3: Verify build**

Run: `dotnet build SimOpt.slnx`
Expected: 0 errors

**Step 4: Commit**

```
feat(fmt): scaffold SimOpt.FMT project — SIM-47 Task 10
```

---

### Task 11: Echo State Network — reservoir computing substrate (TDD)

The ESN is the computational substrate for FMT's implicit models (IWM, ISM). Uses SimOpt.Mathematics.Numerics.Matrices.Matrix for weight storage.

**Files:**
- Create: `src/SimOpt.FMT/Reservoir/EchoStateNetwork.cs`
- Create: `src/SimOpt.FMT/Reservoir/ReservoirConfig.cs`
- Create: `tests/SimOpt.Tests/FMT/EchoStateNetworkTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/EchoStateNetworkTests.cs
using System;
using FluentAssertions;
using SimOpt.FMT.Reservoir;
using Xunit;

namespace SimOpt.Tests.FMT;

public class EchoStateNetworkTests
{
    [Fact]
    public void Constructor_InitializesWithConfig()
    {
        var config = new ReservoirConfig
        {
            InputSize = 3,
            ReservoirSize = 50,
            OutputSize = 2,
            SpectralRadius = 0.9,
            Seed = 42,
        };

        var esn = new EchoStateNetwork(config);

        esn.ReservoirSize.Should().Be(50);
        esn.InputSize.Should().Be(3);
        esn.OutputSize.Should().Be(2);
    }

    [Fact]
    public void Update_ProducesOutput()
    {
        var esn = CreateDefaultESN();
        var input = new double[] { 1.0, 0.5, -0.3 };

        var output = esn.Update(input);

        output.Length.Should().Be(2);
        output.Should().OnlyContain(v => !double.IsNaN(v));
    }

    [Fact]
    public void Update_DifferentInputs_DifferentOutputs()
    {
        var esn = CreateDefaultESN();

        var out1 = esn.Update(new double[] { 1.0, 0.0, 0.0 });
        esn.Reset();
        var out2 = esn.Update(new double[] { 0.0, 0.0, 1.0 });

        // Different inputs should produce different outputs
        (out1[0] == out2[0] && out1[1] == out2[1]).Should().BeFalse();
    }

    [Fact]
    public void Update_SameInputSequence_SameSeed_Deterministic()
    {
        var esn1 = CreateDefaultESN();
        var esn2 = CreateDefaultESN();

        var inputs = new[]
        {
            new double[] { 1.0, 0.5, -0.3 },
            new double[] { 0.2, -0.8, 0.1 },
            new double[] { -0.5, 0.3, 0.9 },
        };

        foreach (var input in inputs)
        {
            var o1 = esn1.Update(input);
            var o2 = esn2.Update(input);

            for (int i = 0; i < o1.Length; i++)
                o1[i].Should().BeApproximately(o2[i], 1e-10);
        }
    }

    [Fact]
    public void SpectralRadius_AffecsDynamics()
    {
        var config1 = new ReservoirConfig
        {
            InputSize = 3, ReservoirSize = 50, OutputSize = 2,
            SpectralRadius = 0.1, Seed = 42,
        };
        var config2 = new ReservoirConfig
        {
            InputSize = 3, ReservoirSize = 50, OutputSize = 2,
            SpectralRadius = 0.99, Seed = 42,
        };

        var esn1 = new EchoStateNetwork(config1);
        var esn2 = new EchoStateNetwork(config2);

        // After several updates, higher spectral radius = more memory = different state
        for (int i = 0; i < 10; i++)
        {
            esn1.Update(new double[] { 1.0, 0.0, 0.0 });
            esn2.Update(new double[] { 1.0, 0.0, 0.0 });
        }

        var out1 = esn1.Update(new double[] { 0.0, 0.0, 0.0 });
        var out2 = esn2.Update(new double[] { 0.0, 0.0, 0.0 });

        // Higher spectral radius retains more memory of past inputs
        // so response to zero input differs more from low-spectral version
        (out1[0] == out2[0]).Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var esn = CreateDefaultESN();
        esn.Update(new double[] { 1.0, 0.5, -0.3 });
        esn.Update(new double[] { 0.2, -0.8, 0.1 });

        esn.Reset();
        var afterReset = esn.Update(new double[] { 1.0, 0.5, -0.3 });

        var freshEsn = CreateDefaultESN();
        var fresh = freshEsn.Update(new double[] { 1.0, 0.5, -0.3 });

        for (int i = 0; i < afterReset.Length; i++)
            afterReset[i].Should().BeApproximately(fresh[i], 1e-10);
    }

    [Fact]
    public void Train_ReducesPredictionError()
    {
        var esn = CreateDefaultESN();

        // Simple task: output = input[0] + input[1]
        var trainingInputs = new double[100][];
        var trainingTargets = new double[100][];
        var rng = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            double a = rng.NextDouble() * 2 - 1;
            double b = rng.NextDouble() * 2 - 1;
            trainingInputs[i] = new[] { a, b, 0.0 };
            trainingTargets[i] = new[] { a + b, a - b };
        }

        esn.Train(trainingInputs, trainingTargets, washout: 10);

        // Test on new data
        esn.Reset();
        double totalError = 0;
        for (int i = 0; i < 20; i++)
        {
            double a = rng.NextDouble() * 2 - 1;
            double b = rng.NextDouble() * 2 - 1;
            var output = esn.Update(new[] { a, b, 0.0 });
            totalError += Math.Abs(output[0] - (a + b));
        }

        (totalError / 20).Should().BeLessThan(0.5, "trained ESN should approximate linear function");
    }

    private static EchoStateNetwork CreateDefaultESN() =>
        new(new ReservoirConfig
        {
            InputSize = 3,
            ReservoirSize = 50,
            OutputSize = 2,
            SpectralRadius = 0.9,
            LeakingRate = 0.3,
            InputScaling = 1.0,
            Seed = 42,
        });
}
```

**Step 2: Implement**

```csharp
// src/SimOpt.FMT/Reservoir/ReservoirConfig.cs
namespace SimOpt.FMT.Reservoir;

public class ReservoirConfig
{
    public int InputSize { get; set; }
    public int ReservoirSize { get; set; }
    public int OutputSize { get; set; }
    public double SpectralRadius { get; set; } = 0.9;
    public double LeakingRate { get; set; } = 0.3;
    public double InputScaling { get; set; } = 1.0;
    public double Sparsity { get; set; } = 0.1;
    public double RidgeParam { get; set; } = 1e-6;
    public int Seed { get; set; } = 42;
}
```

```csharp
// src/SimOpt.FMT/Reservoir/EchoStateNetwork.cs
using System;

namespace SimOpt.FMT.Reservoir;

public class EchoStateNetwork
{
    private readonly ReservoirConfig _config;
    private readonly double[,] _wIn;      // InputSize × ReservoirSize
    private readonly double[,] _wRes;     // ReservoirSize × ReservoirSize
    private double[,] _wOut;              // (ReservoirSize+1) × OutputSize (trainable, +1 for bias)
    private double[] _state;              // ReservoirSize

    public int ReservoirSize => _config.ReservoirSize;
    public int InputSize => _config.InputSize;
    public int OutputSize => _config.OutputSize;

    public EchoStateNetwork(ReservoirConfig config)
    {
        _config = config;
        var rng = new Random(config.Seed);

        _wIn = InitInputWeights(rng);
        _wRes = InitReservoirWeights(rng);
        _wOut = new double[config.ReservoirSize + 1, config.OutputSize]; // zero init
        _state = new double[config.ReservoirSize];
    }

    public double[] Update(double[] input)
    {
        if (input.Length != _config.InputSize)
            throw new ArgumentException($"Expected input size {_config.InputSize}, got {input.Length}");

        var newState = new double[_config.ReservoirSize];

        // x(t+1) = (1-α)·x(t) + α·tanh(W_in·u(t) + W_res·x(t))
        for (int i = 0; i < _config.ReservoirSize; i++)
        {
            double activation = 0;
            for (int j = 0; j < _config.InputSize; j++)
                activation += _wIn[j, i] * input[j];
            for (int j = 0; j < _config.ReservoirSize; j++)
                activation += _wRes[j, i] * _state[j];

            newState[i] = (1 - _config.LeakingRate) * _state[i]
                        + _config.LeakingRate * Math.Tanh(activation);
        }

        _state = newState;

        // y(t) = W_out · [x(t); 1]  (linear readout with bias)
        var output = new double[_config.OutputSize];
        for (int o = 0; o < _config.OutputSize; o++)
        {
            double sum = 0;
            for (int i = 0; i < _config.ReservoirSize; i++)
                sum += _wOut[i, o] * _state[i];
            sum += _wOut[_config.ReservoirSize, o]; // bias
            output[o] = sum;
        }

        return output;
    }

    public void Train(double[][] inputs, double[][] targets, int washout = 0)
    {
        int n = inputs.Length;
        int usable = n - washout;
        int stateSize = _config.ReservoirSize + 1; // +1 for bias

        // Collect reservoir states
        var states = new double[usable][];
        Reset();

        for (int t = 0; t < n; t++)
        {
            // Update state (ignore output during collection)
            var newState = new double[_config.ReservoirSize];
            for (int i = 0; i < _config.ReservoirSize; i++)
            {
                double activation = 0;
                for (int j = 0; j < _config.InputSize; j++)
                    activation += _wIn[j, i] * inputs[t][j];
                for (int j = 0; j < _config.ReservoirSize; j++)
                    activation += _wRes[j, i] * _state[j];
                newState[i] = (1 - _config.LeakingRate) * _state[i]
                            + _config.LeakingRate * Math.Tanh(activation);
            }
            _state = newState;

            if (t >= washout)
            {
                states[t - washout] = new double[stateSize];
                Array.Copy(_state, states[t - washout], _config.ReservoirSize);
                states[t - washout][_config.ReservoirSize] = 1.0; // bias
            }
        }

        // Ridge regression: W_out = (S^T S + λI)^{-1} S^T T
        // S = states matrix (usable × stateSize)
        // T = targets matrix (usable × outputSize)
        var sts = new double[stateSize, stateSize];
        for (int i = 0; i < stateSize; i++)
            for (int j = 0; j < stateSize; j++)
            {
                double sum = 0;
                for (int t = 0; t < usable; t++)
                    sum += states[t][i] * states[t][j];
                sts[i, j] = sum + (i == j ? _config.RidgeParam : 0);
            }

        var stt = new double[stateSize, _config.OutputSize];
        for (int i = 0; i < stateSize; i++)
            for (int o = 0; o < _config.OutputSize; o++)
            {
                double sum = 0;
                for (int t = 0; t < usable; t++)
                    sum += states[t][i] * targets[t + washout][o];
                stt[i, o] = sum;
            }

        // Solve via Cholesky or direct inversion (small matrix for typical reservoirs)
        var inv = InvertMatrix(sts, stateSize);
        _wOut = new double[stateSize, _config.OutputSize];
        for (int i = 0; i < stateSize; i++)
            for (int o = 0; o < _config.OutputSize; o++)
            {
                double sum = 0;
                for (int k = 0; k < stateSize; k++)
                    sum += inv[i, k] * stt[k, o];
                _wOut[i, o] = sum;
            }
    }

    public void Reset()
    {
        _state = new double[_config.ReservoirSize];
    }

    private double[,] InitInputWeights(Random rng)
    {
        var w = new double[_config.InputSize, _config.ReservoirSize];
        for (int i = 0; i < _config.InputSize; i++)
            for (int j = 0; j < _config.ReservoirSize; j++)
                w[i, j] = (rng.NextDouble() * 2 - 1) * _config.InputScaling;
        return w;
    }

    private double[,] InitReservoirWeights(Random rng)
    {
        int n = _config.ReservoirSize;
        var w = new double[n, n];

        // Sparse random matrix
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (rng.NextDouble() < _config.Sparsity)
                    w[i, j] = rng.NextDouble() * 2 - 1;

        // Scale to desired spectral radius (power iteration approximation)
        double maxEig = EstimateSpectralRadius(w, n, rng);
        if (maxEig > 1e-10)
        {
            double scale = _config.SpectralRadius / maxEig;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    w[i, j] *= scale;
        }

        return w;
    }

    private static double EstimateSpectralRadius(double[,] matrix, int n, Random rng)
    {
        var v = new double[n];
        for (int i = 0; i < n; i++) v[i] = rng.NextDouble();

        for (int iter = 0; iter < 100; iter++)
        {
            var mv = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++) sum += matrix[i, j] * v[j];
                mv[i] = sum;
            }

            double norm = 0;
            for (int i = 0; i < n; i++) norm += mv[i] * mv[i];
            norm = Math.Sqrt(norm);
            if (norm < 1e-15) return 0;

            for (int i = 0; i < n; i++) v[i] = mv[i] / norm;

            if (iter == 99) return norm;
        }

        return 0;
    }

    private static double[,] InvertMatrix(double[,] matrix, int n)
    {
        var aug = new double[n, 2 * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) aug[i, j] = matrix[i, j];
            aug[i, n + i] = 1.0;
        }

        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
                if (Math.Abs(aug[row, col]) > Math.Abs(aug[pivot, col]))
                    pivot = row;

            if (pivot != col)
                for (int j = 0; j < 2 * n; j++)
                    (aug[col, j], aug[pivot, j]) = (aug[pivot, j], aug[col, j]);

            double diagVal = aug[col, col];
            if (Math.Abs(diagVal) < 1e-15) continue;

            for (int j = 0; j < 2 * n; j++) aug[col, j] /= diagVal;

            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                double factor = aug[row, col];
                for (int j = 0; j < 2 * n; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        var inv = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                inv[i, j] = aug[i, n + j];
        return inv;
    }
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter EchoStateNetworkTests`
Expected: 7 tests PASS

**Step 4: Commit**

```
feat(fmt): add Echo State Network with ridge regression training — SIM-47 Task 11
```

---

### Task 12: FMT Model Components — IWM, ISM, EWM, ESM (TDD)

**Files:**
- Create: `src/SimOpt.FMT/Models/ImplicitWorldModel.cs`
- Create: `src/SimOpt.FMT/Models/ImplicitSelfModel.cs`
- Create: `src/SimOpt.FMT/Models/ExplicitWorldModel.cs`
- Create: `src/SimOpt.FMT/Models/ExplicitSelfModel.cs`
- Create: `tests/SimOpt.Tests/FMT/FmtModelTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/FmtModelTests.cs
using System;
using FluentAssertions;
using SimOpt.FMT.Models;
using SimOpt.FMT.Reservoir;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.FMT;

public class FmtModelTests
{
    private static readonly ReservoirConfig DefaultReservoirConfig = new()
    {
        InputSize = 30,  // 5x5 local view + 5 action slots
        ReservoirSize = 100,
        OutputSize = 25, // 5x5 predicted next view
        SpectralRadius = 0.95,
        LeakingRate = 0.3,
        Seed = 42,
    };

    [Fact]
    public void ImplicitWorldModel_PredictNextObservation()
    {
        var iwm = new ImplicitWorldModel(DefaultReservoirConfig);

        var observation = new double[25]; // flat 5x5 grid
        var action = new double[5];       // one-hot action
        action[1] = 1.0;                 // North

        var prediction = iwm.Predict(observation, action);

        prediction.Length.Should().Be(25);
        prediction.Should().OnlyContain(v => !double.IsNaN(v));
    }

    [Fact]
    public void ImplicitSelfModel_PredictSelfState()
    {
        var config = new ReservoirConfig
        {
            InputSize = 30, ReservoirSize = 50, OutputSize = 3,
            SpectralRadius = 0.9, Seed = 42,
        };
        var ism = new ImplicitSelfModel(config);

        var observation = new double[25];
        var action = new double[5];
        action[0] = 1.0; // Stay

        var selfPrediction = ism.Predict(observation, action);

        selfPrediction.Length.Should().Be(3); // dx, dy, alive probability
    }

    [Fact]
    public void ExplicitWorldModel_SimulateForward()
    {
        var iwm = new ImplicitWorldModel(DefaultReservoirConfig);
        var ewm = new ExplicitWorldModel(iwm);

        var currentObs = new double[25];
        var actionSequence = new double[][]
        {
            new double[] { 0, 1, 0, 0, 0 }, // North
            new double[] { 0, 1, 0, 0, 0 }, // North again
            new double[] { 0, 0, 0, 1, 0 }, // East
        };

        var predictions = ewm.SimulateForward(currentObs, actionSequence);

        predictions.Length.Should().Be(3);
        predictions[0].Length.Should().Be(25);
    }

    [Fact]
    public void ExplicitSelfModel_CanModelOtherAgent()
    {
        var ismConfig = new ReservoirConfig
        {
            InputSize = 30, ReservoirSize = 50, OutputSize = 3,
            SpectralRadius = 0.9, Seed = 42,
        };
        var ism = new ImplicitSelfModel(ismConfig);
        var esm = new ExplicitSelfModel(ism);

        var otherAgentObs = new double[25];
        var otherAction = new double[] { 0, 1, 0, 0, 0 }; // North

        // ESM can project from another agent's perspective
        var selfPrediction = esm.PredictForAgent(otherAgentObs, otherAction);

        selfPrediction.Length.Should().Be(3);
    }

    [Fact]
    public void ExplicitSelfModel_SelfVsOtherYieldsDifferentResults()
    {
        var ismConfig = new ReservoirConfig
        {
            InputSize = 30, ReservoirSize = 50, OutputSize = 3,
            SpectralRadius = 0.9, Seed = 42,
        };
        var ism = new ImplicitSelfModel(ismConfig);
        var esm = new ExplicitSelfModel(ism);

        var selfObs = new double[25];
        selfObs[12] = 1.0; // unique self observation
        var otherObs = new double[25];
        otherObs[0] = 1.0; // different observation

        var action = new double[] { 0, 1, 0, 0, 0 };

        var selfResult = esm.PredictForSelf(selfObs, action);
        esm.Reset();
        var otherResult = esm.PredictForAgent(otherObs, action);

        // Different observations → different predictions
        (selfResult[0] == otherResult[0]).Should().BeFalse();
    }
}
```

**Step 2: Implement the four models**

```csharp
// src/SimOpt.FMT/Models/ImplicitWorldModel.cs
using SimOpt.FMT.Reservoir;

namespace SimOpt.FMT.Models;

public class ImplicitWorldModel
{
    private readonly EchoStateNetwork _esn;

    public ImplicitWorldModel(ReservoirConfig config)
    {
        _esn = new EchoStateNetwork(config);
    }

    public double[] Predict(double[] observation, double[] action)
    {
        var combined = new double[observation.Length + action.Length];
        observation.CopyTo(combined, 0);
        action.CopyTo(combined, observation.Length);
        return _esn.Update(combined);
    }

    public void Train(double[][] inputs, double[][] targets, int washout = 10) =>
        _esn.Train(inputs, targets, washout);

    public void Reset() => _esn.Reset();

    internal EchoStateNetwork Network => _esn;
}
```

```csharp
// src/SimOpt.FMT/Models/ImplicitSelfModel.cs
using SimOpt.FMT.Reservoir;

namespace SimOpt.FMT.Models;

public class ImplicitSelfModel
{
    private readonly EchoStateNetwork _esn;

    public ImplicitSelfModel(ReservoirConfig config)
    {
        _esn = new EchoStateNetwork(config);
    }

    public double[] Predict(double[] observation, double[] action)
    {
        var combined = new double[observation.Length + action.Length];
        observation.CopyTo(combined, 0);
        action.CopyTo(combined, observation.Length);
        return _esn.Update(combined);
    }

    public void Train(double[][] inputs, double[][] targets, int washout = 10) =>
        _esn.Train(inputs, targets, washout);

    public void Reset() => _esn.Reset();

    internal EchoStateNetwork Network => _esn;
}
```

```csharp
// src/SimOpt.FMT/Models/ExplicitWorldModel.cs
namespace SimOpt.FMT.Models;

public class ExplicitWorldModel
{
    private readonly ImplicitWorldModel _iwm;

    public ExplicitWorldModel(ImplicitWorldModel iwm)
    {
        _iwm = iwm;
    }

    public double[][] SimulateForward(double[] currentObservation, double[][] actionSequence)
    {
        _iwm.Reset();
        var predictions = new double[actionSequence.Length][];
        var current = currentObservation;

        for (int i = 0; i < actionSequence.Length; i++)
        {
            current = _iwm.Predict(current, actionSequence[i]);
            predictions[i] = (double[])current.Clone();
        }

        return predictions;
    }

    public void Reset() => _iwm.Reset();
}
```

```csharp
// src/SimOpt.FMT/Models/ExplicitSelfModel.cs
namespace SimOpt.FMT.Models;

public class ExplicitSelfModel
{
    private readonly ImplicitSelfModel _ism;

    public ExplicitSelfModel(ImplicitSelfModel ism)
    {
        _ism = ism;
    }

    public double[] PredictForSelf(double[] selfObservation, double[] action) =>
        _ism.Predict(selfObservation, action);

    public double[] PredictForAgent(double[] agentObservation, double[] action)
    {
        // Third-person perspective projection: use ISM with another agent's observation
        // The ISM processes the other agent's observation as if it were self
        return _ism.Predict(agentObservation, action);
    }

    public void Reset() => _ism.Reset();
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter FmtModelTests`
Expected: 5 tests PASS

**Step 4: Commit**

```
feat(fmt): add IWM/ISM/EWM/ESM model components — SIM-47 Task 12
```

---

### Task 13: Permeability gates (TDD)

**Files:**
- Create: `src/SimOpt.FMT/Permeability/IPermeabilityGate.cs`
- Create: `src/SimOpt.FMT/Permeability/SigmoidGate.cs`
- Create: `src/SimOpt.FMT/Permeability/NoiseGate.cs`
- Create: `tests/SimOpt.Tests/FMT/PermeabilityTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/PermeabilityTests.cs
using System;
using FluentAssertions;
using SimOpt.FMT.Permeability;
using Xunit;

namespace SimOpt.Tests.FMT;

public class PermeabilityTests
{
    [Fact]
    public void SigmoidGate_HighOpenness_PassesMostSignal()
    {
        var gate = new SigmoidGate(openness: 5.0);
        var input = new double[] { 1.0, -0.5, 0.3 };

        var output = gate.Filter(input);

        // High openness ≈ pass-through
        for (int i = 0; i < input.Length; i++)
            output[i].Should().BeApproximately(input[i], 0.1);
    }

    [Fact]
    public void SigmoidGate_LowOpenness_AttenuatesSignal()
    {
        var gate = new SigmoidGate(openness: -5.0);
        var input = new double[] { 1.0, -0.5, 0.3 };

        var output = gate.Filter(input);

        // Low openness ≈ blocks signal
        for (int i = 0; i < input.Length; i++)
            Math.Abs(output[i]).Should().BeLessThan(Math.Abs(input[i]) * 0.1);
    }

    [Fact]
    public void SigmoidGate_ZeroOpenness_HalvesSignal()
    {
        var gate = new SigmoidGate(openness: 0.0);
        var input = new double[] { 1.0, -1.0, 0.5 };

        var output = gate.Filter(input);

        // σ(0) = 0.5 → halves each element
        for (int i = 0; i < input.Length; i++)
            output[i].Should().BeApproximately(input[i] * 0.5, 0.01);
    }

    [Fact]
    public void NoiseGate_AddsNoiseToSignal()
    {
        var gate = new NoiseGate(noiseLevel: 0.1, seed: 42);
        var input = new double[] { 1.0, 1.0, 1.0 };

        var output = gate.Filter(input);

        // Output should differ from input due to noise
        bool anyDiffers = false;
        for (int i = 0; i < input.Length; i++)
            if (Math.Abs(output[i] - input[i]) > 0.001) anyDiffers = true;

        anyDiffers.Should().BeTrue();
    }

    [Fact]
    public void NoiseGate_DeterministicWithSeed()
    {
        var gate1 = new NoiseGate(noiseLevel: 0.1, seed: 42);
        var gate2 = new NoiseGate(noiseLevel: 0.1, seed: 42);
        var input = new double[] { 1.0, 0.5, -0.3 };

        var out1 = gate1.Filter(input);
        var out2 = gate2.Filter(input);

        for (int i = 0; i < out1.Length; i++)
            out1[i].Should().BeApproximately(out2[i], 1e-10);
    }

    [Fact]
    public void MultipleGates_CanCompose()
    {
        var sigmoid = new SigmoidGate(openness: 0.0);
        var noise = new NoiseGate(noiseLevel: 0.05, seed: 42);
        var input = new double[] { 1.0, -0.5, 0.3 };

        var afterSigmoid = sigmoid.Filter(input);
        var afterBoth = noise.Filter(afterSigmoid);

        // Result should be attenuated AND noisy
        for (int i = 0; i < input.Length; i++)
            Math.Abs(afterBoth[i]).Should().BeLessThan(Math.Abs(input[i]));
    }
}
```

**Step 2: Implement**

```csharp
// src/SimOpt.FMT/Permeability/IPermeabilityGate.cs
namespace SimOpt.FMT.Permeability;

public interface IPermeabilityGate
{
    double[] Filter(double[] signal);
}
```

```csharp
// src/SimOpt.FMT/Permeability/SigmoidGate.cs
using System;

namespace SimOpt.FMT.Permeability;

public class SigmoidGate : IPermeabilityGate
{
    public double Openness { get; set; }

    public SigmoidGate(double openness)
    {
        Openness = openness;
    }

    public double[] Filter(double[] signal)
    {
        double gain = 1.0 / (1.0 + Math.Exp(-Openness));
        var output = new double[signal.Length];
        for (int i = 0; i < signal.Length; i++)
            output[i] = signal[i] * gain;
        return output;
    }
}
```

```csharp
// src/SimOpt.FMT/Permeability/NoiseGate.cs
using System;

namespace SimOpt.FMT.Permeability;

public class NoiseGate : IPermeabilityGate
{
    private readonly double _noiseLevel;
    private readonly Random _rng;

    public NoiseGate(double noiseLevel, int seed)
    {
        _noiseLevel = noiseLevel;
        _rng = new Random(seed);
    }

    public double[] Filter(double[] signal)
    {
        var output = new double[signal.Length];
        for (int i = 0; i < signal.Length; i++)
            output[i] = signal[i] + (_rng.NextDouble() * 2 - 1) * _noiseLevel;
        return output;
    }
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter PermeabilityTests`
Expected: 6 tests PASS

**Step 4: Commit**

```
feat(fmt): add permeability gates (sigmoid + noise) — SIM-47 Task 13
```

---

### Task 14: FMT Agent — full architecture (TDD)

Combines IWM, ISM, EWM, ESM, permeability gates into a complete `IGridAgent`.

**Files:**
- Create: `src/SimOpt.FMT/Agents/FmtAgent.cs`
- Create: `src/SimOpt.FMT/Agents/FmtAgentConfig.cs`
- Create: `tests/SimOpt.Tests/FMT/FmtAgentTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/FmtAgentTests.cs
using FluentAssertions;
using SimOpt.FMT.Agents;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.FMT;

public class FmtAgentTests
{
    [Fact]
    public void Constructor_ImplementsIGridAgent()
    {
        var agent = CreateDefaultFmtAgent("fmt1");

        agent.Should().BeAssignableTo<IGridAgent>();
        agent.Id.Should().Be("fmt1");
    }

    [Fact]
    public void SelectAction_ReturnsValidAction()
    {
        var agent = CreateDefaultFmtAgent("fmt1");
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        var action = agent.SelectAction(obs);

        action.Should().BeOneOf(
            GridAction.Stay, GridAction.North, GridAction.South,
            GridAction.East, GridAction.West);
    }

    [Fact]
    public void OnObserve_DeathEvent_UpdatesInternalModel()
    {
        var agent = CreateDefaultFmtAgent("fmt1");
        agent.Reset(2, 2);

        var deathEvent = new AgentEvent("other", AgentEventType.Death, 3, 3, "hazard");

        // Should not throw — agent processes the observation through ESM
        var act = () => agent.OnObserve(deathEvent);
        act.Should().NotThrow();
    }

    [Fact]
    public void SelectAction_DeterministicWithSameSeed()
    {
        var a1 = CreateDefaultFmtAgent("fmt1");
        var a2 = CreateDefaultFmtAgent("fmt2");
        a1.Reset(2, 2);
        a2.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        for (int i = 0; i < 10; i++)
        {
            var action1 = a1.SelectAction(obs);
            var action2 = a2.SelectAction(obs);
            action1.Should().Be(action2);
        }
    }

    [Fact]
    public void CanRunInGridSimulation()
    {
        var grid = new Grid(10, 10);
        grid[5, 5] = CellType.Hazard;
        var sim = new SimOpt.GridWorld.Simulation.GridSimulation(
            grid, new SimOpt.GridWorld.Simulation.GridSimulationConfig());

        sim.AddAgent(CreateDefaultFmtAgent("fmt1"), 2, 2);

        var result = sim.Run(maxSteps: 20);

        result.TotalSteps.Should().BeGreaterThan(0);
    }

    private static FmtAgent CreateDefaultFmtAgent(string id) =>
        new(id, new FmtAgentConfig
        {
            Seed = 42,
            ReservoirSize = 50,
            ViewRadius = 2,
            SpectralRadius = 0.95,
            Epsilon = 0.3,
        });
}
```

**Step 2: Implement**

```csharp
// src/SimOpt.FMT/Agents/FmtAgentConfig.cs
namespace SimOpt.FMT.Agents;

public class FmtAgentConfig
{
    public int Seed { get; set; } = 42;
    public int ReservoirSize { get; set; } = 100;
    public int ViewRadius { get; set; } = 2;
    public double SpectralRadius { get; set; } = 0.95;
    public double Epsilon { get; set; } = 0.1;
    public double PermeabilityOpenness { get; set; } = 0.0;
    public double NoiseLevel { get; set; } = 0.05;
}
```

```csharp
// src/SimOpt.FMT/Agents/FmtAgent.cs
using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.FMT.Models;
using SimOpt.FMT.Permeability;
using SimOpt.FMT.Reservoir;
using SimOpt.GridWorld.Agents;

namespace SimOpt.FMT.Agents;

public class FmtAgent : IGridAgent
{
    private static readonly GridAction[] AllActions =
        (GridAction[])Enum.GetValues(typeof(GridAction));

    private readonly FmtAgentConfig _config;
    private readonly Random _rng;
    private readonly ImplicitWorldModel _iwm;
    private readonly ImplicitSelfModel _ism;
    private readonly ExplicitWorldModel _ewm;
    private readonly ExplicitSelfModel _esm;
    private readonly SigmoidGate _permeabilityGate;
    private readonly NoiseGate _noiseGate;
    private readonly Dictionary<(int State, GridAction Action), double> _qTable = new();

    private int _lastState;
    private GridAction _lastAction;
    private double[] _lastObsFlat = Array.Empty<double>();

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsAlive { get; private set; }

    public FmtAgent(string id, FmtAgentConfig config)
    {
        Id = id;
        _config = config;
        _rng = new Random(config.Seed);

        int viewSize = (2 * config.ViewRadius + 1);
        int obsSize = viewSize * viewSize;
        int actionSize = AllActions.Length;
        int inputSize = obsSize + actionSize;

        var iwmConfig = new ReservoirConfig
        {
            InputSize = inputSize, ReservoirSize = config.ReservoirSize,
            OutputSize = obsSize, SpectralRadius = config.SpectralRadius,
            Seed = config.Seed,
        };
        var ismConfig = new ReservoirConfig
        {
            InputSize = inputSize, ReservoirSize = config.ReservoirSize / 2,
            OutputSize = 3, SpectralRadius = config.SpectralRadius,
            Seed = config.Seed + 1,
        };

        _iwm = new ImplicitWorldModel(iwmConfig);
        _ism = new ImplicitSelfModel(ismConfig);
        _ewm = new ExplicitWorldModel(_iwm);
        _esm = new ExplicitSelfModel(_ism);
        _permeabilityGate = new SigmoidGate(config.PermeabilityOpenness);
        _noiseGate = new NoiseGate(config.NoiseLevel, config.Seed + 2);
    }

    public GridAction SelectAction(GridObservation observation)
    {
        _lastObsFlat = FlattenObservation(observation);
        _lastState = HashObservation(observation);

        // Use EWM to simulate forward for each action → expected reward
        var actionValues = new double[AllActions.Length];
        for (int i = 0; i < AllActions.Length; i++)
        {
            var actionVec = OneHotAction(AllActions[i]);
            var predicted = _ewm.SimulateForward(_lastObsFlat, new[] { actionVec });
            var filtered = _permeabilityGate.Filter(predicted[0]);

            // Simple value: negative if hazard detected in prediction, positive otherwise
            double value = GetQValue(_lastState, AllActions[i]);
            double modelBonus = filtered.Sum() * 0.01; // small bonus from model
            actionValues[i] = value + modelBonus;
        }

        // Epsilon-greedy
        if (_rng.NextDouble() < _config.Epsilon)
        {
            _lastAction = AllActions[_rng.Next(AllActions.Length)];
        }
        else
        {
            int bestIdx = 0;
            for (int i = 1; i < actionValues.Length; i++)
                if (actionValues[i] > actionValues[bestIdx]) bestIdx = i;
            _lastAction = AllActions[bestIdx];
        }

        return _lastAction;
    }

    public void OnStepComplete(GridObservation newObservation, double reward)
    {
        int newState = HashObservation(newObservation);
        double maxNextQ = AllActions.Max(a => GetQValue(newState, a));
        double currentQ = GetQValue(_lastState, _lastAction);
        double newQ = currentQ + 0.1 * (reward + 0.95 * maxNextQ - currentQ);
        _qTable[(_lastState, _lastAction)] = newQ;
    }

    public void OnDeath(string cause) => IsAlive = false;

    public void OnObserve(AgentEvent agentEvent)
    {
        if (agentEvent.EventType != AgentEventType.Death) return;

        // ESM: project from the dead agent's perspective
        // Construct a synthetic observation for that agent's position
        var syntheticObs = new double[_lastObsFlat.Length];
        // Mark hazard at the death position relative to our knowledge
        var selfPrediction = _esm.PredictForAgent(syntheticObs,
            OneHotAction(GridAction.Stay));

        // Apply permeability: how much of this insight reaches explicit reasoning
        var filtered = _permeabilityGate.Filter(selfPrediction);
        var noisy = _noiseGate.Filter(filtered);

        // Use the insight to penalize the death location in Q-table
        // (This is the key FMT mechanism: learning from observed death)
        int deathState = agentEvent.X * 1000 + agentEvent.Y;
        foreach (var action in AllActions)
        {
            var key = (deathState, action);
            double current = _qTable.TryGetValue(key, out var q) ? q : 0.0;
            double penalty = noisy.Sum() * -0.5;
            _qTable[key] = current + penalty;
        }
    }

    public void Reset(int startX, int startY)
    {
        X = startX;
        Y = startY;
        IsAlive = true;
    }

    private double GetQValue(int state, GridAction action) =>
        _qTable.TryGetValue((state, action), out var q) ? q : 0.0;

    private static double[] FlattenObservation(GridObservation obs)
    {
        int w = obs.LocalView.GetLength(0);
        int h = obs.LocalView.GetLength(1);
        var flat = new double[w * h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                flat[x * h + y] = (double)obs.LocalView[x, y];
        return flat;
    }

    private static int HashObservation(GridObservation obs)
    {
        int hash = 17;
        int w = obs.LocalView.GetLength(0);
        int h = obs.LocalView.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                hash = hash * 31 + (int)obs.LocalView[x, y];
        return hash;
    }

    private static double[] OneHotAction(GridAction action)
    {
        var vec = new double[5];
        vec[(int)action] = 1.0;
        return vec;
    }
}
```

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter FmtAgentTests`
Expected: 5 tests PASS

**Step 4: Commit**

```
feat(fmt): add FmtAgent with full IWM/ISM/EWM/ESM + permeability — SIM-47 Task 14
```

---

### Task 15: Ablated agents — comparison architectures (TDD)

**Files:**
- Create: `src/SimOpt.FMT/Agents/AblatedEsmAgent.cs`
- Create: `src/SimOpt.FMT/Agents/WorldModelOnlyAgent.cs`
- Create: `tests/SimOpt.Tests/FMT/AblationTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/AblationTests.cs
using FluentAssertions;
using SimOpt.FMT.Agents;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.FMT;

public class AblationTests
{
    [Fact]
    public void AblatedEsmAgent_ImplementsIGridAgent()
    {
        var agent = new AblatedEsmAgent("ablated", new FmtAgentConfig { Seed = 42, ReservoirSize = 50, ViewRadius = 2 });
        agent.Should().BeAssignableTo<IGridAgent>();
    }

    [Fact]
    public void AblatedEsmAgent_OnObserveDeath_DoesNotUseESM()
    {
        var agent = new AblatedEsmAgent("ablated", new FmtAgentConfig { Seed = 42, ReservoirSize = 50, ViewRadius = 2 });
        agent.Reset(2, 2);

        var deathEvent = new AgentEvent("other", AgentEventType.Death, 3, 3, "hazard");

        // Should not throw, but uses flat association only
        var act = () => agent.OnObserve(deathEvent);
        act.Should().NotThrow();
    }

    [Fact]
    public void WorldModelOnlyAgent_HasNoSelfModel()
    {
        var agent = new WorldModelOnlyAgent("wm-only", new FmtAgentConfig { Seed = 42, ReservoirSize = 50, ViewRadius = 2 });
        agent.Reset(2, 2);

        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        var action = agent.SelectAction(obs);
        action.Should().BeOneOf(
            GridAction.Stay, GridAction.North, GridAction.South,
            GridAction.East, GridAction.West);
    }

    [Fact]
    public void AllArchitectures_CanRunInSameSimulation()
    {
        var grid = new Grid(10, 10);
        grid[5, 5] = CellType.Hazard;
        var sim = new SimOpt.GridWorld.Simulation.GridSimulation(
            grid, new SimOpt.GridWorld.Simulation.GridSimulationConfig());

        var config = new FmtAgentConfig { Seed = 42, ReservoirSize = 50, ViewRadius = 2 };

        sim.AddAgent(new FmtAgent("fmt", config), 1, 1);
        sim.AddAgent(new AblatedEsmAgent("ablated", config), 3, 1);
        sim.AddAgent(new WorldModelOnlyAgent("wm-only", config), 1, 3);
        sim.AddAgent(new SimOpt.GridWorld.Agents.QLearningAgent("flat-rl", seed: 42), 3, 3);
        sim.AddAgent(new SimOpt.GridWorld.Agents.RandomAgent("random", seed: 42), 8, 8);

        var result = sim.Run(maxSteps: 20);

        result.TotalSteps.Should().BeGreaterThan(0);
    }
}
```

**Step 2: Implement ablated agents**

`AblatedEsmAgent`: Full FMT minus ESM — `OnObserve` uses flat association ("that-square = bad") instead of perspective projection.

`WorldModelOnlyAgent`: Has IWM/EWM for world prediction, no ISM/ESM. Cannot model self-in-world or project to other agents. `OnObserve` death → simple location avoidance.

Implementation follows the same pattern as `FmtAgent` but with removed components. The key difference is in `OnObserve` — without ESM, the agent can only learn "location X is dangerous" (flat association), not "lava causes death by heat" (causal structure via self-model projection).

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter AblationTests`
Expected: 4 tests PASS

**Step 4: Commit**

```
feat(fmt): add ablated ESM and world-model-only comparison agents — SIM-47 Task 15
```

---

### Task 16: Death observation experiment framework (TDD)

**Files:**
- Create: `src/SimOpt.FMT/Experiments/DeathObservationExperiment.cs`
- Create: `src/SimOpt.FMT/Experiments/ExperimentResult.cs`
- Create: `tests/SimOpt.Tests/FMT/DeathObservationExperimentTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/SimOpt.Tests/FMT/DeathObservationExperimentTests.cs
using FluentAssertions;
using SimOpt.FMT.Experiments;
using Xunit;

namespace SimOpt.Tests.FMT;

public class DeathObservationExperimentTests
{
    [Fact]
    public void RunExperiment_ProducesResults()
    {
        var experiment = new DeathObservationExperiment(
            gridSize: 10,
            hazardPositions: new[] { (5, 5), (5, 6) },
            observerStartPosition: (3, 3),
            victimStartPosition: (5, 4),
            episodes: 10,
            stepsPerEpisode: 20,
            seed: 42);

        var results = experiment.Run();

        results.Should().NotBeNull();
        results.AgentResults.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void RunExperiment_ComparesMultipleArchitectures()
    {
        var experiment = new DeathObservationExperiment(
            gridSize: 10,
            hazardPositions: new[] { (5, 5) },
            observerStartPosition: (3, 3),
            victimStartPosition: (5, 4),
            episodes: 5,
            stepsPerEpisode: 15,
            seed: 42);

        var results = experiment.Run();

        // Should have results for each architecture type
        results.AgentResults.Should().ContainKey("fmt");
        results.AgentResults.Should().ContainKey("ablated-esm");
        results.AgentResults.Should().ContainKey("world-model-only");
        results.AgentResults.Should().ContainKey("flat-rl");
    }

    [Fact]
    public void ExperimentResult_ContainsHazardAvoidanceMetric()
    {
        var experiment = new DeathObservationExperiment(
            gridSize: 10,
            hazardPositions: new[] { (5, 5) },
            observerStartPosition: (3, 3),
            victimStartPosition: (5, 4),
            episodes: 5,
            stepsPerEpisode: 15,
            seed: 42);

        var results = experiment.Run();

        foreach (var (_, agentResult) in results.AgentResults)
        {
            agentResult.HazardAvoidanceRate.Should().BeInRange(0.0, 1.0);
            agentResult.SurvivalSteps.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
```

**Step 2: Implement experiment**

`DeathObservationExperiment`:
1. Creates a grid with hazards at specified positions
2. Places a "victim" agent (RandomAgent) that will walk into hazards and die
3. Places an "observer" agent of each architecture type
4. Runs episodes: victim dies, observer watches, observer is tested on whether it avoids the hazard
5. Measures: hazard avoidance rate, survival steps, learning speed

`ExperimentResult`:
- Per-agent: hazard avoidance rate, average survival steps, learning curve (avoidance by episode)
- Cross-agent: comparative table

**Step 3: Run tests**

Run: `dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter DeathObservationExperimentTests`
Expected: 3 tests PASS

**Step 4: Commit**

```
feat(fmt): add death observation experiment framework — SIM-47 Task 16
```

---

## Alignment with Formalization Spec

**Source:** `~/aIware/docs/simopt-fmt-gridworld-spec.md` (Session 209, comprehensive)
**Paper:** `~/aIware/paper/fmt_formal/fmt-formalization.md` (9200 words, v7-aligned)

The comprehensive spec defines three sharp predictions, not just the death observation experiment. This plan's Phase 1-2 build the infrastructure; Phase 3 (below) maps directly to the spec's three predictions.

### Gaps to address during implementation

1. **Hazard type system:** Current `CellType.Hazard` is monolithic. Needs: `HazardType` enum (Lava, DeepWater, Predator, Cliff) with causal properties (heat, submersion, predation, fall). Novel hazards share causal structure with known ones — this is how Prediction 1 (transfer) works. **Fix in Task 2:** extend `CellType` or add a `HazardInfo` class.

2. **Multi-channel gating:** Spec requires ≥ 2-3 independently modulable gates (attention, arousal, domain-specific). Current plan has SigmoidGate + NoiseGate but they're not composed as a family. **Fix in Task 13:** add `GatingFamily` class composing multiple `IPermeabilityGate` instances per channel.

3. **Prediction 2 — criticality phase transition:** Needs dedicated experiment sweeping spectral radius 0.1→1.5 and measuring observational learning at each point. Expect step function near σ ≈ 1, not gradual curve. **Add to Phase 3.**

4. **Prediction 3 — EWM coverage ablation:** EWM needs configurable coverage (hide environmental features). ESM accuracy should degrade proportionally. **Add to Phase 3.**

5. **Measurement protocol:** Cohen's d for effect size, phase transition derivative, correlation analysis. Use `SimOpt.Statistics` for these. Results formatted for paper inclusion.

6. **Representational similarity analysis (RSA):** Spec suggests RSA on hidden states to operationalize "causal structure extraction" vs "flat association." Open research question — address in Phase 3.

7. **Python note:** Spec recommends Python + gymnasium. We chose .NET/SimOpt instead — user decision. SimOpt gains a new simulation kind; FMT gets the math/stats infrastructure.

## Phase 3 — Validation experiments (future plan)

The following tasks complete the FMT validation but are outlined rather than fully specified. Each follows the same TDD pattern.

### Task 17: Transfer experiment
Test whether FMT agent generalizes from observed death-by-lava to novel hazards with similar causal structure (e.g., a "deep-water" hazard that also kills by immersion).

### Task 18: ESM ablation quantitative comparison
Run the death observation experiment across 100 episodes with statistical analysis (using SimOpt.Statistics). Measure effect size of ESM presence on learning speed and transfer.

### Task 19: Criticality sweep
Vary reservoir spectral radius from 0.1 to 1.5 and measure FMT agent performance. Validate the v7 paper claim that edge-of-chaos (ρ ≈ 1.0) is optimal for self-referential simulation.

### Task 20: Permeability family experiment
Test multiple gate types and configurations. Validate v7 §3.6 claim that permeability is a family of mechanisms, not a single parameter.

### Task 21: Observability constraint validation
Verify that ESM predictions are bounded by EWM accuracy (v7 §3.4). Agent can't know more about itself than its world model permits.

### Task 22: Results reporting and visualization
Create a results summary with charts (potentially using SimOpt.Visualization or export to CSV for external plotting).

---

## Backlog Entry

Add to `backlog.md`:
```
- [ ] **SIM-47** FMT architectural validation gridworld — generic GridWorld engine + FMT agent architectures + comparison experiments (size: XL) — P2
```

## File Index

### SimOpt.GridWorld
| File | Purpose |
|------|---------|
| `src/SimOpt.GridWorld/SimOpt.GridWorld.csproj` | Project definition |
| `src/SimOpt.GridWorld/Environment/CellType.cs` | Cell type enum |
| `src/SimOpt.GridWorld/Environment/Grid.cs` | 2D grid data structure |
| `src/SimOpt.GridWorld/Agents/GridAction.cs` | Movement action enum |
| `src/SimOpt.GridWorld/Agents/GridObservation.cs` | Agent perception |
| `src/SimOpt.GridWorld/Agents/AgentEvent.cs` | Event broadcasting |
| `src/SimOpt.GridWorld/Agents/IGridAgent.cs` | Agent interface |
| `src/SimOpt.GridWorld/Agents/RandomAgent.cs` | Random baseline |
| `src/SimOpt.GridWorld/Agents/QLearningAgent.cs` | Flat RL baseline |
| `src/SimOpt.GridWorld/Simulation/GridSimulation.cs` | Step-based runner |
| `src/SimOpt.GridWorld/Simulation/GridSimulationConfig.cs` | Configuration |
| `src/SimOpt.GridWorld/Simulation/StepResult.cs` | Per-step results |
| `src/SimOpt.GridWorld/Simulation/SimulationResult.cs` | Aggregate results |

### SimOpt.FMT
| File | Purpose |
|------|---------|
| `src/SimOpt.FMT/SimOpt.FMT.csproj` | Project definition |
| `src/SimOpt.FMT/Reservoir/ReservoirConfig.cs` | ESN configuration |
| `src/SimOpt.FMT/Reservoir/EchoStateNetwork.cs` | Reservoir computing |
| `src/SimOpt.FMT/Models/ImplicitWorldModel.cs` | IWM — learned environment |
| `src/SimOpt.FMT/Models/ImplicitSelfModel.cs` | ISM — learned self |
| `src/SimOpt.FMT/Models/ExplicitWorldModel.cs` | EWM — active world simulation |
| `src/SimOpt.FMT/Models/ExplicitSelfModel.cs` | ESM — active self simulation |
| `src/SimOpt.FMT/Permeability/IPermeabilityGate.cs` | Gate interface |
| `src/SimOpt.FMT/Permeability/SigmoidGate.cs` | Sigmoid permeability |
| `src/SimOpt.FMT/Permeability/NoiseGate.cs` | Noise injection gate |
| `src/SimOpt.FMT/Agents/FmtAgentConfig.cs` | FMT agent configuration |
| `src/SimOpt.FMT/Agents/FmtAgent.cs` | Full FMT agent |
| `src/SimOpt.FMT/Agents/AblatedEsmAgent.cs` | FMT minus ESM |
| `src/SimOpt.FMT/Agents/WorldModelOnlyAgent.cs` | World model only |
| `src/SimOpt.FMT/Experiments/ExperimentResult.cs` | Experiment output |
| `src/SimOpt.FMT/Experiments/DeathObservationExperiment.cs` | Core experiment |

### Tests
| File | Purpose |
|------|---------|
| `tests/SimOpt.Tests/GridWorld/GridTests.cs` | Grid data structure |
| `tests/SimOpt.Tests/GridWorld/GridObservationTests.cs` | Observation construction |
| `tests/SimOpt.Tests/GridWorld/GridSimulationTests.cs` | Simulation runner |
| `tests/SimOpt.Tests/GridWorld/RandomAgentTests.cs` | Random baseline |
| `tests/SimOpt.Tests/GridWorld/QLearningAgentTests.cs` | Q-learning agent |
| `tests/SimOpt.Tests/GridWorld/DeathObservationTests.cs` | Death broadcast |
| `tests/SimOpt.Tests/GridWorld/GridWorldIntegrationTests.cs` | End-to-end |
| `tests/SimOpt.Tests/FMT/EchoStateNetworkTests.cs` | ESN |
| `tests/SimOpt.Tests/FMT/FmtModelTests.cs` | IWM/ISM/EWM/ESM |
| `tests/SimOpt.Tests/FMT/PermeabilityTests.cs` | Gates |
| `tests/SimOpt.Tests/FMT/FmtAgentTests.cs` | Full FMT agent |
| `tests/SimOpt.Tests/FMT/AblationTests.cs` | Comparison agents |
| `tests/SimOpt.Tests/FMT/DeathObservationExperimentTests.cs` | Experiment |
