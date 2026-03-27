using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using Xunit;

namespace SimOpt.Tests.Mathematics.Distributions;

// ─────────────────────────────────────────────────────────────────────────────
// GaussianDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class GaussianDistributionTests
{
    // Constructor / properties

    [Fact]
    public void GaussianDistribution_Constructor_SetsPropertiesCorrectly()
    {
        var dist = new GaussianDistribution(42, mu: 5.0, sigma: 2.0);

        dist.Mean.Should().Be(5.0);
        dist.Sigma.Should().Be(2.0);
        dist.NonStochasticValue.Should().Be(5.0);
        dist.Name.Should().Be("Gaussian Distribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void GaussianDistribution_DefaultParameters_UsesZeroMeanAndUnitSigma()
    {
        var dist = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);

        dist.Mean.Should().Be(0.0);
        dist.Sigma.Should().Be(1.0);
    }

    [Fact]
    public void GaussianDistribution_ConfigureOnly_IsConfiguredNotInitialized()
    {
        var dist = new GaussianDistribution(mu: 3.0, sigma: 1.5);

        dist.Configured.Should().BeTrue();
        dist.Initialized.Should().BeFalse();
        dist.Mean.Should().Be(3.0);
        dist.Sigma.Should().Be(1.5);
    }

    // DrawCount

    [Fact]
    public void GaussianDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
        dist.Next();
        dist.DrawCount.Should().Be(2);
    }

    // Seed reproducibility

    [Fact]
    public void GaussianDistribution_SameSeed_ProducesSameSequence()
    {
        var dist1 = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);
        var dist2 = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    [Fact]
    public void GaussianDistribution_DifferentSeeds_ProduceDifferentSequences()
    {
        var dist1 = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);
        var dist2 = new GaussianDistribution(99, mu: 0.0, sigma: 1.0);

        bool anyDifferent = false;
        for (int i = 0; i < 20; i++)
        {
            if (Math.Abs(dist1.Next() - dist2.Next()) > 1e-10)
                anyDifferent = true;
        }

        anyDifferent.Should().BeTrue();
    }

    // Reset

    [Fact]
    public void GaussianDistribution_Reset_ReproducesSequenceAndResetsDrawCount()
    {
        var dist = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);
        double first = dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
        dist.Next().Should().Be(first);
    }

    [Fact]
    public void GaussianDistribution_ResetWithSeed_UsesNewSeed()
    {
        var dist1 = new GaussianDistribution(42, mu: 0.0, sigma: 1.0);
        var dist2 = new GaussianDistribution(99, mu: 0.0, sigma: 1.0);

        dist1.Reset(99);

        for (int i = 0; i < 20; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Statistical properties — mean and variance over 10 000 samples

    [Fact]
    public void GaussianDistribution_Next_SampleMeanApproximatesConfiguredMean()
    {
        const int n = 10_000;
        const double mu = 5.0;
        const double tolerance = 0.10; // 10%

        var dist = new GaussianDistribution(42, mu: mu, sigma: 1.0);
        double sum = 0;
        for (int i = 0; i < n; i++) sum += dist.Next();
        double sampleMean = sum / n;

        sampleMean.Should().BeApproximately(mu, mu * tolerance);
    }

    [Fact]
    public void GaussianDistribution_Next_SampleVarianceApproximatesSigmaSquared()
    {
        const int n = 10_000;
        const double mu = 0.0;
        const double sigma = 3.0;
        const double tolerance = 0.15; // 15% on variance

        var dist = new GaussianDistribution(42, mu: mu, sigma: sigma);
        var samples = new double[n];
        for (int i = 0; i < n; i++) samples[i] = dist.Next();

        double mean = samples.Average();
        double variance = samples.Select(x => (x - mean) * (x - mean)).Average();

        variance.Should().BeApproximately(sigma * sigma, sigma * sigma * tolerance);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// NegExponentialDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class NegExponentialDistributionTests
{
    // Constructor / properties

    [Fact]
    public void NegExponentialDistribution_Constructor_SetsPropertiesCorrectly()
    {
        var dist = new NegExponentialDistribution(42, lambda: 2.0, antithetic: false, shift: 0.0);

        dist.Lambda.Should().Be(2.0);
        dist.Mean.Should().BeApproximately(0.5, 1e-10);
        dist.NonStochasticValue.Should().BeApproximately(0.5, 1e-10);
        dist.Name.Should().Be("Negative Exponential Distribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void NegExponentialDistribution_ConfigureMean_SetsLambdaInversely()
    {
        var dist = new NegExponentialDistribution(42, antithetic: false);
        dist.ConfigureMean(4.0);

        dist.Mean.Should().BeApproximately(4.0, 1e-10);
        dist.Lambda.Should().BeApproximately(0.25, 1e-10);
    }

    [Fact]
    public void NegExponentialDistribution_Configure_ZeroLambda_Throws()
    {
        var dist = new NegExponentialDistribution(42, antithetic: false);
        var act = () => dist.Configure(0.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NegExponentialDistribution_Configure_NegativeLambda_Throws()
    {
        var dist = new NegExponentialDistribution(42, antithetic: false);
        var act = () => dist.Configure(-1.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NegExponentialDistribution_ConfigureMean_ZeroMean_Throws()
    {
        var dist = new NegExponentialDistribution(42, antithetic: false);
        var act = () => dist.ConfigureMean(0.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // DrawCount

    [Fact]
    public void NegExponentialDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new NegExponentialDistribution(42, lambda: 1.0, antithetic: false, shift: 0.0);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
    }

    // Non-negative values

    [Fact]
    public void NegExponentialDistribution_Next_ValuesAreNonNegative()
    {
        var dist = new NegExponentialDistribution(42, lambda: 1.0, antithetic: false, shift: 0.0);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThanOrEqualTo(0.0);
        }
    }

    [Fact]
    public void NegExponentialDistribution_Next_ShiftedValuesAreGreaterThanOrEqualToShift()
    {
        const double shift = 5.0;
        var dist = new NegExponentialDistribution(42, lambda: 1.0, antithetic: false, shift: shift);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThanOrEqualTo(shift);
        }
    }

    // Seed reproducibility

    [Fact]
    public void NegExponentialDistribution_SameSeed_ProducesSameSequence()
    {
        var dist1 = new NegExponentialDistribution(42, lambda: 2.0, antithetic: false, shift: 0.0);
        var dist2 = new NegExponentialDistribution(42, lambda: 2.0, antithetic: false, shift: 0.0);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Reset

    [Fact]
    public void NegExponentialDistribution_Reset_ReproducesSequenceAndResetsDrawCount()
    {
        var dist = new NegExponentialDistribution(42, lambda: 1.0, antithetic: false, shift: 0.0);
        double first = dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
        dist.Next().Should().Be(first);
    }

    // Statistical properties

    [Fact]
    public void NegExponentialDistribution_Next_SampleMeanApproximatesExpectedMean()
    {
        const int n = 10_000;
        const double lambda = 2.0;
        const double expectedMean = 1.0 / lambda; // 0.5
        const double tolerance = 0.10;

        var dist = new NegExponentialDistribution(42, lambda: lambda, antithetic: false, shift: 0.0);
        double sum = 0;
        for (int i = 0; i < n; i++) sum += dist.Next();

        (sum / n).Should().BeApproximately(expectedMean, expectedMean * tolerance);
    }

    [Fact]
    public void NegExponentialDistribution_Next_SampleVarianceApproximatesExpected()
    {
        const int n = 10_000;
        const double lambda = 2.0;
        const double expectedVariance = 1.0 / (lambda * lambda); // 0.25
        const double tolerance = 0.15;

        var dist = new NegExponentialDistribution(42, lambda: lambda, antithetic: false, shift: 0.0);
        var samples = new double[n];
        for (int i = 0; i < n; i++) samples[i] = dist.Next();

        double mean = samples.Average();
        double variance = samples.Select(x => (x - mean) * (x - mean)).Average();

        variance.Should().BeApproximately(expectedVariance, expectedVariance * tolerance);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ConstantDoubleDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class ConstantDoubleDistributionTests
{
    [Fact]
    public void ConstantDoubleDistribution_Constructor_SetsPropertiesCorrectly()
    {
        var dist = new ConstantDoubleDistribution(7.5);

        dist.Mean.Should().Be(7.5);
        dist.NonStochasticValue.Should().Be(7.5);
        dist.Name.Should().Be("Constant");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void ConstantDoubleDistribution_DefaultValue_IsZero()
    {
        var dist = new ConstantDoubleDistribution();
        dist.Configure(0.0);

        dist.Next().Should().Be(0.0);
    }

    [Fact]
    public void ConstantDoubleDistribution_Next_AlwaysReturnsConfiguredValue()
    {
        const double constant = 42.5;
        var dist = new ConstantDoubleDistribution(constant);

        for (int i = 0; i < 100; i++)
        {
            dist.Next().Should().Be(constant);
        }
    }

    [Fact]
    public void ConstantDoubleDistribution_Next_NegativeValue_AlwaysReturnsThatValue()
    {
        const double constant = -3.14;
        var dist = new ConstantDoubleDistribution(constant);

        for (int i = 0; i < 10; i++)
        {
            dist.Next().Should().Be(constant);
        }
    }

    [Fact]
    public void ConstantDoubleDistribution_Next_ZeroValue_AlwaysReturnsZero()
    {
        var dist = new ConstantDoubleDistribution(0.0);

        for (int i = 0; i < 10; i++)
        {
            dist.Next().Should().Be(0.0);
        }
    }

    [Fact]
    public void ConstantDoubleDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new ConstantDoubleDistribution(1.0);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
        dist.Next();
        dist.DrawCount.Should().Be(2);
    }

    [Fact]
    public void ConstantDoubleDistribution_Reset_ResetsDrawCount()
    {
        var dist = new ConstantDoubleDistribution(5.0);
        dist.Next();
        dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void ConstantDoubleDistribution_Reset_NextStillReturnsConstant()
    {
        const double constant = 9.99;
        var dist = new ConstantDoubleDistribution(constant);
        dist.Next();
        dist.Reset();

        dist.Next().Should().Be(constant);
    }

    [Fact]
    public void ConstantDoubleDistribution_Configure_ChangesReturnedValue()
    {
        var dist = new ConstantDoubleDistribution(1.0);
        dist.Configure(99.0);

        dist.Next().Should().Be(99.0);
        dist.Mean.Should().Be(99.0);
    }

    [Fact]
    public void ConstantDoubleDistribution_Initialize_SetsInitializedFlag()
    {
        var dist = new ConstantDoubleDistribution();
        dist.Configure(3.0);
        dist.Initialize(42);

        dist.Initialized.Should().BeTrue();
    }

    [Fact]
    public void ConstantDoubleDistribution_SeedIgnored_SameValueRegardlessOfSeed()
    {
        var dist1 = new ConstantDoubleDistribution(5.0);
        var dist2 = new ConstantDoubleDistribution(5.0);
        dist1.Initialize(1);
        dist2.Initialize(9999);

        for (int i = 0; i < 10; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// LogNormalDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class LogNormalDistributionTests
{
    // Constructor / properties

    [Fact]
    public void LogNormalDistribution_Constructor_SetsPropertiesCorrectly()
    {
        const double mean = 3.0;
        const double stddev = 1.0;
        var dist = new LogNormalDistribution(42, mean: mean, stddev: stddev, antithetic: false, shift: 0.0);

        dist.Mean.Should().BeApproximately(mean, 1e-10);
        dist.NonStochasticValue.Should().BeApproximately(mean, 1e-10);
        dist.Name.Should().Be("Log Normal Distribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void LogNormalDistribution_Configure_ZeroSigma_Throws()
    {
        var dist = new LogNormalDistribution(42, antithetic: false);
        var act = () => dist.Configure(0.0, 0.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LogNormalDistribution_ConfigureMean_ZeroMean_Throws()
    {
        var dist = new LogNormalDistribution(42, antithetic: false);
        var act = () => dist.ConfigureMean(0.0, 1.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LogNormalDistribution_ConfigureMean_NegativeStddev_Throws()
    {
        var dist = new LogNormalDistribution(42, antithetic: false);
        var act = () => dist.ConfigureMean(1.0, -0.5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // All values must be strictly positive

    [Fact]
    public void LogNormalDistribution_Next_ValuesAreStrictlyPositive()
    {
        var dist = new LogNormalDistribution(42, mean: 2.0, stddev: 0.5, antithetic: false, shift: 0.0);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThan(0.0);
        }
    }

    [Fact]
    public void LogNormalDistribution_Next_ShiftedValuesExceedShift()
    {
        const double shift = 2.0;
        var dist = new LogNormalDistribution(42, mean: 3.0, stddev: 0.5, antithetic: false, shift: shift);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThan(shift);
        }
    }

    // DrawCount

    [Fact]
    public void LogNormalDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new LogNormalDistribution(42, mean: 1.0, stddev: 0.5, antithetic: false);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
    }

    // Seed reproducibility

    [Fact]
    public void LogNormalDistribution_SameSeed_ProducesSameSequence()
    {
        var dist1 = new LogNormalDistribution(42, mean: 2.0, stddev: 1.0, antithetic: false);
        var dist2 = new LogNormalDistribution(42, mean: 2.0, stddev: 1.0, antithetic: false);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Reset

    [Fact]
    public void LogNormalDistribution_Reset_ReproducesSequenceAndResetsDrawCount()
    {
        var dist = new LogNormalDistribution(42, mean: 2.0, stddev: 0.5, antithetic: false);
        double first = dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
        dist.Next().Should().Be(first);
    }

    // Statistical mean

    [Fact]
    public void LogNormalDistribution_Next_SampleMeanApproximatesConfiguredMean()
    {
        const int n = 10_000;
        const double mean = 3.0;
        const double stddev = 0.5;
        const double tolerance = 0.10;

        var dist = new LogNormalDistribution(42, mean: mean, stddev: stddev, antithetic: false);
        double sum = 0;
        for (int i = 0; i < n; i++) sum += dist.Next();

        (sum / n).Should().BeApproximately(mean, mean * tolerance);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TriangularDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class TriangularDistributionTests
{
    // Constructor / properties

    [Fact]
    public void TriangularDistribution_Constructor_SetsPropertiesCorrectly()
    {
        var dist = new TriangularDistribution(42, minimum: 0.0, mode: 3.0, maximum: 6.0);

        dist.Minimum.Should().Be(0.0);
        dist.Mode.Should().Be(3.0);
        dist.Maximum.Should().Be(6.0);
        dist.Mean.Should().BeApproximately((0.0 + 3.0 + 6.0) / 3.0, 1e-10);
        dist.Name.Should().Be("TriangularDistribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void TriangularDistribution_Configure_MaxLessThanMin_Throws()
    {
        var act = () => new TriangularDistribution(42, minimum: 10.0, mode: 5.0, maximum: 5.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TriangularDistribution_Configure_ModeLessThanMin_Throws()
    {
        var act = () => new TriangularDistribution(42, minimum: 5.0, mode: 3.0, maximum: 10.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TriangularDistribution_Configure_ModeGreaterThanMax_Throws()
    {
        var act = () => new TriangularDistribution(42, minimum: 0.0, mode: 12.0, maximum: 10.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TriangularDistribution_Mean_IsAverageOfMinModMax()
    {
        const double min = 1.0, mode = 4.0, max = 7.0;
        var dist = new TriangularDistribution(min, mode, max);

        dist.Mean.Should().BeApproximately((min + mode + max) / 3.0, 1e-10);
    }

    [Fact]
    public void TriangularDistribution_NonStochasticValue_EqualsMean()
    {
        var dist = new TriangularDistribution(42, minimum: 0.0, mode: 2.0, maximum: 8.0);

        dist.NonStochasticValue.Should().BeApproximately(dist.Mean, 1e-10);
    }

    // DrawCount

    [Fact]
    public void TriangularDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new TriangularDistribution(42, minimum: 0.0, mode: 0.5, maximum: 1.0);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
    }

    // Seed reproducibility

    [Fact]
    public void TriangularDistribution_SameSeed_ProducesSameSequence()
    {
        var dist1 = new TriangularDistribution(42, minimum: 0.0, mode: 5.0, maximum: 10.0);
        var dist2 = new TriangularDistribution(42, minimum: 0.0, mode: 5.0, maximum: 10.0);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Reset

    [Fact]
    public void TriangularDistribution_Reset_ReproducesSequenceAndResetsDrawCount()
    {
        var dist = new TriangularDistribution(42, minimum: 0.0, mode: 5.0, maximum: 10.0);
        double first = dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
        dist.Next().Should().Be(first);
    }

    // ConfigureMean

    [Fact]
    public void TriangularDistribution_ConfigureMean_SetsExpectedMean()
    {
        const double min = 0.0, mean = 5.0, max = 10.0;
        var dist = new TriangularDistribution(42, antithetic: false);
        dist.ConfigureMean(min, mean, max);

        dist.Mean.Should().BeApproximately(mean, 1e-10);
    }

    [Fact]
    public void TriangularDistribution_ConfigureMean_MeanAtBoundary_Throws()
    {
        var dist = new TriangularDistribution(42, antithetic: false);
        var act = () => dist.ConfigureMean(0.0, 0.0, 10.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// WeibullDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class WeibullDistributionTests
{
    // Constructor / properties

    [Fact]
    public void WeibullDistribution_Constructor_SetsPropertiesCorrectly()
    {
        const double mean = 2.0;
        const double k = 2.0;
        var dist = new WeibullDistribution(42, mean: mean, k: k, shift: 0.0, antithetic: false);

        dist.K.Should().Be(k);
        dist.Mean.Should().BeApproximately(mean, 1e-6);
        dist.NonStochasticValue.Should().BeApproximately(mean, 1e-6);
        dist.Name.Should().Be("Weibull Distribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    // Non-negative values

    [Fact]
    public void WeibullDistribution_Next_ValuesAreNonNegative()
    {
        var dist = new WeibullDistribution(42, mean: 2.0, k: 2.0, shift: 0.0, antithetic: false);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThanOrEqualTo(0.0);
        }
    }

    [Fact]
    public void WeibullDistribution_Next_ShiftedValuesExceedShift()
    {
        const double shift = 3.0;
        var dist = new WeibullDistribution(42, mean: 2.0, k: 2.0, shift: shift, antithetic: false);

        for (int i = 0; i < 1_000; i++)
        {
            dist.Next().Should().BeGreaterThanOrEqualTo(shift);
        }
    }

    // DrawCount

    [Fact]
    public void WeibullDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new WeibullDistribution(42, mean: 1.0, k: 1.0, shift: 0.0, antithetic: false);

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
    }

    // Seed reproducibility

    [Fact]
    public void WeibullDistribution_SameSeed_ProducesSameSequence()
    {
        var dist1 = new WeibullDistribution(42, mean: 2.0, k: 2.0, shift: 0.0, antithetic: false);
        var dist2 = new WeibullDistribution(42, mean: 2.0, k: 2.0, shift: 0.0, antithetic: false);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Reset

    [Fact]
    public void WeibullDistribution_Reset_ReproducesSequenceAndResetsDrawCount()
    {
        var dist = new WeibullDistribution(42, mean: 2.0, k: 2.0, shift: 0.0, antithetic: false);
        double first = dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
        dist.Next().Should().Be(first);
    }

    // ConfigureMean round-trip

    [Fact]
    public void WeibullDistribution_ConfigureMean_MeanPropertyMatchesInput()
    {
        const double mean = 5.0;
        const double k = 3.0;
        var dist = new WeibullDistribution(mean, k);

        dist.Mean.Should().BeApproximately(mean, 1e-10);
        dist.K.Should().Be(k);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ProbabilisticDistribution
// ─────────────────────────────────────────────────────────────────────────────

public class ProbabilisticDistributionTests
{
    private static List<int> EqualFrequencies(int count) => Enumerable.Repeat(1, count).ToList();

    // Constructor / properties

    [Fact]
    public void ProbabilisticDistribution_Constructor_SetsPropertiesCorrectly()
    {
        var frequencies = new List<int> { 1, 2, 3 };
        var dist = new ProbabilisticDistribution(42, 1, 3, frequencies);

        dist.Minimum.Should().Be(1);
        dist.Maximum.Should().Be(3);
        dist.Frequencies.Should().BeEquivalentTo(frequencies);
        dist.Name.Should().Be("Special Probabilistic Distribution");
        dist.Initialized.Should().BeTrue();
        dist.Configured.Should().BeTrue();
        dist.DrawCount.Should().Be(0);
    }

    [Fact]
    public void ProbabilisticDistribution_Configure_WrongFrequencyCount_Throws()
    {
        var act = () => new ProbabilisticDistribution(0, 4, new List<int> { 1, 2, 3 });

        act.Should().Throw<ArgumentException>();
    }

    // Next() values within range

    [Fact]
    public void ProbabilisticDistribution_Next_ValuesWithinConfiguredRange()
    {
        var frequencies = EqualFrequencies(5); // values 1-5, equal probability
        var dist = new ProbabilisticDistribution(42, 1, 5, frequencies);

        for (int i = 0; i < 1_000; i++)
        {
            int value = dist.Next();
            value.Should().BeInRange(1, 5);
        }
    }

    [Fact]
    public void ProbabilisticDistribution_Next_SingletonRange_AlwaysReturnsThatValue()
    {
        var frequencies = new List<int> { 1 };
        var dist = new ProbabilisticDistribution(42, 7, 7, frequencies);

        for (int i = 0; i < 20; i++)
        {
            dist.Next().Should().Be(7);
        }
    }

    // DrawCount

    [Fact]
    public void ProbabilisticDistribution_DrawCount_IncrementsOnNext()
    {
        var dist = new ProbabilisticDistribution(42, 0, 2, EqualFrequencies(3));

        dist.DrawCount.Should().Be(0);
        dist.Next();
        dist.DrawCount.Should().Be(1);
    }

    // Seed reproducibility

    [Fact]
    public void ProbabilisticDistribution_SameSeed_ProducesSameSequence()
    {
        var frequencies = EqualFrequencies(5);
        var dist1 = new ProbabilisticDistribution(42, 1, 5, frequencies);
        var dist2 = new ProbabilisticDistribution(42, 1, 5, frequencies);

        for (int i = 0; i < 50; i++)
        {
            dist1.Next().Should().Be(dist2.Next());
        }
    }

    // Reset

    [Fact]
    public void ProbabilisticDistribution_Reset_ResetsDrawCount()
    {
        var dist = new ProbabilisticDistribution(42, 0, 2, EqualFrequencies(3));
        dist.Next();
        dist.Reset();

        dist.DrawCount.Should().Be(0);
    }

    // Statistical coverage — all values from range appear

    [Fact]
    public void ProbabilisticDistribution_Next_AllValuesAppearOverManySamples()
    {
        const int min = 1, max = 5;
        var frequencies = EqualFrequencies(max - min + 1);
        var dist = new ProbabilisticDistribution(42, min, max, frequencies);
        var seen = new HashSet<int>();

        for (int i = 0; i < 500; i++)
        {
            seen.Add(dist.Next());
        }

        for (int v = min; v <= max; v++)
        {
            seen.Should().Contain(v, $"value {v} should appear in {500} draws");
        }
    }

    // Mean computation

    [Fact]
    public void ProbabilisticDistribution_Mean_ComputedCorrectlyForEqualWeights()
    {
        // values 1,2,3 equal frequency → mean = 2
        var frequencies = EqualFrequencies(3);
        var dist = new ProbabilisticDistribution(1, 3, frequencies);

        dist.Mean.Should().Be(2);
    }

    [Fact]
    public void ProbabilisticDistribution_Mean_ComputedCorrectlyForSkewedWeights()
    {
        // values 0,1,2 with frequencies 1,1,8 → mean ≈ round((0*1+1*1+2*8)/10) = round(1.7) = 2
        var frequencies = new List<int> { 1, 1, 8 };
        var dist = new ProbabilisticDistribution(0, 2, frequencies);

        dist.Mean.Should().Be(2);
    }
}
