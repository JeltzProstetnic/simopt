using System;
using FluentAssertions;
using SimOpt.GridWorld.Environment;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class GridTests
{
    [Fact]
    public void Constructor_CreatesGridWithDimensions()
    {
        var grid = new Grid(10, 8);

        grid.Width.Should().Be(10);
        grid.Height.Should().Be(8);
    }

    [Fact]
    public void NewGrid_AllCellsEmpty()
    {
        var grid = new Grid(5, 5);

        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                grid[x, y].Should().Be(CellType.Empty);
    }

    [Fact]
    public void Indexer_SetAndGet()
    {
        var grid = new Grid(5, 5);
        grid[2, 3] = CellType.Hazard;

        grid[2, 3].Should().Be(CellType.Hazard);
    }

    [Fact]
    public void InBounds_InsideGrid_ReturnsTrue()
    {
        var grid = new Grid(5, 5);

        grid.InBounds(0, 0).Should().BeTrue();
        grid.InBounds(4, 4).Should().BeTrue();
        grid.InBounds(2, 3).Should().BeTrue();
    }

    [Fact]
    public void InBounds_OutsideGrid_ReturnsFalse()
    {
        var grid = new Grid(5, 5);

        grid.InBounds(-1, 0).Should().BeFalse();
        grid.InBounds(0, -1).Should().BeFalse();
        grid.InBounds(5, 0).Should().BeFalse();
        grid.InBounds(0, 5).Should().BeFalse();
    }

    [Fact]
    public void Indexer_OutOfBounds_Throws()
    {
        var grid = new Grid(5, 5);

        var act = () => grid[5, 0];
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ZeroOrNegativeDimensions_Throws()
    {
        var act = () => new Grid(0, 5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
