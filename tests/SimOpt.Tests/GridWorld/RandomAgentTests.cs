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
        var grid = new Grid2D(5, 5);
        var agent = new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42);
        agent.Id.Should().Be("r1");
    }

    [Fact]
    public void Reset_SetsPositionAndAlive()
    {
        var grid = new Grid2D(5, 5);
        var agent = new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42);
        agent.Reset(new Coord2D(3, 4));

        agent.Position.Should().Be(new Coord2D(3, 4));
        agent.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void SelectAction_ReturnsDifferentActions_WithSeed()
    {
        var grid = new Grid2D(5, 5);
        var agent = new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42);
        agent.Reset(new Coord2D(2, 2));

        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2);

        var actions = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 100; i++)
            actions.Add(agent.SelectAction(obs));

        actions.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void OnDeath_SetsAliveToFalse()
    {
        var grid = new Grid2D(5, 5);
        var agent = new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42);
        agent.Reset(new Coord2D(2, 2));

        agent.OnDeath("hazard");

        agent.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void DeterministicWithSameSeed()
    {
        var grid = new Grid2D(5, 5);
        var a1 = new RandomAgent<Coord2D>("r1", grid.Topology, seed: 42);
        var a2 = new RandomAgent<Coord2D>("r2", grid.Topology, seed: 42);
        a1.Reset(new Coord2D(2, 2));
        a2.Reset(new Coord2D(2, 2));

        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2);

        for (int i = 0; i < 20; i++)
            a1.SelectAction(obs).Should().Be(a2.SelectAction(obs));
    }

    [Fact]
    public void HexAgent_WorksWithHexTopology()
    {
        var grid = new HexGrid(3);
        var agent = new RandomAgent<HexCoord>("hex1", grid.Topology, seed: 42);
        agent.Reset(new HexCoord(0, 0));

        var obs = GridObservation<HexCoord>.FromGrid(grid, new HexCoord(0, 0), viewRadius: 1);
        var action = agent.SelectAction(obs);

        action.Should().BeInRange(0, 6);
    }
}
