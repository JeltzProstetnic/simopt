using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Statistics.Analysis;
using Xunit;

namespace SimOpt.Tests.Statistics;

/// <summary>
/// SIM-64 — goodness-of-fit tests, the second half of the analytic battery's remit.
///
/// <para>
/// The queueing benchmarks check the engine's <b>means</b>. They cannot see a sampler that draws
/// from the wrong distribution while happening to have the right mean, and a mean-correct sampler
/// with the wrong shape produces confident, plausible, wrong answers everywhere the tail matters —
/// which in queueing is everywhere.
/// </para>
///
/// <para>
/// The statistics are pinned against values computed by hand before the implementation existed, and
/// the critical values against the published table (<c>docs/2026-08-26-analytic-reference.md</c>
/// §4). A test that only ever checked "the sampler is not rejected" would pass just as happily
/// against an instrument with no power at all, so every acceptance here is paired with a rejection.
/// </para>
/// </summary>
public class GoodnessOfFitTests
{
    private static readonly Func<double, double> UniformCdf = x => Math.Clamp(x, 0d, 1d);

    // ── the Kolmogorov–Smirnov statistic ─────────────────────────────────────

    [Fact]
    public void TheKsStatistic_MatchesTheHandComputedValue()
    {
        // Sample {0.1, 0.5, 0.9} against Uniform(0,1). The ECDF steps to i/3 at each point, so
        //   D⁺ = max(i/n − F(xᵢ))     = max(0.2333, 0.1667, 0.1000) = 7/30
        //   D⁻ = max(F(xᵢ) − (i−1)/n) = max(0.1000, 0.1667, 0.2333) = 7/30
        double d = GoodnessOfFit.KolmogorovSmirnov(new[] { 0.1, 0.5, 0.9 }, UniformCdf);

        d.Should().BeApproximately(7d / 30d, 1e-12);
    }

    [Fact]
    public void TheKsStatistic_TakesTheLargerOfTheTwoOneSidedDeviations()
    {
        // {0.7, 0.8, 0.9} against Uniform(0,1): D⁺ = 0.1 but D⁻ = 0.7. An implementation that
        // computed only the step-above deviation — the common shortcut — would report 0.1 and
        // cheerfully accept a sample that is nowhere near uniform.
        double d = GoodnessOfFit.KolmogorovSmirnov(new[] { 0.7, 0.8, 0.9 }, UniformCdf);

        d.Should().BeApproximately(0.7, 1e-12);
    }

    [Fact]
    public void TheKsStatistic_DoesNotDependOnTheOrderTheSampleArrivesIn()
    {
        double ordered = GoodnessOfFit.KolmogorovSmirnov(new[] { 0.1, 0.5, 0.9 }, UniformCdf);
        double shuffled = GoodnessOfFit.KolmogorovSmirnov(new[] { 0.9, 0.1, 0.5 }, UniformCdf);

        // The caller hands over a sample, not a sorted sample. Sorting is the instrument's job.
        shuffled.Should().Be(ordered);
    }

    [Fact]
    public void TheKsCriticalValues_MatchThePublishedTable()
    {
        // c_α = √(−½ ln(α/2)), the asymptotic two-sided Kolmogorov distribution.
        GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.10).Should().BeApproximately(1.22387, 1e-5);
        GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.05).Should().BeApproximately(1.35810, 1e-5);
        GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01).Should().BeApproximately(1.62762, 1e-5);
    }

    // ── the Anderson–Darling statistic ───────────────────────────────────────

    [Fact]
    public void TheAndersonDarlingStatistic_MatchesTheHandComputedValue()
    {
        // Same sample, same null: A² = −n − (1/n)Σ(2i−1)[ln F(xᵢ) + ln(1 − F(x_{n+1−i}))].
        // With n = 3 and F(x) = x this evaluates to 0.2725528086…
        double a2 = GoodnessOfFit.AndersonDarling(new[] { 0.1, 0.5, 0.9 }, UniformCdf);

        a2.Should().BeApproximately(0.27255280864, 1e-10);
    }

    [Fact]
    public void TheAndersonDarlingCriticalValues_MatchThePublishedTable()
    {
        // Fully-specified (no parameter estimated from the sample) critical values. These are
        // tabulated constants, not a closed form — which is exactly why they are pinned here.
        GoodnessOfFit.AndersonDarlingCriticalValue(0.10).Should().BeApproximately(1.933, 1e-9);
        GoodnessOfFit.AndersonDarlingCriticalValue(0.05).Should().BeApproximately(2.492, 1e-9);
        GoodnessOfFit.AndersonDarlingCriticalValue(0.025).Should().BeApproximately(3.070, 1e-9);
        GoodnessOfFit.AndersonDarlingCriticalValue(0.01).Should().BeApproximately(3.857, 1e-9);
    }

    [Fact]
    public void AnUntabulatedSignificanceLevel_IsRefusedRatherThanInterpolated()
    {
        // Silently returning the nearest tabulated value would make a stated α a lie, and the
        // reader of a rejection has no way to notice.
        Action act = () => GoodnessOfFit.AndersonDarlingCriticalValue(0.02);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── the samplers the engine actually runs on ─────────────────────────────

    /// <summary>
    /// n = 10,000, deliberately not more.
    /// </summary>
    /// <remarks>
    /// At n = 10⁶ the KS test has the power to reject on deviations of order 10⁻³ — including the
    /// ones this codebase's Mersenne Twister genuinely has (SIM-81), which are real but irrelevant
    /// to any queueing answer. A gate that fails on a defect nobody is going to fix, and that does
    /// not affect the product, is a gate that gets switched off.
    /// </remarks>
    private const int SampleSize = 10_000;

    private static double[] ExponentialSample(double rate, int seed, int n = SampleSize)
    {
        var d = new NegExponentialDistribution(seed, antithetic: false);
        d.Configure(rate);
        return Enumerable.Range(0, n).Select(_ => d.Next()).ToArray();
    }

    private static Func<double, double> ExponentialCdf(double rate)
        => x => x <= 0d ? 0d : 1d - Math.Exp(-rate * x);

    [Fact]
    public void TheExponentialSampler_IsNotRejectedAgainstItsOwnDistribution()
    {
        double[] sample = ExponentialSample(rate: 0.8, seed: 20_260_826);

        double d = GoodnessOfFit.KolmogorovSmirnov(sample, ExponentialCdf(0.8));
        double a2 = GoodnessOfFit.AndersonDarling(sample, ExponentialCdf(0.8));

        (Math.Sqrt(SampleSize) * d).Should().BeLessThan(GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01),
            $"the sampler must not be distinguishable from Exp(0.8): sqrt(n)*D = {Math.Sqrt(SampleSize) * d:F5}");
        a2.Should().BeLessThan(GoodnessOfFit.AndersonDarlingCriticalValue(0.01),
            $"Anderson-Darling weights the upper tail, which is where queueing is most sensitive: A2 = {a2:F5}");
    }

    [Fact]
    public void ASamplerWithTheWrongRate_IsRejected()
    {
        // The instrument must have power, or the test above is worthless. A 10% rate error leaves
        // the distribution the right *shape* and every mean-based check in the battery would still
        // be within a few per cent of it — this is precisely the class of defect the queueing
        // benchmarks cannot see.
        double[] sample = ExponentialSample(rate: 0.8, seed: 20_260_826);

        double d = GoodnessOfFit.KolmogorovSmirnov(sample, ExponentialCdf(0.88));
        double a2 = GoodnessOfFit.AndersonDarling(sample, ExponentialCdf(0.88));

        (Math.Sqrt(SampleSize) * d).Should().BeGreaterThan(GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01));
        a2.Should().BeGreaterThan(GoodnessOfFit.AndersonDarlingCriticalValue(0.01));
    }

    [Fact]
    public void AShiftedSampler_IsRejected()
    {
        // A constant shift is the signature of an off-by-one in an inverse transform — SIM-56 was
        // exactly this class of defect — and it moves the mean by the same shift, so it is caught
        // here as well as by any mean check. Included because a GOF instrument that only detected
        // rate errors would be half an instrument.
        double[] sample = ExponentialSample(rate: 1.0, seed: 4_242).Select(x => x + 0.05).ToArray();

        double a2 = GoodnessOfFit.AndersonDarling(sample, ExponentialCdf(1.0));

        a2.Should().BeGreaterThan(GoodnessOfFit.AndersonDarlingCriticalValue(0.01));
    }

    [Fact]
    public void TheUniformSampler_IsNotRejectedAgainstUniform()
    {
        var d = new UniformDoubleDistribution();
        d.Configure(0d, 1d);
        d.Initialize(20_260_826, false);

        double[] sample = Enumerable.Range(0, SampleSize).Select(_ => d.Next()).ToArray();
        double ks = GoodnessOfFit.KolmogorovSmirnov(sample, UniformCdf);

        (Math.Sqrt(SampleSize) * ks).Should().BeLessThan(GoodnessOfFit.KolmogorovSmirnovCriticalValue(0.01),
            $"sqrt(n)*D = {Math.Sqrt(SampleSize) * ks:F5}");
    }

    // ── chi-square, for the discrete samplers ────────────────────────────────

    [Fact]
    public void TheChiSquareStatistic_MatchesTheHandComputedValue()
    {
        // Observed {10,20,30,40} against 25 expected in each cell:
        // Σ(o−e)²/e = (225 + 25 + 25 + 225)/25 = 20.
        double x = GoodnessOfFit.ChiSquare(new[] { 10, 20, 30, 40 }, new[] { 25d, 25d, 25d, 25d });

        x.Should().BeApproximately(20d, 1e-12);
    }

    [Fact]
    public void TheChiSquarePValue_IsTheUpperTail()
    {
        // χ² = 20 on 3 degrees of freedom has an upper-tail probability of 0.000170…, so the
        // p-value must be tiny rather than close to 1 — the direction is the thing worth pinning,
        // because an implementation returning the lower tail would silently accept everything.
        double p = GoodnessOfFit.ChiSquarePValue(new[] { 10, 20, 30, 40 }, new[] { 25d, 25d, 25d, 25d });

        p.Should().BeApproximately(0.00017, 1e-5);
    }

    [Fact]
    public void AFairSampleIsNotRejected_ByTheSameInstrumentThatRejectsALoadedOne()
    {
        double fair = GoodnessOfFit.ChiSquarePValue(
            new[] { 251, 249, 248, 252 }, Enumerable.Repeat(250d, 4).ToArray());
        double loaded = GoodnessOfFit.ChiSquarePValue(
            new[] { 400, 200, 200, 200 }, Enumerable.Repeat(250d, 4).ToArray());

        fair.Should().BeGreaterThan(0.05);
        loaded.Should().BeLessThan(0.01);
    }

    [Fact]
    public void MismatchedCellCounts_AreRefused()
    {
        Action act = () => GoodnessOfFit.ChiSquare(new[] { 1, 2 }, new[] { 1d });

        act.Should().Throw<ArgumentException>();
    }

    // ── guards ───────────────────────────────────────────────────────────────

    [Fact]
    public void AnEmptySample_HasNoStatistic()
    {
        Action ks = () => GoodnessOfFit.KolmogorovSmirnov(Array.Empty<double>(), UniformCdf);
        Action ad = () => GoodnessOfFit.AndersonDarling(Array.Empty<double>(), UniformCdf);

        ks.Should().Throw<ArgumentException>();
        ad.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ASampleTheNullAssignsZeroProbabilityTo_IsReportedRatherThanReturningInfinity()
    {
        // A² takes ln F(x) and ln(1 − F(x)). A sample point outside the null's support drives one
        // of those to −∞ and the statistic to +∞ or NaN, which would read as "rejected" for the
        // wrong reason — the failure is in the null or the sample, not in the fit, and the caller
        // needs to be told which.
        Action act = () => GoodnessOfFit.AndersonDarling(new[] { -1.0, 0.5 }, ExponentialCdf(1.0));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*support*");
    }
}
