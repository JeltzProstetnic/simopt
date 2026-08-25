using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-57 — declared means must match sampled means.
///
/// <para>
/// A distribution's <c>Mean</c> property is what the optimizer, the validation layer and any
/// analytic pre-check consult when they need the expected value without paying for a simulation
/// run. If it disagrees with what the sampler actually produces, every such shortcut is wrong in
/// a way no amount of simulation will reveal.
/// </para>
/// <para>
/// The uniform distributions returned <c>(max - min) / 2</c>, the half-width, rather than
/// <c>(min + max) / 2</c>, the midpoint. Identical whenever min is zero — which is exactly why it
/// survived: every casual test case starts at zero.
/// </para>
/// </summary>
public class DistributionMeanTests
{
    private const int Seed = 20260825;
    private const int SampleSize = 200_000;

    private static double SampleMean(Func<double> next, int n = SampleSize)
    {
        double total = 0;
        for (int i = 0; i < n; i++) total += next();
        return total / n;
    }

    // ---- Uniform (double) ----

    [Theory]
    [InlineData(0, 10, 5)]      // min = 0: the case where the defect is invisible
    [InlineData(10, 20, 15)]    // min != 0: the defect returns 5 instead of 15
    [InlineData(100, 400, 250)]
    public void UniformDouble_Mean_IsTheMidpoint(double min, double max, double expected)
    {
        new UniformDoubleDistribution(Seed, min, max).Mean.Should().Be(expected);
    }

    [Fact]
    public void UniformDouble_DeclaredMean_MatchesSampledMean()
    {
        var dist = new UniformDoubleDistribution(Seed, 10, 20);

        SampleMean(dist.Next).Should().BeApproximately(dist.Mean, 0.05);
    }

    /// <summary>
    /// SIM-57, found while tracing a hang in <see cref="GammaDistribution"/>. The seed-only and
    /// source-only initializers set the random source but never established <c>interval</c>, which
    /// therefore stayed at its implicit zero — so <c>Next()</c> returned <c>min + U * 0</c>, a
    /// constant, for the entire life of the instance. Nothing threw and nothing warned.
    /// <see cref="GammaDistribution"/> builds its internal uniform through exactly this path, so
    /// every seed-constructed Gamma was drawing against a degenerate source.
    /// </summary>
    [Fact]
    public void UniformDouble_ConstructedWithSeedOnly_IsNotDegenerate()
    {
        // Two-argument form selects the seed-only initializer — the degenerate path.
        var dist = new UniformDoubleDistribution(Seed, false);

        var values = new double[1_000];
        for (int i = 0; i < values.Length; i++) values[i] = dist.Next();

        values.Distinct().Should().HaveCountGreaterThan(1,
            "an unconfigured uniform must still span its documented default range, not collapse to a constant");
        values.Average().Should().BeApproximately(0.5, 0.05);
        values.Min().Should().BeGreaterThanOrEqualTo(0.0);
        values.Max().Should().BeLessThan(1.0);
    }

    [Fact]
    public void UniformDouble_ConstructedFromSourceOnly_IsNotDegenerate()
    {
        var dist = new UniformDoubleDistribution(new SimOpt.Mathematics.Stochastics.RandomSources.MersenneTwister(Seed));

        var values = new double[1_000];
        for (int i = 0; i < values.Length; i++) values[i] = dist.Next();

        values.Distinct().Should().HaveCountGreaterThan(1);
        values.Average().Should().BeApproximately(0.5, 0.05);
    }

    // ---- Uniform (integer) ----

    [Theory]
    [InlineData(0u, 10u, 5)]
    [InlineData(10u, 20u, 15)]
    [InlineData(100u, 400u, 250)]
    public void UniformInteger_Mean_IsTheMidpoint(uint min, uint max, int expected)
    {
        new UniformIntegerDistribution(Seed, min, max).Mean.Should().Be(expected);
    }

    [Theory]
    [InlineData(10u, 20u, 15.0)]
    [InlineData(100u, 400u, 250.0)]
    public void UniformInteger_DoubleMean_IsTheMidpoint(uint min, uint max, double expected)
    {
        IDistribution<double> dist = new UniformIntegerDistribution(Seed, min, max);

        dist.Mean.Should().Be(expected);
    }

    // ---- Gamma ----

    /// <summary>
    /// The Marsaglia-Tsang sampler needs <c>d = k - 1/3</c>. It was written <c>k - 1/3</c> in
    /// integer arithmetic, so the term evaluated to zero and <c>d</c> collapsed to <c>k</c>,
    /// inflating every draw by a factor of roughly <c>(k + 1/3)/k</c> — about 17% at shape 2.
    /// </summary>
    [Theory]
    [InlineData(10.0, 2.0)]
    [InlineData(50.0, 4.0)]
    [InlineData(3.0, 1.5)]
    public void Gamma_SampledMean_MatchesDeclaredMean(double mean, double shape)
    {
        var dist = new GammaDistribution(Seed, mean, shape);

        SampleMean(dist.Next).Should().BeApproximately(mean, 0.03 * mean);
    }

    /// <summary>
    /// Var[X] = k * theta^2. The mean alone would not catch a shape/scale mix-up that happens to
    /// preserve the product.
    /// </summary>
    [Fact]
    public void Gamma_SampledVariance_MatchesTheoretical()
    {
        const double mean = 10.0, shape = 2.0;
        var dist = new GammaDistribution(Seed, mean, shape);
        var theta = mean / shape;

        var values = new double[SampleSize];
        for (int i = 0; i < SampleSize; i++) values[i] = dist.Next();

        var sampleMean = values.Average();
        var variance = values.Sum(v => (v - sampleMean) * (v - sampleMean)) / (values.Length - 1);

        variance.Should().BeApproximately(shape * theta * theta, 0.06 * shape * theta * theta);
    }

    // ---- Negative exponential (guards the SIM-56 1-U reformulation against a shift in the mean) ----

    [Theory]
    [InlineData(0.5)]
    [InlineData(2.0)]
    public void NegExponential_SampledMean_IsOneOverLambda(double lambda)
    {
        var dist = new NegExponentialDistribution(Seed, lambda, antithetic: false, shift: 0);

        SampleMean(dist.Next).Should().BeApproximately(1.0 / lambda, 0.02 / lambda);
    }

    /// <summary>
    /// Erlang(k, lambda) is a sum of k exponentials, so its mean is k/lambda. Confirms the SIM-56
    /// per-factor <c>1-U</c> substitution did not shift the distribution.
    /// </summary>
    [Fact]
    public void Erlang_SampledMean_IsKOverLambda()
    {
        var dist = new ErlangDistribution(Seed, mean: 12.0, k: 3);

        SampleMean(dist.Next).Should().BeApproximately(12.0, 0.25);
    }
}
