using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics;
using SimOpt.Mathematics.Stochastics.Distributions;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-110 step 2 (Q-VIEW-PATH): the schema's <c>normal</c> family, a Gaussian truncated at zero, so a
/// time drawn from it is never negative and the viewer's presets keep their Gaussian arrivals. A draw
/// below zero is drawn again (rejection), so the distribution is the Gaussian conditioned on X ≥ 0, its
/// mean is μ + σ·φ(α)/(1 − Φ(α)) with α = −μ/σ, and a shift is added after the truncation, as for every
/// family. The fit is checked as SIM-65 requires of a new sampler: the mean against the closed form and
/// the whole distribution by a Kolmogorov–Smirnov test.
/// </summary>
public class TruncatedGaussianDistributionTests
{
    private readonly ITestOutputHelper output;

    public TruncatedGaussianDistributionTests(ITestOutputHelper output) => this.output = output;

    private const int Seed = 20_260_826;

    private static TruncatedGaussianDistribution Make(double mu, double sigma, double shift = 0d)
    {
        var d = new TruncatedGaussianDistribution();
        d.Configure(mu, sigma, shift);
        d.Initialize(Seed);
        return d;
    }

    private static double Pdf(double z) => Math.Exp(-0.5 * z * z) / Math.Sqrt(2 * Math.PI);

    [Fact]
    public void ConfiguredThenInitialised_ItKeepsItsConfiguration_AsTheEngineUsesIt()
    {
        var d = new TruncatedGaussianDistribution();
        d.Configure(2, 0.3);

        d.Configured.Should().BeTrue();
        d.Initialized.Should().BeFalse();
        d.Initialize(Seed);
        d.Initialized.Should().BeTrue();
        Enumerable.Range(0, 1_000).Select(_ => d.Next()).Average().Should().BeApproximately(2, 0.05);
    }

    [Fact]
    public void NoDrawIsBelowZero_AndTheMeanIsTheTruncatedMean()
    {
        // μ = 1, σ = 1: a sixth of the untruncated mass is below zero
        TruncatedGaussianDistribution d = Make(1, 1);
        double alpha = -1d;
        double analytic = 1 + Pdf(alpha) / (1 - MMath.Normal(alpha));
        const int n = 200_000;
        double[] x = Enumerable.Range(0, n).Select(_ => d.Next()).ToArray();
        double mean = x.Average();
        double se = Math.Sqrt(x.Sum(v => (v - mean) * (v - mean)) / (n - 1) / n);
        output.WriteLine($"truncated mean: analytic {analytic:F5}, property {d.Mean:F5}, sampled {mean:F5} ± {3 * se:F5}");

        x.Min().Should().BeGreaterThanOrEqualTo(0);
        d.Mean.Should().BeApproximately(analytic, 1e-6);
        mean.Should().BeApproximately(analytic, 3 * se);
    }

    [Fact]
    public void TheDrawsFollowTheTruncatedDistribution_ByAKolmogorovSmirnovTest()
    {
        TruncatedGaussianDistribution d = Make(1, 1);
        const int n = 20_000;
        double[] x = Enumerable.Range(0, n).Select(_ => d.Next()).OrderBy(v => v).ToArray();
        double lowTail = MMath.Normal(-1);
        double Cdf(double v) => (MMath.Normal(v - 1) - lowTail) / (1 - lowTail);

        double dMax = 0;
        for (int i = 0; i < n; i++)
            dMax = Math.Max(dMax, Math.Max((i + 1d) / n - Cdf(x[i]), Cdf(x[i]) - (double)i / n));
        double critical = 1.63 / Math.Sqrt(n);   // 1 % level
        output.WriteLine($"KS D = {dMax:F5}, 1 % critical value {critical:F5}");

        dMax.Should().BeLessThan(critical);
    }

    [Fact]
    public void ANarrowGaussian_IsBarelyTruncated_AndAShiftIsAddedAfterTheTruncation()
    {
        // the viewer's default: a spread of 15 % of the mean leaves 3e-12 of the mass below zero
        Make(2, 0.3).Mean.Should().BeApproximately(2, 1e-9);
        TruncatedGaussianDistribution shifted = Make(1, 1, shift: 5);
        shifted.Mean.Should().BeApproximately(5 + 1 + Pdf(-1) / (1 - MMath.Normal(-1)), 1e-6);
        Enumerable.Range(0, 10_000).Select(_ => shifted.Next()).Min().Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void ASpreadThatIsNotPositive_IsRefused()
    {
        Action zero = () => new TruncatedGaussianDistribution().Configure(1, 0);

        zero.Should().Throw<ArgumentOutOfRangeException>();
    }
}
