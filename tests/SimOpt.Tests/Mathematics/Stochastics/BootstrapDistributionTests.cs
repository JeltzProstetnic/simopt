using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Statistics.Analysis;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-118 item 1: a bootstrap of observed values, each drawn with equal chance and with replacement.
/// A downstream integrator built this himself: <c>HistogramDoubleDistribution</c> interpolates between
/// bins and <c>ProbabilisticDistribution</c> is integer-only and draws without replacement.
/// </summary>
public class BootstrapDistributionTests
{
    private static readonly double[] Mined = { 3.5, 4.0, 4.0, 6.25, 9.0, 12.5 };   // 4.0 was seen twice

    [Fact]
    public void Every_observation_is_drawn_with_equal_chance()
    {
        var d = new BootstrapDistribution(seed: 7, Mined);
        double[] distinct = Mined.Distinct().ToArray();
        int[] counts = new int[distinct.Length];

        const int draws = 60_000;
        for (int i = 0; i < draws; i++)
            counts[Array.IndexOf(distinct, d.Next())]++;

        double[] expected = distinct.Select(v => draws * Mined.Count(m => m == v) / (double)Mined.Length).ToArray();
        GoodnessOfFit.ChiSquarePValue(counts, expected).Should().BeGreaterThan(0.001,
            "each observation is one equal share, so a value seen twice has two");
    }

    [Fact]
    public void The_instrument_rejects_a_bootstrap_that_ignores_duplicates()
    {
        // the same counts tested against "each distinct value equally likely" must fail:
        // the chi-square check has the power to see the duplicate's double share
        var d = new BootstrapDistribution(seed: 7, Mined);
        double[] distinct = Mined.Distinct().ToArray();
        int[] counts = new int[distinct.Length];
        const int draws = 60_000;
        for (int i = 0; i < draws; i++)
            counts[Array.IndexOf(distinct, d.Next())]++;

        double[] wrong = distinct.Select(_ => draws / (double)distinct.Length).ToArray();
        GoodnessOfFit.ChiSquarePValue(counts, wrong).Should().BeLessThan(1e-6);
    }

    [Fact]
    public void Draws_are_with_replacement()
    {
        var d = new BootstrapDistribution(seed: 11, new[] { 1d, 2d, 3d });

        double[] draws = Enumerable.Range(0, 3_000).Select(_ => d.Next()).ToArray();
        int repeats = draws.Zip(draws.Skip(1), (a, b) => a == b ? 1 : 0).Sum();

        repeats.Should().BeInRange(900, 1100, "a draw repeats the previous one a third of the time");
    }

    [Fact]
    public void Its_mean_is_the_mean_of_the_observations()
    {
        var d = new BootstrapDistribution(Mined);

        d.Mean.Should().BeApproximately(Mined.Average(), 1e-12);
        d.NonStochasticValue.Should().Be(d.Mean);
        d.Minimum.Should().Be(3.5);
        d.Maximum.Should().Be(12.5);
    }

    [Fact]
    public void The_same_seed_draws_the_same_values_and_a_reset_starts_over()
    {
        var a = new BootstrapDistribution(seed: 3, Mined);
        var b = new BootstrapDistribution(seed: 3, Mined);

        double[] first = Enumerable.Range(0, 50).Select(_ => a.Next()).ToArray();
        Enumerable.Range(0, 50).Select(_ => b.Next()).Should().Equal(first);

        a.Reset();
        Enumerable.Range(0, 50).Select(_ => a.Next()).Should().Equal(first);
        a.DrawCount.Should().Be(50);
    }

    [Fact]
    public void It_works_through_the_engines_random_wrapper()
    {
        var model = new SimOpt.Simulation.Engine.Model("bootstrap", 42, 0d);
        var d = new BootstrapDistribution(Mined);   // configured, not initialised: what the engine's Random<T> requires
        var r = new SimOpt.Simulation.Engine.Random<double>(model, d);

        Enumerable.Range(0, 200).Select(_ => r.Next()).Should().OnlyContain(v => Mined.Contains(v));
    }

    [Fact]
    public void It_refuses_no_observations_and_values_that_are_not_numbers()
    {
        Action empty = () => new BootstrapDistribution(Array.Empty<double>());
        Action nan = () => new BootstrapDistribution(new[] { 1d, double.NaN });
        Action infinite = () => new BootstrapDistribution(new[] { double.PositiveInfinity });

        empty.Should().Throw<ArgumentException>().WithMessage("*at least one*");
        nan.Should().Throw<ArgumentException>().WithMessage("*finite*");
        infinite.Should().Throw<ArgumentException>().WithMessage("*finite*");
    }
}
