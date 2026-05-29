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

        agent.Object.Y.Should().Be(1);
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
        agent.Object.Y.Should().Be(2);
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

        sim.Tick();
        sim.Tick();

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
