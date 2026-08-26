using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.McpServer.Models;
using SimOpt.McpServer.Simulation;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 — the schema's distribution object, which is what lets a topology say anything other
/// than "exponential".
/// </summary>
/// <remarks>
/// <para>
/// Until this existed the MCP schema offered exactly one shape for every duration in a model. A
/// setup time of "20 to 40 minutes" — the phrasing the product's own walkthrough uses — had to be
/// entered as an exponential with a mean of 30, which puts 28% of its mass below 10 minutes and an
/// unbounded tail above 40. That is not a rounding error in the answer; it is a different queueing
/// system.
/// </para>
/// <para>
/// Two things are asserted of every type: that the factory hands back a distribution which is
/// <em>configured but not initialised</em> — <see cref="SimOpt.Simulation.Engine.Random{T}"/>
/// rejects both a bare and an already-initialised distribution, and getting this wrong is exactly
/// the SIM-89 defect that made every <c>create_model</c> call throw — and that what it samples has
/// the mean its parameters imply. Shape is checked separately and harder, in
/// <c>GoodnessOfFitTests</c>, because a mean cannot see a wrong shape.
/// </para>
/// </remarks>
public class DistributionFactoryTests
{
    private const int Seed = 20_260_826;
    private const int SampleSize = 40_000;

    private static double SampleMean(IDistribution<double> dist, int n = SampleSize)
    {
        dist.Initialize(Seed);
        double sum = 0d;
        for (int i = 0; i < n; i++) sum += dist.Next();
        return sum / n;
    }

    private static IDistribution<double> Build(DistributionSpec spec)
        => DistributionFactory.Create(spec, "node 'x', parameter 'service'");

    // ── every type builds, in the state the engine requires ──────────────────

    public static IEnumerable<object[]> EveryType() => new[]
    {
        new object[] { new DistributionSpec { Type = "exponential", Mean = 2.5 }, 2.5 },
        new object[] { new DistributionSpec { Type = "triangular", Min = 1, Mode = 2, Max = 6 }, 3.0 },
        new object[] { new DistributionSpec { Type = "uniform", Min = 2, Max = 4 }, 3.0 },
        new object[] { new DistributionSpec { Type = "lognormal", Mean = 5, Stddev = 2 }, 5.0 },
        new object[] { new DistributionSpec { Type = "gamma", Mean = 4, K = 2 }, 4.0 },
        new object[] { new DistributionSpec { Type = "constant", Value = 7 }, 7.0 },
        new object[]
        {
            // Grid points 0, 5, 10 carrying 0.25 / 0.5 / 0.25 ⇒ mean 5.
            new DistributionSpec { Type = "empirical", Min = 0, Max = 10, Probabilities = new List<double> { 0.25, 0.5, 0.25 } },
            5.0,
        },
    };

    [Theory]
    [MemberData(nameof(EveryType))]
    public void EveryDeclaredType_BuildsConfiguredAndUninitialised(DistributionSpec spec, double _)
    {
        IDistribution<double> dist = Build(spec);

        dist.Configured.Should().BeTrue($"'{spec.Type}' must be usable by Random<T>, which refuses an unconfigured distribution");
        dist.Initialized.Should().BeFalse($"'{spec.Type}' must be seeded by the engine, which refuses an already-initialised distribution (SIM-89)");
    }

    [Theory]
    [MemberData(nameof(EveryType))]
    public void EveryDeclaredType_SamplesTheMeanItsParametersImply(DistributionSpec spec, double expectedMean)
    {
        double observed = SampleMean(Build(spec));

        // 2% of the mean. Loose enough that no seed makes it flaky, tight enough that a
        // parameter fed to the wrong slot — the failure mode of a factory that maps names to
        // positional Configure arguments — cannot survive it.
        observed.Should().BeApproximately(expectedMean, 0.02 * expectedMean,
            $"'{spec.Type}' sampled {observed:F4} against an implied mean of {expectedMean:F4}");
    }

    [Theory]
    [MemberData(nameof(EveryType))]
    public void EveryDeclaredType_ReportsItsOwnMean(DistributionSpec spec, double expectedMean)
    {
        // The engine's own Mean property must agree with the sample, or a caller reading it — the
        // analytic pre-check in SIM-66 will — is told something the simulation does not do.
        Build(spec).Mean.Should().BeApproximately(expectedMean, 1e-9);
    }

    // ── alternative parameterisations ────────────────────────────────────────

    [Fact]
    public void Exponential_AcceptsARateAsWellAsAMean()
    {
        SampleMean(Build(new DistributionSpec { Type = "exponential", Rate = 0.4 }))
            .Should().BeApproximately(2.5, 0.05);
    }

    [Fact]
    public void Exponential_RefusesBothAMeanAndARate()
    {
        // Both given is not a harmless redundancy: they disagree in every case except mean = 1/rate,
        // and silently preferring one of them means the model the user reads back is not the model
        // that ran.
        Action act = () => Build(new DistributionSpec { Type = "exponential", Mean = 2.5, Rate = 0.4 });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mean*rate*");
    }

    [Fact]
    public void Gamma_AcceptsShapeAndScaleAsWellAsMeanAndShape()
    {
        // k = 2, theta = 3 ⇒ mean 6.
        SampleMean(Build(new DistributionSpec { Type = "gamma", K = 2, Theta = 3 }))
            .Should().BeApproximately(6.0, 0.15);
    }

    [Fact]
    public void Lognormal_AcceptsLogSpaceParametersAsWellAsMoments()
    {
        // mu = 0, sigma = 0.5 ⇒ mean = exp(0.125) = 1.13315.
        SampleMean(Build(new DistributionSpec { Type = "lognormal", Mu = 0, Sigma = 0.5 }))
            .Should().BeApproximately(Math.Exp(0.125), 0.02);
    }

    // ── refusals, each naming the fix (UN-008) ───────────────────────────────

    [Fact]
    public void AnUnknownType_IsRefusedWithTheListOfTypesThatExist()
    {
        Action act = () => Build(new DistributionSpec { Type = "poisson" });

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("poisson").And.Contain("triangular",
                "an LLM correcting itself needs the vocabulary, not just a rejection");
    }

    [Fact]
    public void AMissingType_IsRefusedRatherThanDefaultingToExponential()
    {
        // Defaulting is the dangerous option: the model runs, produces plausible numbers, and is
        // not the model the user described.
        Action act = () => Build(new DistributionSpec { Mean = 3 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*type*");
    }

    [Fact]
    public void AMissingParameter_IsRefusedWithTheParameterAndTheNodeNamed()
    {
        Action act = () => Build(new DistributionSpec { Type = "triangular", Min = 1, Max = 6 });

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("mode").And.Contain("node 'x'");
    }

    [Theory]
    [InlineData("triangular", "*mode*")]   // min > mode
    [InlineData("uniform", "*min*max*")]   // min > max
    public void AnImpossibleParameterCombination_IsRefused(string type, string expected)
    {
        var spec = type == "triangular"
            ? new DistributionSpec { Type = "triangular", Min = 5, Mode = 2, Max = 6 }
            : new DistributionSpec { Type = "uniform", Min = 9, Max = 1 };

        Action act = () => Build(spec);

        act.Should().Throw<InvalidOperationException>().WithMessage(expected);
    }

    [Fact]
    public void ANegativeMean_IsRefused()
    {
        // Every distribution in the schema models a duration or an interval. A negative one is
        // meaningless and, left alone, schedules an event in the past.
        Action act = () => Build(new DistributionSpec { Type = "exponential", Mean = -1 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*positive*");
    }

    [Fact]
    public void EmpiricalProbabilitiesThatDoNotSumToOne_AreRefusedWithTheSumStated()
    {
        Action act = () => Build(new DistributionSpec
        {
            Type = "empirical", Min = 0, Max = 10,
            Probabilities = new List<double> { 0.25, 0.5, 0.1 },
        });

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("0.85", "the caller cannot fix a sum they are not told");
    }

    // ── the shift, shared by every continuous type ───────────────────────────

    [Fact]
    public void AShiftMovesTheWholeDistribution()
    {
        // A shifted exponential is how a "minimum handling time plus a random remainder" is
        // expressed without inventing a node for it.
        double plain = SampleMean(Build(new DistributionSpec { Type = "exponential", Mean = 2.0 }));
        double shifted = SampleMean(Build(new DistributionSpec { Type = "exponential", Mean = 2.0, Shift = 5.0 }));

        (shifted - plain).Should().BeApproximately(5.0, 0.1);
    }
}
