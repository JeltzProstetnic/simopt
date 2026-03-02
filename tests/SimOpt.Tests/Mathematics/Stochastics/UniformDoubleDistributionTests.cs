using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

public class UniformDoubleDistributionTests
{
    [Fact]
    public void Next_ValuesWithinBounds()
    {
        var dist = new UniformDoubleDistribution(42, 0, 10);
        for (int i = 0; i < 1000; i++)
        {
            var value = dist.Next();
            value.Should().BeGreaterThanOrEqualTo(0);
            value.Should().BeLessThanOrEqualTo(10);
        }
    }

    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var dist1 = new UniformDoubleDistribution(42, 0, 100);
        var dist2 = new UniformDoubleDistribution(42, 0, 100);

        for (int i = 0; i < 100; i++)
            dist1.Next().Should().Be(dist2.Next());
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var dist1 = new UniformDoubleDistribution(42, 0, 100);
        var dist2 = new UniformDoubleDistribution(99, 0, 100);

        // At least one of the first 10 values should differ
        var anyDifferent = false;
        for (int i = 0; i < 10; i++)
        {
            if (Math.Abs(dist1.Next() - dist2.Next()) > 1e-10)
                anyDifferent = true;
        }
        anyDifferent.Should().BeTrue();
    }

    [Fact]
    public void DrawCount_IncrementsOnNext()
    {
        var dist = new UniformDoubleDistribution(42, antithetic: false);
        dist.DrawCount.Should().Be(0);

        dist.Next();
        dist.DrawCount.Should().Be(1);

        dist.Next();
        dist.DrawCount.Should().Be(2);
    }

    [Fact]
    public void Reset_ReproducesSequence()
    {
        var dist = new UniformDoubleDistribution(42, 0, 100);
        var first = dist.Next();
        dist.Reset();
        dist.Next().Should().Be(first);
    }

    [Fact]
    public void Configure_InvalidRange_Throws()
    {
        var act = () => new UniformDoubleDistribution(42, 10, 5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Mean_IsMiddleOfRange()
    {
        var dist = new UniformDoubleDistribution(42, min: 0, max: 10);
        dist.Mean.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void NonStochasticValue_IsMiddleOfRange()
    {
        var dist = new UniformDoubleDistribution(42, min: 0, max: 10);
        dist.NonStochasticValue.Should().BeApproximately(5.0, 1e-10);
    }
}
