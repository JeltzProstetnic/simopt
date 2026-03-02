using FluentAssertions;
using SimOpt.Mathematics.Numerics;
using Xunit;

namespace SimOpt.Tests.Mathematics.Numerics;

public class ComplexTests
{
    [Fact]
    public void Constructor_SetsRealAndImaginary()
    {
        var c = new Complex(3.0, 4.0);
        c.Re.Should().Be(3.0);
        c.Im.Should().Be(4.0);
    }

    [Fact]
    public void Constructor_DefaultsToZero()
    {
        var c = new Complex();
        c.Re.Should().Be(0);
        c.Im.Should().Be(0);
    }

    [Fact]
    public void Magnitude_ReturnsCorrectValue()
    {
        var c = new Complex(3.0, 4.0);
        c.Magnitude.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void SquaredMagnitude_ReturnsCorrectValue()
    {
        var c = new Complex(3.0, 4.0);
        c.SquaredMagnitude.Should().BeApproximately(25.0, 1e-10);
    }

    [Fact]
    public void IsReal_TrueWhenImaginaryIsZero()
    {
        new Complex(5.0, 0).IsReal.Should().BeTrue();
        new Complex(5.0, 1.0).IsReal.Should().BeFalse();
    }

    [Fact]
    public void IsImaginary_TrueWhenRealIsZero()
    {
        new Complex(0, 5.0).IsImaginary.Should().BeTrue();
        new Complex(1.0, 5.0).IsImaginary.Should().BeFalse();
        new Complex(0, 0).IsImaginary.Should().BeFalse();
    }

    [Fact]
    public void Addition_ProducesCorrectResult()
    {
        var a = new Complex(1.0, 2.0);
        var b = new Complex(3.0, 4.0);
        var sum = a + b;
        sum.Re.Should().Be(4.0);
        sum.Im.Should().Be(6.0);
    }

    [Fact]
    public void Subtraction_ProducesCorrectResult()
    {
        var a = new Complex(5.0, 7.0);
        var b = new Complex(3.0, 2.0);
        var diff = a - b;
        diff.Re.Should().Be(2.0);
        diff.Im.Should().Be(5.0);
    }

    [Fact]
    public void Multiplication_ProducesCorrectResult()
    {
        // (1+2i)(3+4i) = 3+4i+6i+8i² = 3+10i-8 = -5+10i
        var a = new Complex(1.0, 2.0);
        var b = new Complex(3.0, 4.0);
        var product = a * b;
        product.Re.Should().BeApproximately(-5.0, 1e-10);
        product.Im.Should().BeApproximately(10.0, 1e-10);
    }

    [Fact]
    public void Division_ProducesCorrectResult()
    {
        // (4+2i)/(1+i) = (4+2i)(1-i)/(1+1) = (4-4i+2i-2i²)/2 = (6-2i)/2 = 3-i
        var a = new Complex(4.0, 2.0);
        var b = new Complex(1.0, 1.0);
        var quotient = a / b;
        quotient.Re.Should().BeApproximately(3.0, 1e-10);
        quotient.Im.Should().BeApproximately(-1.0, 1e-10);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var a = new Complex(1.0, 2.0);
        var b = new Complex(1.0, 2.0);
        var c = new Complex(3.0, 4.0);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void StaticConstants_AreCorrect()
    {
        Complex.Zero.Re.Should().Be(0);
        Complex.Zero.Im.Should().Be(0);
        Complex.One.Re.Should().Be(1);
        Complex.One.Im.Should().Be(0);
        Complex.I.Re.Should().Be(0);
        Complex.I.Im.Should().Be(1);
    }

    [Fact]
    public void Sin_RealInput_MatchesMathSin()
    {
        var c = new Complex(Math.PI / 6, 0);
        var result = Complex.Sin(c);
        result.Re.Should().BeApproximately(0.5, 1e-10);
        result.Im.Should().BeApproximately(0.0, 1e-10);
    }
}
