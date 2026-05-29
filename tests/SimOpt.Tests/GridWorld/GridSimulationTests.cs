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
    private static Grid2D CreateSimpleGrid()
    {
        var grid = new Grid2D(5, 5);
        grid[4, 4] = CellType.Hazard;
        return grid;
    }

    [Fact]
    public void Tick_AdvancesStepCounter()
    {
        var grid = CreateSimpleGrid();
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Step.Should().Be(0);
        sim.Tick();
        sim.Step.Should().Be(1);
    }

    [Fact]
    public void Tick_AgentMovesNorth_PositionUpdates()
    {
        var grid = CreateSimpleGrid();
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Tick();

        agent.Object.Position.Should().Be(new Coord2D(2, 1));
    }

    [Fact]
    public void Tick_AgentMovesIntoWall_PositionUnchanged()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Wall;
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Tick();

        agent.Object.Position.Should().Be(new Coord2D(2, 2));
    }

    [Fact]
    public void Tick_AgentMovesOntoHazard_Dies()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        var result = sim.Tick();

        agent.Object.IsAlive.Should().BeFalse();
        result.Deaths.Should().ContainSingle()
            .Which.AgentId.Should().Be("a1");
    }

    [Fact]
    public void Tick_DeadAgent_SkippedInActionSelection()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Tick();
        sim.Tick();

        agent.Verify(a => a.SelectAction(It.IsAny<GridObservation<Coord2D>>()), Times.Once);
    }

    [Fact]
    public void Run_StopsAtMaxSteps()
    {
        var sim = new GridSimulation<Coord2D>(CreateSimpleGrid(), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        var result = sim.Run(maxSteps: 10);

        result.TotalSteps.Should().Be(10);
    }

    [Fact]
    public void Run_StopsWhenAllAgentsDead()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        var result = sim.Run(maxSteps: 100);

        result.TotalSteps.Should().BeLessThan(100);
    }

    [Fact]
    public void AddAgent_OutOfBounds_Throws()
    {
        var sim = new GridSimulation<Coord2D>(new Grid2D(5, 5), new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.Stay);

        var act = () => sim.AddAgent(agent.Object, new Coord2D(5, 0));
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Tick_AgentMovesOffGridEdge_PositionUnchanged()
    {
        var grid = new Grid2D(5, 5);
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = CreateMockAgent("a1", Actions.Rect.North);
        sim.AddAgent(agent.Object, new Coord2D(2, 0));

        sim.Tick();

        agent.Object.Position.Should().Be(new Coord2D(2, 0));
    }

    [Fact]
    public void Run_StopWhenAllDeadFalse_ContinuesFullDuration()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 2] = CellType.Hazard;
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig { StopWhenAllDead = false });
        var agent = CreateMockAgent("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        var result = sim.Run(maxSteps: 10);

        result.AgentsDead.Should().Be(1);
        result.TotalSteps.Should().Be(10);
    }

    [Fact]
    public void Tick_AgentDiesOnHazard_ReceivesTerminalReward()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 2] = CellType.Hazard;
        var config = new GridSimulationConfig { HazardReward = -99.0 };
        var sim = new GridSimulation<Coord2D>(grid, config);
        var agent = CreateMockAgent("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Tick();

        agent.Verify(a => a.OnStepComplete(It.IsAny<GridObservation<Coord2D>>(), -99.0), Times.Once);
        agent.Verify(a => a.OnDeath("hazard"), Times.Once);
    }

    private static Mock<IGridAgent<Coord2D>> CreateMockAgent(string id, int action) =>
        MockAgentHelper.Create2D(id, action);
}
