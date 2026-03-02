using FluentAssertions;
using SimOpt.Basics.Datastructures.Geometry;
using Xunit;

namespace SimOpt.Tests.Basics.Geometry;

public class VectorTests
{
    [Fact]
    public void Length_2D_ReturnsCorrectValue()
    {
        var v = new Vector(3, 4);
        v.Length().Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Length_3D_ReturnsCorrectValue()
    {
        var v = new Vector(1, 2, 2);
        v.Length().Should().BeApproximately(3.0, 1e-10);
    }

    [Fact]
    public void DotProduct_ReturnsCorrectValue()
    {
        var v1 = new Vector(1, 2, 3);
        var v2 = new Vector(4, 5, 6);
        // 1*4 + 2*5 + 3*6 = 32
        (v1 * v2).Should().BeApproximately(32.0, 1e-10);
    }

    [Fact]
    public void CrossProduct_ReturnsCorrectValue()
    {
        var v1 = new Vector(1, 0, 0);
        var v2 = new Vector(0, 1, 0);
        var cross = v1 ^ v2;
        // i×j = k
        cross.X.Should().BeApproximately(0, 1e-10);
        cross.Y.Should().BeApproximately(0, 1e-10);
        cross.Z.Should().BeApproximately(1, 1e-10);
    }

    [Fact]
    public void Normalize_ProducesUnitVector()
    {
        var v = new Vector(3, 4, 0);
        var n = Vector.Normalize(v);
        n.Length().Should().BeApproximately(1.0, 1e-10);
        n.X.Should().BeApproximately(0.6, 1e-10);
        n.Y.Should().BeApproximately(0.8, 1e-10);
    }

    [Fact]
    public void ScalarMultiplication_WorksCorrectly()
    {
        var v = new Vector(1, 2, 3);
        var result = v * 2;
        result.X.Should().Be(2);
        result.Y.Should().Be(4);
        result.Z.Should().Be(6);
    }

    [Fact]
    public void Addition_WorksCorrectly()
    {
        var v1 = new Vector(1, 2, 3);
        var v2 = new Vector(4, 5, 6);
        var result = v1 + v2;
        result.X.Should().Be(5);
        result.Y.Should().Be(7);
        result.Z.Should().Be(9);
    }

    [Fact]
    public void Negation_WorksCorrectly()
    {
        var v = new Vector(1, 2, 3);
        var neg = -v;
        neg.X.Should().Be(-1);
        neg.Y.Should().Be(-2);
        neg.Z.Should().Be(-3);
    }

    [Fact]
    public void Rotate2D_90Degrees_CorrectResult()
    {
        var v = new Vector(1, 0);
        v.Rotate(Math.PI / 2);
        v.X.Should().BeApproximately(0, 1e-10);
        v.Y.Should().BeApproximately(1, 1e-10);
    }

    [Fact]
    public void Create_FromPoints_ProducesCorrectVector()
    {
        var from = new Point(1, 2, 3);
        var to = new Point(4, 6, 8);
        var v = Vector.Create(from, to);
        v.X.Should().Be(3);
        v.Y.Should().Be(4);
        v.Z.Should().Be(5);
    }

    [Fact]
    public void Equals_NaNZ_VsDefinedZ_ShouldNotBeEqual()
    {
        // Vector.Equals has a bug on line 245: NaN check compares this.Z with this.Z
        // instead of other.Z. This means a 2D vector (Z=NaN) wrongly equals a 3D vector.
        var v2d = new Vector(1, 2);       // Z = NaN
        var v3d = new Vector(1, 2, 5.0);  // Z = 5.0
        v2d.Equals(v3d).Should().BeFalse("2D vector (Z=NaN) should not equal 3D vector (Z=5)");
    }
}
