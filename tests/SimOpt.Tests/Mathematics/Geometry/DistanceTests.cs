using FluentAssertions;
using SimOpt.Mathematics.Geometry;
using Xunit;

namespace SimOpt.Tests.Mathematics.Geometry;

public class DistanceTests
{
    [Fact]
    public void Euclidean_KnownValues_ReturnsCorrectDistance()
    {
        double[] x = [0, 0, 0];
        double[] y = [3, 4, 0];
        x.Euclidean(y).Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Euclidean_IdenticalPoints_ReturnsZero()
    {
        double[] x = [1, 2, 3];
        x.Euclidean(x).Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void Euclidean_IsSymmetric()
    {
        double[] x = [1, 2, 3];
        double[] y = [4, 5, 6];
        x.Euclidean(y).Should().BeApproximately(y.Euclidean(x), 1e-10);
    }

    [Fact]
    public void SquareEuclidean_ReturnsSquaredDistance()
    {
        double[] x = [0, 0];
        double[] y = [3, 4];
        x.SquareEuclidean(y).Should().BeApproximately(25.0, 1e-10);
    }

    [Fact]
    public void Manhattan_KnownValues_ReturnsCorrectDistance()
    {
        double[] x = [0, 0, 0];
        double[] y = [3, 4, 5];
        x.Manhattan(y).Should().BeApproximately(12.0, 1e-10);
    }

    [Fact]
    public void Manhattan_IsSymmetric()
    {
        double[] x = [1, 2];
        double[] y = [4, 6];
        x.Manhattan(y).Should().BeApproximately(y.Manhattan(x), 1e-10);
    }

    [Fact]
    public void Modular_WrapsAround()
    {
        // Distance between 1 and 9 mod 10 should be 2 (not 8)
        Distance.Modular(1, 9, 10).Should().Be(2);
    }

    [Fact]
    public void Modular_SameValue_ReturnsZero()
    {
        Distance.Modular(5, 5, 10).Should().Be(0);
    }
}
