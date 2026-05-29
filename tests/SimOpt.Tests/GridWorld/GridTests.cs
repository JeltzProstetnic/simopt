using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Environment.Topologies;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridTests
{
    [Fact]
    public void Grid2D_Constructor_CreatesGridWithDimensions()
    {
        var grid = new Grid2D(10, 8);

        grid.Width.Should().Be(10);
        grid.Height.Should().Be(8);
    }

    [Fact]
    public void Grid2D_NewGrid_AllCellsEmpty()
    {
        var grid = new Grid2D(5, 5);

        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                grid[x, y].Should().Be(CellType.Empty);
    }

    [Fact]
    public void Grid2D_Indexer_SetAndGet()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 3] = CellType.Hazard;

        grid[2, 3].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void Grid2D_InBounds_InsideGrid_ReturnsTrue()
    {
        var grid = new Grid2D(5, 5);

        grid.InBounds(0, 0).Should().BeTrue();
        grid.InBounds(4, 4).Should().BeTrue();
        grid.InBounds(2, 3).Should().BeTrue();
    }

    [Fact]
    public void Grid2D_InBounds_OutsideGrid_ReturnsFalse()
    {
        var grid = new Grid2D(5, 5);

        grid.InBounds(-1, 0).Should().BeFalse();
        grid.InBounds(0, -1).Should().BeFalse();
        grid.InBounds(5, 0).Should().BeFalse();
        grid.InBounds(0, 5).Should().BeFalse();
    }

    [Fact]
    public void Grid2D_Indexer_OutOfBounds_Throws()
    {
        var grid = new Grid2D(5, 5);

        var act = () => grid[5, 0];
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Grid2D_Constructor_ZeroOrNegativeDimensions_Throws()
    {
        var act = () => new Grid2D(0, 5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Grid2D_GenericIndexer_Works()
    {
        var grid = new Grid2D(5, 5);
        grid[new Coord2D(2, 3)] = CellType.Resource;

        grid[new Coord2D(2, 3)].Should().Be(CellType.Resource);
        grid[2, 3].Should().Be(CellType.Resource);
    }

    [Fact]
    public void Grid2D_Topology_IsRectangular()
    {
        var grid = new Grid2D(5, 5);
        grid.Topology.Should().BeOfType<RectangularTopology>();
        grid.Topology.ActionCount.Should().Be(5);
    }

    [Fact]
    public void Grid3D_BasicOperations()
    {
        var grid = new Grid3D(4, 4, 4);

        grid.Width.Should().Be(4);
        grid.Height.Should().Be(4);
        grid.Depth.Should().Be(4);
        grid.AllCoords.Count.Should().Be(64);

        grid[1, 2, 3] = CellType.Hazard;
        grid[1, 2, 3].Should().Be(CellType.Hazard);

        grid.InBounds(0, 0, 0).Should().BeTrue();
        grid.InBounds(4, 0, 0).Should().BeFalse();
    }

    [Fact]
    public void Grid3D_Topology_IsCubic()
    {
        var grid = new Grid3D(3, 3, 3);
        grid.Topology.Should().BeOfType<CubicTopology>();
        grid.Topology.ActionCount.Should().Be(7);
    }

    [Fact]
    public void HexGrid_BasicOperations()
    {
        var grid = new HexGrid(2);

        grid.Radius.Should().Be(2);
        grid.AllCoords.Count.Should().Be(19);
        grid.InBounds(new HexCoord(0, 0)).Should().BeTrue();
        grid.InBounds(new HexCoord(2, 0)).Should().BeTrue();
        grid.InBounds(new HexCoord(3, 0)).Should().BeFalse();

        grid[new HexCoord(1, -1)] = CellType.Resource;
        grid[new HexCoord(1, -1)].Should().Be(CellType.Resource);
    }

    [Fact]
    public void HexGrid_Topology_IsHex()
    {
        var grid = new HexGrid(3);
        grid.Topology.Should().BeOfType<HexTopology>();
        grid.Topology.ActionCount.Should().Be(7);
    }

    [Fact]
    public void Grids_Factory_CreatesCorrectTypes()
    {
        Grids.Rectangular(5, 5).Should().BeOfType<Grid2D>();
        Grids.Cubic(3, 3, 3).Should().BeOfType<Grid3D>();
        Grids.Hexagonal(2).Should().BeOfType<HexGrid>();
    }

    [Fact]
    public void GetOrDefault_OutOfBounds_ReturnsWall()
    {
        var grid = new Grid2D(5, 5);
        grid.GetOrDefault(new Coord2D(10, 10)).Should().Be(CellType.Wall);
    }
}
