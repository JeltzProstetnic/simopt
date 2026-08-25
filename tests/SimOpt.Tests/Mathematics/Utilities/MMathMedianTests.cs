using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Mathematics;
using Xunit;

namespace SimOpt.Tests.Mathematics.Utilities;

/// <summary>
/// SIM-57 — the median.
///
/// <para>
/// Both median implementations indexed one position too high. <see cref="MMath.Median(double[])"/>
/// read <c>data[N/2]</c> and <c>data[N/2 + 1]</c> for even N and <c>data[(N+1)/2]</c> for odd N,
/// which is off by one in both branches and reads out of bounds for N of 1 or 2. The
/// <c>List&lt;T&gt;</c> extension overloads repeat the odd-N error independently.
/// </para>
/// <para>
/// A median is the statistic a user reaches for precisely when the mean is misleading — a skewed
/// waiting-time distribution — so a silently-shifted median misleads exactly where it was supposed
/// to help.
/// </para>
/// </summary>
public class MMathMedianTests
{
    // ---- MMath.Median(double[]) ----

    [Fact]
    public void Median_OddCount_IsTheMiddleValue()
    {
        MMath.Median(new double[] { 1, 2, 3, 4, 5 }).Should().Be(3);
    }

    [Fact]
    public void Median_EvenCount_IsTheMeanOfTheTwoMiddleValues()
    {
        MMath.Median(new double[] { 1, 2, 3, 4 }).Should().Be(2.5);
    }

    [Fact]
    public void Median_SingleValue_IsThatValue()
    {
        MMath.Median(new double[] { 42 }).Should().Be(42);
    }

    [Fact]
    public void Median_TwoValues_IsTheirMean()
    {
        MMath.Median(new double[] { 10, 20 }).Should().Be(15);
    }

    [Fact]
    public void Median_UnsortedInput_IsSortedFirst()
    {
        MMath.Median(new double[] { 9, 1, 7, 3, 5 }).Should().Be(5);
    }

    [Fact]
    public void Median_DoesNotMutateCallerArray()
    {
        var values = new double[] { 9, 1, 7 };

        MMath.Median(values);

        values.Should().ContainInOrder(9, 1, 7)
            .And.HaveCount(3, "the caller's array must not be sorted in place");
    }

    [Fact]
    public void Median_AlreadySortedFlag_SkipsSortingButStillIndexesCorrectly()
    {
        MMath.Median(new double[] { 1, 2, 3, 4, 5 }, alreadySorted: true).Should().Be(3);
        MMath.Median(new double[] { 1, 2, 3, 4 }, alreadySorted: true).Should().Be(2.5);
    }

    /// <summary>
    /// A skewed sample is where the off-by-one does real damage: for a right-skewed waiting-time
    /// sample the shifted index reports a value well above the true centre.
    /// </summary>
    [Fact]
    public void Median_SkewedSample_ReportsTheTrueCentre()
    {
        var waitingTimes = new double[] { 0.5, 0.7, 0.9, 1.1, 1.3, 4.0, 30.0 };

        MMath.Median(waitingTimes).Should().Be(1.1);
    }

    // ---- List<T> extension overloads (sorted input assumed) ----

    [Fact]
    public void ListMedian_Int_OddCount_IsTheMiddleValue()
    {
        new List<int> { 1, 2, 3, 4, 5 }.Median().Should().Be(3);
    }

    [Fact]
    public void ListMedian_Int_EvenCount_IsTheMeanOfTheTwoMiddleValues()
    {
        new List<int> { 1, 2, 3, 4 }.Median().Should().Be(2.5);
    }

    [Fact]
    public void ListMedian_Long_OddCount_IsTheMiddleValue()
    {
        new List<long> { 1, 2, 3 }.Median().Should().Be(2);
    }

    [Fact]
    public void ListMedian_Float_OddCount_IsTheMiddleValue()
    {
        new List<float> { 1f, 2f, 3f, 4f, 5f }.Median().Should().Be(3f);
    }

    [Fact]
    public void ListMedian_Double_OddCount_IsTheMiddleValue()
    {
        new List<double> { 1, 2, 3, 4, 5 }.Median().Should().Be(3);
    }

    [Fact]
    public void ListMedian_Decimal_OddCount_IsTheMiddleValue()
    {
        new List<decimal> { 1m, 2m, 3m, 4m, 5m }.Median().Should().Be(3m);
    }

    [Fact]
    public void ListMedian_SingleValue_IsThatValue()
    {
        new List<double> { 42 }.Median().Should().Be(42);
        new List<int> { 42 }.Median().Should().Be(42);
    }

    /// <summary>
    /// The array and list implementations must agree — they are the same statistic and callers
    /// reach for whichever matches the type they happen to hold.
    /// </summary>
    [Fact]
    public void ListMedian_AgreesWithArrayMedian()
    {
        var sorted = new double[] { 2, 4, 6, 8, 10, 12, 14 };

        new List<double>(sorted).Median().Should().Be(MMath.Median(sorted));
    }
}
