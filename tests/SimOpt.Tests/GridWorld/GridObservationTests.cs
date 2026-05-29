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
        obs.LocalView.GetLength(0).Should().Be(5);
        obs.LocalView.GetLength(1).Should().Be(5);
    }

    [Fact]
    public void FromGrid_NearEdge_OutOfBoundsAreWall()
    {
        var grid = new Grid(5, 5);
        var obs = GridObservation.FromGrid(grid, 0, 0, viewRadius: 2);

        obs.LocalView[0, 0].Should().Be(CellType.Wall);
        obs.LocalView[2, 2].Should().Be(CellType.Empty);
    }

    [Fact]
    public void FromGrid_SeesHazard()
    {
        var grid = new Grid(5, 5);
        grid[3, 2] = CellType.Hazard;
        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2);

        obs.LocalView[3, 2].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void OtherAgents_TracksVisibleAgentPositions()
    {
        var grid = new Grid(10, 10);
        var others = new List<(string Id, int X, int Y, bool Alive)>
        {
            ("agent-b", 3, 3, true),
            ("agent-c", 8, 8, true),
        };

        var obs = GridObservation.FromGrid(grid, 2, 2, viewRadius: 2, otherAgents: others);

        obs.VisibleAgents.Should().ContainSingle()
            .Which.Id.Should().Be("agent-b");
    }
}
