using System;
using FluentAssertions;
using SimOpt.Mathematics.SpecialFunctions;
using Xunit;

namespace SimOpt.Tests.Mathematics.SpecialFunctions;

/// <summary>
/// SIM-63 — the Student's t quantile that puts a confidence interval around a replication mean.
///
/// <para>
/// The constants below come from root-finding on the exact identity
/// F(t;ν) = 1 − ½·I_{ν/(ν+t²)}(ν/2, ½) in high precision, verified against the closed forms that
/// exist for ν = 1 (Cauchy, tan(19π/40)), ν = 2 and ν = 4. They are asserted as literals on
/// purpose: that is what makes this a regression gate on the root-finder rather than a tautology
/// checking the implementation against itself.
/// </para>
/// <para>
/// The production path is the root-finder, not a table. A table is silently wrong at every degree
/// of freedom it does not contain, which is unacceptable in software whose output is used as expert
/// evidence; a root-finder with no pinned values is merely untested. Hence both.
/// </para>
/// </summary>
public class StudentTTests
{
    [Theory]
    [InlineData(1, 12.7062047362)]
    [InlineData(2, 4.3026527297)]
    [InlineData(3, 3.1824463053)]
    [InlineData(4, 2.7764451052)]
    [InlineData(5, 2.5705818356)]
    [InlineData(9, 2.2621571628)]
    [InlineData(10, 2.2281388520)]
    [InlineData(19, 2.0930240544)]
    [InlineData(20, 2.0859634473)]
    [InlineData(29, 2.0452296421)]
    [InlineData(30, 2.0422724563)]
    public void TwoSidedCriticalValue_MatchesThePublishedTable(int df, double expected)
    {
        StudentT.TwoSidedCriticalValue(0.05, df).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void TheOneDegreeOfFreedomCase_MatchesItsExactClosedForm()
    {
        // ν = 1 is the Cauchy distribution, whose quantile is available in closed form. It is also
        // the case a two-replication experiment lands on, so it is the one most likely to be
        // reached in anger — and its enormous value (12.7) is the honest signal that two
        // replications buy almost nothing.
        double exact = Math.Tan(19d * Math.PI / 40d);
        StudentT.TwoSidedCriticalValue(0.05, 1).Should().BeApproximately(exact, 1e-10);
    }

    [Fact]
    public void AtLargeDegreesOfFreedom_ItConvergesToTheNormalQuantileFromAbove()
    {
        const double z = 1.959963984540;   // z_{0.975}

        double t1e3 = StudentT.TwoSidedCriticalValue(0.05, 1000);
        double t1e5 = StudentT.TwoSidedCriticalValue(0.05, 100000);
        double t1e7 = StudentT.TwoSidedCriticalValue(0.05, 10000000);

        // The t exceeds z at every finite ν and decreases toward it — asserting equality at some
        // arbitrary "large" ν would be wrong, because the gap is a real quantity, not error.
        t1e3.Should().BeGreaterThan(z);
        t1e5.Should().BeLessThan(t1e3).And.BeGreaterThan(z);
        t1e7.Should().BeLessThan(t1e5).And.BeGreaterThan(z);

        // The gap follows the standard expansion t ≈ z(1 + (z²+1)/(4ν)), which is 2.372e-5 at
        // ν = 100,000. Pinning the RATE of convergence rather than a single value catches an
        // off-by-one in the degrees of freedom or a one-sided/two-sided mix-up — errors that stay
        // small at large ν and so slip past the table rows above.
        (t1e5 - z).Should().BeApproximately(z * (z * z + 1d) / (4d * 100000d), 1e-8);
        (t1e7 - z).Should().BeLessThan(1e-6);
    }

    [Fact]
    public void QuantileAndCdf_AreInverses()
    {
        foreach (int df in new[] { 1, 2, 5, 9, 19, 30, 120 })
        {
            foreach (double p in new[] { 0.01, 0.1, 0.25, 0.5, 0.75, 0.9, 0.975, 0.99 })
            {
                double t = StudentT.Quantile(p, df);
                StudentT.Cdf(t, df).Should().BeApproximately(p, 1e-9,
                    "round-tripping p={0} at df={1} must return the same probability", p, df);
            }
        }
    }

    [Fact]
    public void TheDistributionIsSymmetric()
    {
        StudentT.Quantile(0.975, 9).Should().BeApproximately(-StudentT.Quantile(0.025, 9), 1e-12);
        StudentT.Quantile(0.5, 9).Should().Be(0d);
        StudentT.Cdf(0d, 9).Should().BeApproximately(0.5, 1e-12);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void AProbabilityOutsideTheOpenUnitInterval_IsRejected(double p)
    {
        // Returning +/-infinity here would propagate silently into a reported half-width.
        Action act = () => StudentT.Quantile(p, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ZeroDegreesOfFreedom_IsRejected()
    {
        Action act = () => StudentT.TwoSidedCriticalValue(0.05, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
