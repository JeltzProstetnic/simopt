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
