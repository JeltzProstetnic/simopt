using FluentAssertions;
using SimOpt.Basics.Datastructures.Geometry;
using Xunit;

namespace SimOpt.Tests.Basics.Geometry;

public class PointTests
{
    [Fact]
    public void Constructor2D_SetsXYAndNaNZ()
    {
        var p = new Point(3.0, 4.0);
        p.X.Should().Be(3.0);
        p.Y.Should().Be(4.0);
        double.IsNaN(p.Z).Should().BeTrue();
    }

    [Fact]
    public void Constructor3D_SetsAllCoordinates()
    {
        var p = new Point(1.0, 2.0, 3.0);
        p.X.Should().Be(1.0);
        p.Y.Should().Be(2.0);
        p.Z.Should().Be(3.0);
    }

    [Fact]
    public void EuclideanDistance2D_ReturnsCorrectValue()
    {
        var p1 = new Point(0, 0);
        var p2 = new Point(3, 4);
        p1.GetEuclideanDistance(p2).Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void EuclideanDistance3D_ReturnsCorrectValue()
    {
        var p1 = new Point(0, 0, 0);
        var p2 = new Point(1, 2, 2);
        p1.GetEuclideanDistance(p2).Should().BeApproximately(3.0, 1e-10);
    }

    [Fact]
    public void EuclideanDistance_MismatchedDimensions_Throws()
    {
        var p2d = new Point(0, 0);
        var p3d = new Point(0, 0, 0);
        var act = () => p2d.GetEuclideanDistance(p3d);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Addition_WorksCorrectly()
    {
        var p1 = new Point(1, 2, 3);
        var p2 = new Point(4, 5, 6);
        var result = p1 + p2;
        result.X.Should().Be(5);
        result.Y.Should().Be(7);
        result.Z.Should().Be(9);
    }

    [Fact]
    public void Subtraction_ProducesVector()
    {
        var p1 = new Point(4, 6, 8);
        var p2 = new Point(1, 2, 3);
        var result = p1 - p2;
        result.X.Should().Be(3);
        result.Y.Should().Be(4);
        result.Z.Should().Be(5);
    }

    [Fact]
    public void Equality_2DPoints_ComparesCorrectly()
    {
        var p1 = new Point(1.0, 2.0);
        var p2 = new Point(1.0, 2.0);
        (p1 == p2).Should().BeTrue();
    }

    [Fact]
    public void Equality_3DPoints_ComparesCorrectly()
    {
        var p1 = new Point(1, 2, 3);
        var p2 = new Point(1, 2, 4);
        (p1 == p2).Should().BeFalse();
    }

    [Fact]
    public void ToString_2D_OmitsZ()
    {
        var p = new Point(1, 2);
        p.ToString().Should().Be("(1,2)");
    }
}
