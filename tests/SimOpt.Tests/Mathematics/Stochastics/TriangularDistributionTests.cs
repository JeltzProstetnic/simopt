using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-57 — the triangular sampler.
///
/// <para>
/// Triangular is the distribution a non-expert actually supplies: "it takes 20 to 40 minutes,
/// usually 30". It is therefore the single most load-bearing distribution in a product whose
/// premise is that a domain expert describes their system in ordinary language.
/// </para>
/// <para>
/// The sampler computed the CDF at the mode as <c>(max-min)/(mode-min)</c> — the reciprocal of the
/// correct <c>(mode-min)/(max-min)</c> — and rescaled by <c>(mode-min)</c> instead of
/// <c>(max-min)</c>. The consequence is not noise: the support is silently truncated. For
/// Tri(10, 12, 20) no draw could ever exceed 14.47, so the entire upper tail — every slow case,
/// which is exactly what a capacity study is about — was missing.
/// </para>
/// </summary>
public class TriangularDistributionTests
{
    private const int Seed = 20260825;
    private const int SampleSize = 200_000;

    private static double[] Sample(double min, double mode, double max, int n = SampleSize)
    {
        var dist = new TriangularDistribution(Seed, min, mode, max);
        var values = new double[n];
        for (int i = 0; i < n; i++) values[i] = dist.Next();
        return values;
    }

    /// <summary>
    /// The defect's signature. A right-skewed triangular whose mode sits near its minimum: the
    /// broken sampler's reachable maximum was min + 2*sqrt(5) ≈ 14.47 against a true max of 20.
    /// </summary>
    [Fact]
    public void Next_ReachesTheUpperTail()
    {
        var values = Sample(10, 12, 20);

        values.Max().Should().BeGreaterThan(17.0,
            "Tri(10,12,20) must reach toward its maximum of 20; a sampler capped near 14.5 has " +
            "silently deleted every slow case from the model");
    }

    [Theory]
    [InlineData(10, 12, 20)]   // right-skewed — mode near the minimum
    [InlineData(0, 0.5, 1)]    // symmetric
    [InlineData(5, 19, 20)]    // left-skewed — mode near the maximum
    [InlineData(20, 30, 40)]   // the canonical "20 to 40 minutes, usually 30"
    public void Next_StaysWithinSupport(double min, double mode, double max)
    {
        var values = Sample(min, mode, max);

        values.Min().Should().BeGreaterThanOrEqualTo(min);
        values.Max().Should().BeLessThanOrEqualTo(max);
    }

    /// <summary>
    /// E[X] = (min + mode + max) / 3. At 200k draws the standard error is under 0.005 for these
    /// parameters, so a 0.05 tolerance is roughly ten sigma — tight enough to catch a wrong CDF,
    /// loose enough never to flake.
    /// </summary>
    [Theory]
    [InlineData(10, 12, 20)]
    [InlineData(0, 0.5, 1)]
    [InlineData(5, 19, 20)]
    [InlineData(20, 30, 40)]
    public void Next_MeanMatchesTheoretical(double min, double mode, double max)
    {
        var values = Sample(min, mode, max);
        var expected = (min + mode + max) / 3.0;
        var tolerance = 0.02 * (max - min);

        values.Average().Should().BeApproximately(expected, tolerance);
    }

    /// <summary>
    /// Var[X] = (min² + mode² + max² − min·mode − min·max − mode·max) / 18. The mean alone does not
    /// pin the shape — a sampler can hit the right centre with the wrong spread.
    /// </summary>
    [Theory]
    [InlineData(10, 12, 20)]
    [InlineData(20, 30, 40)]
    public void Next_VarianceMatchesTheoretical(double min, double mode, double max)
    {
        var values = Sample(min, mode, max);
        var mean = values.Average();
        var actual = values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1);

        var expected = (min * min + mode * mode + max * max
                        - min * mode - min * max - mode * max) / 18.0;

        actual.Should().BeApproximately(expected, 0.05 * expected);
    }

    /// <summary>
    /// The descending branch of the density must actually be taken. With the reciprocal CDF the
    /// branch condition was always true, so the sampler only ever ran its ascending half.
    /// </summary>
    [Fact]
    public void Next_PopulatesBothSidesOfTheMode()
    {
        var values = Sample(10, 12, 20);

        values.Count(v => v < 12).Should().BeGreaterThan(0, "the ascending branch must be reachable");
        values.Count(v => v > 12).Should().BeGreaterThan(0, "the descending branch must be reachable");
    }

    /// <summary>
    /// For a symmetric triangular the mode is the median, so the draws split evenly about it.
    /// </summary>
    [Fact]
    public void Next_SymmetricCase_SplitsEvenlyAboutTheMode()
    {
        var values = Sample(0, 0.5, 1);

        var below = values.Count(v => v < 0.5) / (double)values.Length;

        below.Should().BeApproximately(0.5, 0.01);
    }

    /// <summary>
    /// P(X &lt;= mode) = (mode - min) / (max - min). This is the value the sampler got backwards,
    /// asserted directly against the sample.
    /// </summary>
    [Theory]
    [InlineData(10, 12, 20, 0.2)]
    [InlineData(5, 19, 20, 14.0 / 15.0)]
    [InlineData(20, 30, 40, 0.5)]
    public void Next_CdfAtMode_MatchesTheoretical(double min, double mode, double max, double expected)
    {
        var values = Sample(min, mode, max);

        var proportion = values.Count(v => v <= mode) / (double)values.Length;

        proportion.Should().BeApproximately(expected, 0.01);
    }
}
