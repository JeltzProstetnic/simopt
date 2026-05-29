using System.Linq;
using FluentAssertions;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Environment.Topologies;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class TopologyTests
{
    [Fact]
    public void Rectangular_Neighbors_Corner_Returns2()
    {
        var topo = new RectangularTopology(5, 5);
        var neighbors = topo.Neighbors(new Coord2D(0, 0)).ToList();

        neighbors.Should().HaveCount(2);
        neighbors.Should().Contain(new Coord2D(1, 0));
        neighbors.Should().Contain(new Coord2D(0, 1));
    }

    [Fact]
    public void Rectangular_Neighbors_Center_Returns4()
    {
        var topo = new RectangularTopology(5, 5);
        var neighbors = topo.Neighbors(new Coord2D(2, 2)).ToList();

        neighbors.Should().HaveCount(4);
    }

    [Fact]
    public void Rectangular_Neighbors_Edge_Returns3()
    {
        var topo = new RectangularTopology(5, 5);
        var neighbors = topo.Neighbors(new Coord2D(0, 2)).ToList();

        neighbors.Should().HaveCount(3);
    }

    [Fact]
    public void Rectangular_Step_Stay_ReturnsSamePosition()
    {
        var topo = new RectangularTopology(5, 5);
        topo.Step(new Coord2D(2, 2), Actions.Rect.Stay).Should().Be(new Coord2D(2, 2));
    }

    [Fact]
    public void Rectangular_Distance_Manhattan()
    {
        var topo = new RectangularTopology(10, 10);
        topo.Distance(new Coord2D(0, 0), new Coord2D(3, 4)).Should().Be(7);
    }

    [Fact]
    public void Rectangular_Symmetry_NeighborsAreBidirectional()
    {
        var topo = new RectangularTopology(5, 5);
        var grid = new Grid2D(5, 5);

        foreach (var coord in grid.AllCoords)
        {
            foreach (var neighbor in topo.Neighbors(coord))
            {
                topo.Neighbors(neighbor).Should().Contain(coord,
                    $"if {neighbor} is a neighbor of {coord}, then {coord} should be a neighbor of {neighbor}");
            }
        }
    }

    [Fact]
    public void Cubic_Neighbors_Corner_Returns3()
    {
        var topo = new CubicTopology(4, 4, 4);
        var neighbors = topo.Neighbors(new Coord3D(0, 0, 0)).ToList();

        neighbors.Should().HaveCount(3);
    }

    [Fact]
    public void Cubic_Neighbors_Center_Returns6()
    {
        var topo = new CubicTopology(5, 5, 5);
        var neighbors = topo.Neighbors(new Coord3D(2, 2, 2)).ToList();

        neighbors.Should().HaveCount(6);
    }

    [Fact]
    public void Cubic_Step_AllDirections()
    {
        var topo = new CubicTopology(5, 5, 5);
        var center = new Coord3D(2, 2, 2);

        topo.Step(center, Actions.Cubic.North).Should().Be(new Coord3D(2, 1, 2));
        topo.Step(center, Actions.Cubic.South).Should().Be(new Coord3D(2, 3, 2));
        topo.Step(center, Actions.Cubic.East).Should().Be(new Coord3D(3, 2, 2));
        topo.Step(center, Actions.Cubic.West).Should().Be(new Coord3D(1, 2, 2));
        topo.Step(center, Actions.Cubic.Up).Should().Be(new Coord3D(2, 2, 3));
        topo.Step(center, Actions.Cubic.Down).Should().Be(new Coord3D(2, 2, 1));
    }

    [Fact]
    public void Cubic_Symmetry_NeighborsAreBidirectional()
    {
        var topo = new CubicTopology(3, 3, 3);
        var grid = new Grid3D(3, 3, 3);

        foreach (var coord in grid.AllCoords)
        {
            foreach (var neighbor in topo.Neighbors(coord))
            {
                topo.Neighbors(neighbor).Should().Contain(coord);
            }
        }
    }

    [Fact]
    public void Hex_Neighbors_Center_Returns6()
    {
        var topo = new HexTopology(3);
        var neighbors = topo.Neighbors(new HexCoord(0, 0)).ToList();

        neighbors.Should().HaveCount(6);
    }

    [Fact]
    public void Hex_Neighbors_Edge_ReturnsFewerThan6()
    {
        var topo = new HexTopology(2);
        var neighbors = topo.Neighbors(new HexCoord(2, 0)).ToList();

        neighbors.Count.Should().BeLessThan(6);
        neighbors.Should().AllSatisfy(n =>
        {
            (System.Math.Abs(n.Q) <= 2 && System.Math.Abs(n.R) <= 2 && System.Math.Abs(n.S) <= 2)
                .Should().BeTrue();
        });
    }

    [Fact]
    public void Hex_Step_AllDirections()
    {
        var topo = new HexTopology(3);
        var center = new HexCoord(0, 0);

        topo.Step(center, Actions.Hex.East).Should().Be(new HexCoord(1, 0));
        topo.Step(center, Actions.Hex.NorthEast).Should().Be(new HexCoord(1, -1));
        topo.Step(center, Actions.Hex.NorthWest).Should().Be(new HexCoord(0, -1));
        topo.Step(center, Actions.Hex.West).Should().Be(new HexCoord(-1, 0));
        topo.Step(center, Actions.Hex.SouthWest).Should().Be(new HexCoord(-1, 1));
        topo.Step(center, Actions.Hex.SouthEast).Should().Be(new HexCoord(0, 1));
    }

    [Fact]
    public void Hex_Symmetry_NeighborsAreBidirectional()
    {
        var topo = new HexTopology(3);
        var grid = new HexGrid(3);

        foreach (var coord in grid.AllCoords)
        {
            foreach (var neighbor in topo.Neighbors(coord))
            {
                topo.Neighbors(neighbor).Should().Contain(coord,
                    $"if {neighbor} is a neighbor of {coord}, then {coord} should be a neighbor of {neighbor}");
            }
        }
    }

    [Fact]
    public void HexCoord_DistanceTo_KnownPairs()
    {
        new HexCoord(0, 0).DistanceTo(new HexCoord(0, 0)).Should().Be(0);
        new HexCoord(0, 0).DistanceTo(new HexCoord(1, 0)).Should().Be(1);
        new HexCoord(0, 0).DistanceTo(new HexCoord(2, -1)).Should().Be(2);
        new HexCoord(0, 0).DistanceTo(new HexCoord(3, 0)).Should().Be(3);
        new HexCoord(1, -1).DistanceTo(new HexCoord(-1, 1)).Should().Be(2);
    }

    [Fact]
    public void HexCoord_S_Property()
    {
        var c = new HexCoord(2, -1);
        c.S.Should().Be(-1);
        (c.Q + c.R + c.S).Should().Be(0);
    }

    [Fact]
    public void Coord2D_Arithmetic()
    {
        var a = new Coord2D(3, 4);
        var b = new Coord2D(1, 2);

        (a + b).Should().Be(new Coord2D(4, 6));
        (a - b).Should().Be(new Coord2D(2, 2));
    }

    [Fact]
    public void Coord3D_Arithmetic()
    {
        var a = new Coord3D(3, 4, 5);
        var b = new Coord3D(1, 2, 3);

        (a + b).Should().Be(new Coord3D(4, 6, 8));
        (a - b).Should().Be(new Coord3D(2, 2, 2));
    }

    [Fact]
    public void Hex_Neighborhood_Radius1_Returns7()
    {
        var topo = new HexTopology(5);
        topo.Neighborhood(new HexCoord(0, 0), 1).Count().Should().Be(7);
    }

    [Fact]
    public void Hex_Neighborhood_Radius2_Returns19()
    {
        var topo = new HexTopology(5);
        topo.Neighborhood(new HexCoord(0, 0), 2).Count().Should().Be(19);
    }

    [Fact]
    public void Rectangular_ActionName_AllNamed()
    {
        var topo = new RectangularTopology(5, 5);
        for (int i = 0; i < topo.ActionCount; i++)
            topo.ActionName(i).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Rectangular_InvalidActionId_Throws()
    {
        var topo = new RectangularTopology(5, 5);
        var act = () => topo.Step(new Coord2D(2, 2), 99);
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Hex_InvalidActionId_Throws()
    {
        var topo = new HexTopology(3);
        var act = () => topo.Step(new HexCoord(0, 0), -1);
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Cubic_InvalidActionId_Throws()
    {
        var topo = new CubicTopology(3, 3, 3);
        var act = () => topo.ActionName(7);
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }
}
