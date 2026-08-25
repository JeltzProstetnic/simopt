using System;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-56 — the random-source contract.
///
/// <para>
/// <see cref="IRandomSource"/> documents <c>NextInteger()</c> as <c>[0, int.MaxValue)</c> and
/// <c>NextDouble()</c> as <c>[0, 1)</c>. Every distribution in the framework, and therefore every
/// simulated duration, rests on those two guarantees. Before SIM-56 none of them was tested and
/// three of the four generators violated at least one.
/// </para>
/// <para>
/// These tests are deliberately deterministic wherever the defect allows it: a statistical test
/// that only fails once every few billion draws is not a regression gate.
/// </para>
/// </summary>
public class RandomSourceContractTests
{
    private const int SampleSize = 200_000;

    // ---------------------------------------------------------------------------------------
    // UniformMapping — the shared raw-draw → contract-range mapping.
    // Testing the pure function makes the boundary cases exhaustive and instantaneous, instead
    // of hoping a 2^-32 event shows up in a sampling loop.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The bug that crashes a long optimization run: <c>Math.Abs((int)0x80000000u)</c> is
    /// <c>Math.Abs(int.MinValue)</c>, which throws <see cref="OverflowException"/>. One draw in
    /// 2^32 — rare per draw, near-certain across an optimization campaign, and a hard crash.
    /// </summary>
    [Fact]
    public void UniformMapping_IntMinValueBitPattern_DoesNotThrow()
    {
        var act = () => UniformMapping.TryMapToInteger(0x80000000u, out _);
        act.Should().NotThrow<OverflowException>();
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x00000001u)]
    [InlineData(0x7FFFFFFEu)]
    [InlineData(0x7FFFFFFFu)]
    [InlineData(0x80000000u)]  // int.MinValue bit pattern — the overflow case
    [InlineData(0x80000001u)]
    [InlineData(0xFFFFFFFEu)]
    [InlineData(0xFFFFFFFFu)]
    public void UniformMapping_BoundaryDraws_MapIntoContractRange(uint raw)
    {
        if (UniformMapping.TryMapToInteger(raw, out var value))
        {
            value.Should().BeGreaterThanOrEqualTo(0);
            value.Should().BeLessThan(int.MaxValue, "the contract interval [0, int.MaxValue) is half-open");
        }
    }

    /// <summary>
    /// The excluded endpoint must be rejected rather than clamped or wrapped — clamping biases the
    /// top of the range and wrapping biases the bottom.
    /// </summary>
    [Fact]
    public void UniformMapping_ExcludedEndpoint_IsRejected()
    {
        // Exactly two raw draws fold onto int.MaxValue once the sign bit is masked away.
        UniformMapping.TryMapToInteger(0x7FFFFFFFu, out _).Should().BeFalse();
        UniformMapping.TryMapToInteger(0xFFFFFFFFu, out _).Should().BeFalse();
    }

    /// <summary>
    /// The mapping must not discard entropy. Masking the sign bit preserves the low 31 bits;
    /// shifting instead would confine MersenneTwister's first 624 draws — which come straight
    /// from <c>System.Random.Next()</c> and never set the high bit — to the lower half of the
    /// range, putting <c>NextDouble()</c> on [0, 0.5).
    /// </summary>
    [Theory]
    [InlineData(0x00000001u, 1)]
    [InlineData(0x12345678u, 0x12345678)]
    [InlineData(0x7FFFFFFEu, 0x7FFFFFFE)]
    public void UniformMapping_PreservesLowBits(uint raw, int expected)
    {
        UniformMapping.TryMapToInteger(raw, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    /// <summary>
    /// The whole documented range must be reachable from a source whose high bit is never set —
    /// which is precisely MersenneTwister's seeded state.
    /// </summary>
    [Fact]
    public void MersenneTwister_FirstDraws_SpanTheFullUnitInterval()
    {
        var rnd = new MersenneTwister(20260825);
        var values = new double[600]; // fewer than the 624-draw seeded window
        for (int i = 0; i < values.Length; i++) values[i] = rnd.NextDouble();

        System.Linq.Enumerable.Average(values).Should().BeApproximately(0.5, 0.05,
            "the seeded window must not be confined to a sub-interval");
        System.Linq.Enumerable.Max(values).Should().BeGreaterThan(0.9);
    }

    [Fact]
    public void UniformMapping_ToDouble_StaysBelowOne()
    {
        UniformMapping.ToDouble(0).Should().Be(0.0);
        UniformMapping.ToDouble(int.MaxValue - 1).Should().BeLessThan(1.0);
    }

    // ---------------------------------------------------------------------------------------
    // Generator contract — all four sources, both polarities.
    // ---------------------------------------------------------------------------------------

    public static TheoryData<string, bool> AllSources => new()
    {
        { nameof(MersenneTwister), false },
        { nameof(MersenneTwister), true },
        { nameof(R250_521), false },
        { nameof(R250_521), true },
        { nameof(LinearCongruentialGenerator), false },
        { nameof(LinearCongruentialGenerator), true },
        { nameof(SubtractiveCongruentialGenerator), false },
        { nameof(SubtractiveCongruentialGenerator), true },
    };

    private static IRandomSource Create(string name, bool antithetic) => name switch
    {
        nameof(MersenneTwister) => new MersenneTwister(12345, antithetic),
        nameof(R250_521) => new R250_521(12345, antithetic),
        nameof(LinearCongruentialGenerator) => new LinearCongruentialGenerator(12345, antithetic),
        nameof(SubtractiveCongruentialGenerator) => new SubtractiveCongruentialGenerator(12345, antithetic),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown random source")
    };

    [Theory]
    [MemberData(nameof(AllSources))]
    public void NextInteger_HonoursDocumentedRange(string source, bool antithetic)
    {
        var rnd = Create(source, antithetic);

        for (int i = 0; i < SampleSize; i++)
        {
            var value = rnd.NextInteger();
            value.Should().BeGreaterThanOrEqualTo(0,
                "{0} (antithetic={1}) must never return a negative integer", source, antithetic);
            value.Should().BeLessThan(int.MaxValue,
                "{0} (antithetic={1}) documents [0, int.MaxValue)", source, antithetic);
        }
    }

    [Theory]
    [MemberData(nameof(AllSources))]
    public void NextDouble_HonoursUnitInterval(string source, bool antithetic)
    {
        var rnd = Create(source, antithetic);

        for (int i = 0; i < SampleSize; i++)
        {
            var value = rnd.NextDouble();
            value.Should().BeGreaterThanOrEqualTo(0.0,
                "{0} (antithetic={1}) must never return a negative variate — Math.Log of one is NaN",
                source, antithetic);
            value.Should().BeLessThan(1.0,
                "{0} (antithetic={1}) documents [0, 1); returning 1.0 breaks inverse-transform samplers",
                source, antithetic);
        }
    }

    /// <summary>
    /// A generator that only produces values in a fraction of its range is broken even if every
    /// value it produces is technically inside the contract. Guards against the LCG defect below
    /// silently reappearing as "returns 0 or 1".
    /// </summary>
    [Theory]
    [MemberData(nameof(AllSources))]
    public void NextInteger_SpansItsRange(string source, bool antithetic)
    {
        var rnd = Create(source, antithetic);
        var low = 0;
        var high = 0;

        for (int i = 0; i < 10_000; i++)
        {
            if (rnd.NextInteger() < int.MaxValue / 2) low++; else high++;
        }

        low.Should().BeGreaterThan(0, "{0} must produce values in the lower half of its range", source);
        high.Should().BeGreaterThan(0, "{0} must produce values in the upper half of its range", source);
    }

    // ---------------------------------------------------------------------------------------
    // Generator-specific defects, tested deterministically.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// R250_521 advances its 521-entry buffer with <c>(i2 != 521) ? i2 + 1 : 0</c>. Valid indices
    /// are 0..520, so the index reaches 521 and the next draw indexes past the end. Deterministic:
    /// it crashes on a specific draw, every time.
    /// </summary>
    [Fact]
    public void R250_521_SurvivesPastItsBufferLength()
    {
        var rnd = new R250_521(4242);

        var act = () =>
        {
            for (int i = 0; i < 5_000; i++) rnd.NextInteger();
        };

        act.Should().NotThrow<IndexOutOfRangeException>(
            "the 521-entry buffer has valid indices 0..520; wrapping at 521 reads past the end");
    }

    /// <summary>
    /// <c>LinearCongruentialGenerator.NextInteger()</c> calls <c>Next(int.MaxValue)</c> where a bit
    /// count is expected. The shift <c>48 - int.MaxValue</c> is masked to 49, and the internal seed
    /// is only 48 bits wide, so the result is always 0 — and every
    /// <c>UniformIntegerDistribution</c> built on an LCG silently returns its minimum forever.
    /// </summary>
    [Fact]
    public void LinearCongruentialGenerator_NextInteger_IsNotConstantZero()
    {
        var rnd = new LinearCongruentialGenerator(99);
        var distinct = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < 1_000; i++) distinct.Add(rnd.NextInteger());

        distinct.Should().HaveCountGreaterThan(1,
            "an LCG returning a constant makes every integer distribution built on it degenerate");
    }

    // ---------------------------------------------------------------------------------------
    // Exponential-family guards: NextDouble() legitimately returns 0.0, and log(0) is -infinity.
    // A zero-returning source proves the guard deterministically.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void NegExponential_IsFinite_WhenUnderlyingDrawIsZero()
    {
        var dist = new NegExponentialDistribution(new ZeroRandomSource(), lambda: 0.5, shift: 0);

        var value = dist.Next();

        double.IsFinite(value).Should().BeTrue(
            "-(1/lambda) * log(0) is +infinity, which becomes an event scheduled at infinity");
    }

    [Fact]
    public void Erlang_IsFinite_WhenUnderlyingDrawIsZero()
    {
        var dist = new ErlangDistribution(new ZeroRandomSource(), mean: 10, k: 3);

        var value = dist.Next();

        double.IsFinite(value).Should().BeTrue(
            "a single zero factor drives the product to zero and the logarithm to -infinity");
    }

    [Theory]
    [InlineData(nameof(MersenneTwister))]
    [InlineData(nameof(R250_521))]
    [InlineData(nameof(LinearCongruentialGenerator))]
    public void NegExponential_IsAlwaysFinite_AcrossSources(string source)
    {
        var dist = new NegExponentialDistribution(Create(source, false), lambda: 0.5, shift: 0);

        for (int i = 0; i < SampleSize; i++)
            double.IsFinite(dist.Next()).Should().BeTrue("{0} must not yield an infinite duration", source);
    }

    /// <summary>
    /// A random source that always yields the lower endpoint of the contract interval. Legal
    /// output — <c>[0, 1)</c> includes zero — which is exactly why samplers taking a logarithm
    /// must guard rather than assume.
    /// </summary>
    private sealed class ZeroRandomSource : IRandomSource
    {
        public bool Antithetic => false;
        public int Seed => 0;
        public string Name => "Always Zero";
        public bool Initialized => true;

        public int NextInteger() => 0;
        public double NextDouble() => 0.0;

        public void Initialize() { }
        public void Initialize(int seed) { }
        public void Initialize(int seed, bool antithetic) { }
        public void Initialize(bool antithetic) { }

        public void Reset() { }
        public void Reset(int seed) { }
        public void Reset(int seed, bool antithetic) { }
        public void Reset(bool antithetic) { }
    }
}
