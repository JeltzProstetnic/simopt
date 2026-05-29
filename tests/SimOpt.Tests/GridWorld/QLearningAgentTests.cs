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

        var first = agent.SelectAction(obs);
        for (int i = 0; i < 20; i++)
            agent.SelectAction(obs).Should().Be(first);
    }

    [Fact]
    public void LearnFromExperience_AvoidHazardAfterDeath()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;

        var agent = new QLearningAgent("trained", seed: 1, epsilon: 0.1,
            learningRate: 0.5, discount: 0.9);

        for (int episode = 0; episode < 200; episode++)
        {
            agent.Reset(2, 2);
            var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
            var action = agent.SelectAction(obs);

            if (action == GridAction.North)
            {
                var obsAfter = GridObservation.FromGrid(grid, 2, 1, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: -10.0);
                agent.OnDeath("hazard");
            }
            else
            {
                var obsAfter = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);
                agent.OnStepComplete(obsAfter, reward: 0.0);
            }
        }

        var stateHash = agent.ObservationToState(
            GridObservation.FromGrid(grid, 2, 2, viewRadius: 2));
        agent.GetQValue(stateHash, GridAction.North)
            .Should().BeLessThan(0, "North leads to hazard death");
    }

    [Fact]
    public void CanRunInGridSimulation()
    {
        var grid = new Grid(5, 5);
        grid[0, 0] = CellType.Hazard;
        grid[4, 4] = CellType.Resource;

        var sim = new SimOpt.GridWorld.Simulation.GridSimulation(
            grid, new SimOpt.GridWorld.Simulation.GridSimulationConfig());
        sim.AddAgent(new QLearningAgent("q1", seed: 42), 2, 2);

        var result = sim.Run(maxSteps: 50);

        result.TotalSteps.Should().BeGreaterThan(0);
    }
}
