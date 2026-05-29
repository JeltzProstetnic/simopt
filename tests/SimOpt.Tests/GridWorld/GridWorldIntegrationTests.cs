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
    public void TwoRandomAgents_DeathBroadcast()
    {
        var grid = new Grid(10, 10);
        grid[5, 5] = CellType.Hazard;
        grid[5, 4] = CellType.Hazard;
        grid[4, 5] = CellType.Hazard;
        grid[6, 5] = CellType.Hazard;
        grid[5, 6] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig { StopWhenAllDead = false });
        sim.AddAgent(new RandomAgent("near-danger", seed: 1), 5, 3);
        sim.AddAgent(new RandomAgent("safe", seed: 99), 0, 0);

        var result = sim.Run(maxSteps: 200);

        result.TotalSteps.Should().Be(200);
    }
}
