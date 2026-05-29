using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class QLearningAgentTests
{
    [Fact]
    public void SelectAction_InitiallyExplores()
    {
        var grid = new Grid2D(5, 5);
        var agent = new QLearningAgent<Coord2D>("q1", grid.Topology, seed: 42, epsilon: 1.0);
        agent.Reset(new Coord2D(2, 2));

        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2);

        var actions = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 100; i++)
            actions.Add(agent.SelectAction(obs));

        actions.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void SelectAction_ExploitsWhenEpsilonZero()
    {
        var grid = new Grid2D(5, 5);
        var agent = new QLearningAgent<Coord2D>("q1", grid.Topology, seed: 42, epsilon: 0.0);
        agent.Reset(new Coord2D(2, 2));

        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2);

        var first = agent.SelectAction(obs);
        for (int i = 0; i < 20; i++)
            agent.SelectAction(obs).Should().Be(first);
    }

    [Fact]
    public void LearnFromExperience_AvoidHazardAfterDeath()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;

        var agent = new QLearningAgent<Coord2D>("trained", grid.Topology, seed: 1,
            epsilon: 0.1, learningRate: 0.5, discount: 0.9);

        var startPos = new Coord2D(2, 2);
        var hazardPos = new Coord2D(2, 1);

        for (int episode = 0; episode < 200; episode++)
        {
            agent.Reset(startPos);
            var obs = GridObservation<Coord2D>.FromGrid(grid, startPos, viewRadius: 2);
            var action = agent.SelectAction(obs);

            if (action == Actions.Rect.North)
            {
                var obsAfter = GridObservation<Coord2D>.FromGrid(grid, hazardPos, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: -10.0);
                agent.OnDeath("hazard");
            }
            else
            {
                var obsAfter = GridObservation<Coord2D>.FromGrid(grid, startPos, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: 0.0);
            }
        }

        var stateHash = agent.ObservationToState(
            GridObservation<Coord2D>.FromGrid(grid, startPos, viewRadius: 2));
        agent.GetQValue(stateHash, Actions.Rect.North)
            .Should().BeLessThan(0, "North leads to hazard death");
    }

    [Fact]
    public void CanRunInGridSimulation()
    {
        var grid = new Grid2D(5, 5);
        grid[0, 0] = CellType.Hazard;
        grid[4, 4] = CellType.Resource;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        sim.AddAgent(new QLearningAgent<Coord2D>("q1", grid.Topology, seed: 42), new Coord2D(2, 2));

        var result = sim.Run(maxSteps: 50);

        result.TotalSteps.Should().BeGreaterThan(0);
    }
}
