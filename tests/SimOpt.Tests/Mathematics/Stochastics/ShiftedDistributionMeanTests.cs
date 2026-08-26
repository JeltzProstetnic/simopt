using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-65 / SIM-102 — three distributions sample a shifted value and report an unshifted mean.
/// </summary>
/// <remarks>
/// <para>
/// <c>NegExponentialDistribution</c>, <c>LogNormalDistribution</c> and <c>GammaDistribution</c> all
/// take a <c>shift</c>, all add it in <c>Next()</c>, and all stored <c>mean</c> at configure time
/// without it. So a distribution that draws around 7.0 reported a mean of 2.0, and
/// <c>NonStochasticValue</c> — the value the engine substitutes when a model is run
/// deterministically — was wrong by the same amount.
/// </para>
/// <para>
/// It had never bitten because nothing in the repository set a non-zero shift. SIM-65 is what
/// changes that: <c>shift</c> becomes a schema parameter, so a user can now write it, and SIM-66's
/// analytic pre-check reads <c>Mean</c> to estimate offered load. A station whose service mean
/// reads 2.0 while it actually serves at 7.0 would be declared stable at ρ = 0.29 while queueing
/// without bound at ρ = 1.02 — the pre-check would confidently certify the model it exists to
/// catch.
/// </para>
/// <para>
/// <c>Shift</c> is also a public settable property, so the mean cannot be computed once at
/// configure time; it has to be read through.
/// </para>
/// </remarks>
public class ShiftedDistributionMeanTests
{
    private readonly ITestOutputHelper output;

    public ShiftedDistributionMeanTests(ITestOutputHelper output) => this.output = output;

    private const int Seed = 20_260_826;
    private const int SampleSize = 200_000;
    private const double Shift = 5.0;

    private static double SampleMean(IDistribution<double> d)
    {
        double sum = 0d;
        for (int i = 0; i < SampleSize; i++) sum += d.Next();
        return sum / SampleSize;
    }

    private static NegExponentialDistribution Exponential(double mean, double shift)
    {
        var d = new NegExponentialDistribution();
        d.ConfigureMean(mean, shift);
        d.Initialize(Seed);
        return d;
    }

    private static LogNormalDistribution Lognormal(double mean, double stddev, double shift)
    {
        var d = new LogNormalDistribution();
        d.ConfigureMean(mean, stddev, shift);
        // Through the interface: the concrete type also carries a four-argument Initialize that
        // makes the one-argument call ambiguous.
        ((IDistribution<double>)d).Initialize(Seed, false);
        return d;
    }

    private static GammaDistribution Gamma(double mean, double k, double shift)
    {
        var d = new GammaDistribution();
        d.ConfigureMeanK(mean, k, shift);
        ((IDistribution<double>)d).Initialize(Seed, false);
        return d;
    }

    [Fact]
    public void AShiftedExponential_ReportsTheMeanItActuallyDraws()
    {
        NegExponentialDistribution d = Exponential(2.0, Shift);
        double observed = SampleMean(d);

        output.WriteLine($"shifted exponential  reported {d.Mean,9:F5}   sampled {observed,9:F5}");
        d.Mean.Should().BeApproximately(2.0 + Shift, 1e-12);
        observed.Should().BeApproximately(d.Mean, 0.05);
    }

    [Fact]
    public void AShiftedLognormal_ReportsTheMeanItActuallyDraws()
    {
        LogNormalDistribution d = Lognormal(3.0, 1.0, Shift);
        double observed = SampleMean(d);

        output.WriteLine($"shifted lognormal    reported {d.Mean,9:F5}   sampled {observed,9:F5}");
        d.Mean.Should().BeApproximately(3.0 + Shift, 1e-12);
        observed.Should().BeApproximately(d.Mean, 0.05);
    }

    [Fact]
    public void AShiftedGamma_ReportsTheMeanItActuallyDraws()
    {
        GammaDistribution d = Gamma(4.0, 2.0, Shift);
        double observed = SampleMean(d);

        output.WriteLine($"shifted gamma        reported {d.Mean,9:F5}   sampled {observed,9:F5}");
        d.Mean.Should().BeApproximately(4.0 + Shift, 1e-12);
        observed.Should().BeApproximately(d.Mean, 0.08);
    }

    [Fact]
    public void TheDeterministicValue_CarriesTheShiftToo()
    {
        // NonStochasticValue is what a deterministic run substitutes for a draw. If it omits the
        // shift, switching a model to non-stochastic mode silently changes the system it models.
        Exponential(2.0, Shift).NonStochasticValue.Should().BeApproximately(7.0, 1e-12);
        Lognormal(3.0, 1.0, Shift).NonStochasticValue.Should().BeApproximately(8.0, 1e-12);
        Gamma(4.0, 2.0, Shift).NonStochasticValue.Should().BeApproximately(9.0, 1e-12);
    }

    [Fact]
    public void ChangingTheShiftAfterConfiguring_MovesTheReportedMean()
    {
        // Shift is a public settable property, so a mean computed once at configure time goes stale
        // the moment anyone uses it.
        NegExponentialDistribution d = Exponential(2.0, 0d);
        d.Mean.Should().BeApproximately(2.0, 1e-12);

        d.Shift = 3.0;

        d.Mean.Should().BeApproximately(5.0, 1e-12);
    }

    [Fact]
    public void AnUnshiftedDistribution_IsUnaffected()
    {
        // Every existing caller passes no shift. This is the assertion that says so.
        Exponential(2.0, 0d).Mean.Should().BeApproximately(2.0, 1e-12);
        Lognormal(3.0, 1.0, 0d).Mean.Should().BeApproximately(3.0, 1e-12);
        Gamma(4.0, 2.0, 0d).Mean.Should().BeApproximately(4.0, 1e-12);
    }
}
