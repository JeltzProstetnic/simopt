using FluentAssertions;
using SimOpt.Mathematics;
using Xunit;

namespace SimOpt.Tests.Mathematics.Utilities;

public class MMathTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 120)]
    [InlineData(10, 3628800)]
    public void Factorial_Int_KnownValues(int n, int expected)
    {
        MMath.Factorial(n).Should().Be(expected);
    }

    [Fact]
    public void Factorial_Double_IntegerInput_MatchesIntVersion()
    {
        MMath.Factorial(5.0).Should().BeApproximately(120.0, 1e-10);
    }

    [Theory]
    [InlineData(7, 3, 1)]    // 7 mod 3 = 1
    [InlineData(-1, 3, 2)]   // -1 mod 3 = 2 (not -1)
    [InlineData(-7, 3, 2)]   // -7 mod 3 = 2
    [InlineData(0, 5, 0)]    // 0 mod 5 = 0
    public void Mod_HandlesNegativeNumbers(int x, int m, int expected)
    {
        MMath.Mod(x, m).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(-4, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public void IsEven_ReturnsCorrectResult(int value, bool expected)
    {
        value.IsEven().Should().Be(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(-3, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public void IsOdd_ReturnsCorrectResult(int value, bool expected)
    {
        value.IsOdd().Should().Be(expected);
    }

    [Fact]
    public void Constants_AreCorrect()
    {
        MMath.HalfPi.Should().BeApproximately(Math.PI / 2, 1e-15);
        MMath.TwoPi.Should().BeApproximately(Math.PI * 2, 1e-15);
        MMath.RTD.Should().BeApproximately(180.0 / Math.PI, 1e-15);
        MMath.DTR.Should().BeApproximately(Math.PI / 180.0, 1e-15);
    }
}
