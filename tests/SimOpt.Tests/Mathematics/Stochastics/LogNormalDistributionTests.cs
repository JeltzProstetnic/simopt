using System;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-65 / SIM-103 — the lognormal distribution could not be used by the simulation engine at all.
/// </summary>
/// <remarks>
/// <para>
/// Two defects, both found by wiring lognormal into the topology schema, both of which made the
/// distribution unreachable from any model:
/// </para>
/// <list type="number">
/// <item><description>
/// The parameterless constructor left the internal Gaussian null and <c>Configure</c> dereferenced
/// it immediately, so <c>new LogNormalDistribution(); d.ConfigureMean(3, 1);</c> threw
/// <c>NullReferenceException</c>. The public <c>LogNormalDistribution(mean, stddev, shift)</c>
/// constructor chains to the parameterless one, so <b>it threw on every call</b> — a constructor
/// that cannot be called once.
/// </description></item>
/// <item><description>
/// <c>Initialize(seed)</c> <em>replaced</em> the internal Gaussian rather than re-seeding it, so
/// the mu and sigma set by <c>Configure</c> were discarded and the distribution silently reverted
/// to the standard lognormal. This is the worse of the two, because it produces numbers rather
/// than an exception.
/// </description></item>
/// </list>
/// <para>
/// Together they closed the only order the engine can use: <see cref="SimOpt.Simulation.Engine.Random{T}"/>
/// requires a distribution that is configured and <em>not</em> initialised, then initialises it
/// itself. Defect 1 blocks the configure, and defect 2 would have thrown the configuration away
/// afterwards. Gamma, checked at the same time, does not share either problem.
/// </para>
/// </remarks>
public class LogNormalDistributionTests
{
    private readonly ITestOutputHelper output;

    public LogNormalDistributionTests(ITestOutputHelper output) => this.output = output;

    private const int Seed = 20_260_826;
    private const int SampleSize = 200_000;

    private static double SampleMean(IDistribution<double> d)
    {
        double sum = 0d;
        for (int i = 0; i < SampleSize; i++) sum += d.Next();
        return sum / SampleSize;
    }

    [Fact]
    public void TheParameterlessConstructor_ProducesAConfigurableDistribution()
    {
        var d = new LogNormalDistribution();

        Action act = () => d.ConfigureMean(3.0, 1.0);

        act.Should().NotThrow("the engine's only usable order is construct, configure, then initialise");
        d.Configured.Should().BeTrue();
        d.Initialized.Should().BeFalse();
    }

    [Fact]
    public void TheMeanAndStddevConstructor_DoesNotThrow()
    {
        Action act = () => new LogNormalDistribution(3.0, 1.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void InitializingAfterConfiguring_KeepsTheConfiguration()
    {
        var d = new LogNormalDistribution();
        d.ConfigureMean(3.0, 1.0);
        ((IDistribution<double>)d).Initialize(Seed, false);

        double observed = SampleMean(d);

        output.WriteLine($"lognormal mean 3 stddev 1   sampled {observed:F5}");
        observed.Should().BeApproximately(3.0, 0.03,
            "a distribution that reverts to the standard lognormal after seeding samples around " +
            "exp(0.5) = 1.6487, and does so without any error");
    }

    [Fact]
    public void InitializingAfterConfiguring_KeepsLogSpaceParametersToo()
    {
        var d = new LogNormalDistribution();
        d.Configure(mu: 1.0, sigma: 0.25);
        ((IDistribution<double>)d).Initialize(Seed, false);

        // mean = exp(mu + sigma^2/2) = exp(1.03125) = 2.80466
        SampleMean(d).Should().BeApproximately(Math.Exp(1.03125), 0.02);
    }

    [Fact]
    public void TheSeededConstructorPath_StillWorks()
    {
        // The pre-existing way of getting a usable lognormal. It must not regress.
        var d = new LogNormalDistribution(Seed, antithetic: false);
        d.ConfigureMean(3.0, 1.0);

        SampleMean(d).Should().BeApproximately(3.0, 0.03);
    }
}
