using System.Collections.Generic;
using System.Linq;
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
        var grid = new Grid2D(7, 7);
        var pos = new Coord2D(3, 3);
        var obs = GridObservation<Coord2D>.FromGrid(grid, pos, viewRadius: 2);

        obs.AgentPosition.Should().Be(pos);
        obs.ViewRadius.Should().Be(2);
        obs.Cells.Values.Should().AllBeEquivalentTo(CellType.Empty);
    }

    [Fact]
    public void FromGrid_NearEdge_OutOfBoundsAreWall()
    {
        var grid = new Grid2D(5, 5);
        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(0, 0), viewRadius: 2);

        obs.Cells[new Coord2D(-2, -2)].Should().Be(CellType.Wall);
        obs.Cells[new Coord2D(0, 0)].Should().Be(CellType.Empty);
    }

    [Fact]
    public void FromGrid_SeesHazard()
    {
        var grid = new Grid2D(5, 5);
        grid[3, 2] = CellType.Hazard;
        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2);

        obs.Cells[new Coord2D(3, 2)].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void OtherAgents_TracksVisibleAgentPositions()
    {
        var grid = new Grid2D(10, 10);
        var others = new List<(string Id, Coord2D Position, bool Alive)>
        {
            ("agent-b", new Coord2D(3, 3), true),
            ("agent-c", new Coord2D(8, 8), true),
        };

        var obs = GridObservation<Coord2D>.FromGrid(grid, new Coord2D(2, 2), viewRadius: 2, otherAgents: others);

        obs.VisibleAgents.Should().ContainSingle()
            .Which.Id.Should().Be("agent-b");
    }

    [Fact]
    public void HexGrid_Observation_ProducesHexNeighborhood()
    {
        var grid = new HexGrid(3);
        var obs = GridObservation<HexCoord>.FromGrid(grid, new HexCoord(0, 0), viewRadius: 1);

        obs.Cells.Should().HaveCount(7);
        obs.Cells.Keys.Should().Contain(new HexCoord(0, 0));
        obs.Cells.Keys.Should().Contain(new HexCoord(1, 0));
    }
}
