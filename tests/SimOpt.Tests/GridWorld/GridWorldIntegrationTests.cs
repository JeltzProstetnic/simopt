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
        var grid = new Grid2D(10, 10);
        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42), new Coord2D(5, 5));

        var result = sim.Run(maxSteps: 100);

        result.AgentsAlive.Should().Be(1);
        result.TotalSteps.Should().Be(100);
    }

    [Fact]
    public void RandomAgent_EventuallyDiesOnHazardGrid()
    {
        var grid = new Grid2D(5, 5);
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                if (x != 2 || y != 2)
                    grid[x, y] = CellType.Hazard;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42), new Coord2D(2, 2));

        var result = sim.Run(maxSteps: 1000);

        result.AgentsDead.Should().Be(1);
        result.TotalSteps.Should().BeLessThan(1000);
    }

    [Fact]
    public void TwoRandomAgents_DeathBroadcast()
    {
        var grid = new Grid2D(10, 10);
        grid[5, 5] = CellType.Hazard;
        grid[5, 4] = CellType.Hazard;
        grid[4, 5] = CellType.Hazard;
        grid[6, 5] = CellType.Hazard;
        grid[5, 6] = CellType.Hazard;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig { StopWhenAllDead = false });
        sim.AddAgent(new RandomAgent<Coord2D>("near-danger", grid.Topology, seed: 1), new Coord2D(5, 3));
        sim.AddAgent(new RandomAgent<Coord2D>("safe", grid.Topology, seed: 99), new Coord2D(0, 0));

        var result = sim.Run(maxSteps: 200);

        result.TotalSteps.Should().Be(200);
    }

    [Fact]
    public void HexGrid_RandomAgentSimulation()
    {
        var grid = new HexGrid(4);
        grid[new HexCoord(2, 0)] = CellType.Hazard;
        grid[new HexCoord(-2, 0)] = CellType.Hazard;

        var sim = new GridSimulation<HexCoord>(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent<HexCoord>("hex1", grid.Topology, seed: 42), new HexCoord(0, 0));

        var result = sim.Run(maxSteps: 100);

        result.TotalSteps.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Grid3D_RandomAgentSimulation()
    {
        var grid = new Grid3D(5, 5, 3);
        grid[2, 2, 2] = CellType.Hazard;

        var sim = new GridSimulation<Coord3D>(grid, new GridSimulationConfig());
        sim.AddAgent(new RandomAgent<Coord3D>("3d1", grid.Topology, seed: 42), new Coord3D(0, 0, 0));

        var result = sim.Run(maxSteps: 50);

        result.TotalSteps.Should().BeGreaterThan(0);
    }
}
