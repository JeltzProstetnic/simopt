using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Interfaces;
using SimOpt.Mathematics.Stochastics.RandomSources;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-98: the three random sources besides the Mersenne Twister (SIM-81) checked against something.
///
/// <para>
/// <see cref="LinearCongruentialGenerator"/> is <c>java.util.Random</c>; its vectors below were printed by
/// the JDK itself (OpenJDK 21.0.13, <c>new Random(seed).nextDouble()</c> and <c>nextInt() &gt;&gt;&gt; 1</c>),
/// and they contain the widely published values <c>new Random(0).nextDouble() = 0.730967787376657</c> and
/// <c>new Random(42).nextDouble() = 0.7275636800328681</c>.
/// </para>
/// <para>
/// <see cref="SubtractiveCongruentialGenerator"/> wraps the seeded <c>System.Random</c>, which is Knuth's
/// subtractive generator (TAOCP vol. 2, 3.6) as .NET Framework published it. It is checked against
/// <see cref="Knuth"/>, written here from that published algorithm, so a .NET release that changed the
/// seeded generator would fail this test instead of silently moving every run made with it.
/// </para>
/// <para>
/// <see cref="R250_521"/> has no published reference vector for its seeding, so it can only be checked
/// for uniformity; it is not for a defensible run (use <see cref="MersenneTwister"/>). The uniformity
/// checks run on all four generators at fixed seeds, with limits at the 99.9 % quantile.
/// </para>
/// </summary>
public class RandomSourceReferenceTests
{
    // ── LinearCongruentialGenerator = java.util.Random ────────────────────────────────────────

    [Theory]
    [InlineData(0, new[] { 0.730967787376657, 0.24053641567148587, 0.6374174253501083, 0.5504370051176339, 0.5975452777972018 }, 0.8440817772259168)]
    [InlineData(1, new[] { 0.7308781907032909, 0.41008081149220166, 0.20771484130971707, 0.3327170559595112, 0.9677559094241207 }, 0.11120927191077379)]
    [InlineData(42, new[] { 0.7275636800328681, 0.6832234717598454, 0.30871945533265976, 0.27707849007413665, 0.6655489517945736 }, 0.9949257705877734)]
    [InlineData(-7, new[] { 0.2691218093260761, 0.6731811902798038, 0.5214896372918163, 0.8518782305056931, 0.08574465212415472 }, 0.999887873680794)]
    [InlineData(int.MaxValue, new[] { 0.6201007799387834, 0.8487186553865054, 0.7977885253197272, 0.0677063174196677, 0.45540022789662593 }, 0.36924873016366455)]
    public void TheLcgsDoubles_AreJavasNextDouble(int seed, double[] first, double tenThousandth)
    {
        var lcg = new LinearCongruentialGenerator(seed);

        double[] draws = Enumerable.Range(0, 10_000).Select(_ => lcg.NextDouble()).ToArray();

        draws.Take(5).Should().Equal(first);
        draws[9_999].Should().Be(tenThousandth);
    }

    [Theory]
    [InlineData(0, new[] { 1569741360, 1785505948, 516548029, 1302116447, 1368843515 }, 1824485501)]
    [InlineData(42, new[] { 1562431130, 117392763, 1467211248, 102948884, 662969970 }, 1436702535)]
    [InlineData(-7, new[] { 577934662, 1935951297, 1445645590, 2031187707, 1119890478 }, 1741110337)]
    public void TheLcgsIntegers_AreJavasNextIntShiftedToItsTop31Bits(int seed, int[] first, int tenThousandth)
    {
        // nextInt() >>> 1 is Java's next(31) for the same state; for seed 0 the first is
        // -1155484576 >>> 1 = 1569741360, the published new Random(0).nextInt()
        var lcg = new LinearCongruentialGenerator(seed);

        int[] draws = Enumerable.Range(0, 10_000).Select(_ => lcg.NextInteger()).ToArray();

        draws.Take(5).Should().Equal(first);
        draws[9_999].Should().Be(tenThousandth);
    }

    // ── SubtractiveCongruentialGenerator = Knuth's subtractive generator ──────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(-7)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void TheSubtractiveGenerator_IsKnuthsSubtractiveGenerator(int seed)
    {
        var doubles = new SubtractiveCongruentialGenerator(seed);
        var integers = new SubtractiveCongruentialGenerator(seed);
        var reference = new Knuth(seed);
        var referenceIntegers = new Knuth(seed);

        for (int i = 0; i < 10_000; i++)
        {
            doubles.NextDouble().Should().Be(reference.Next() * (1d / int.MaxValue), $"draw {i} of seed {seed}");
            integers.NextInteger().Should().Be(referenceIntegers.Next(), $"draw {i} of seed {seed}");
        }
    }

    [Fact]
    public void TheSubtractiveGenerator_SaysWhatItIs()
    {
        new SubtractiveCongruentialGenerator(1).Name.Should().Contain("subtractive").And.NotContain("Multiplicative");
    }

    /// <summary>
    /// Knuth's subtractive generator as .NET Framework published it (<c>System.Random</c>, reference
    /// source): 55 lagged words, the second lag 21 places behind, values in [0, int.MaxValue).
    /// </summary>
    private sealed class Knuth
    {
        private const int MBig = int.MaxValue, MSeed = 161803398;
        private readonly int[] seedArray = new int[56];
        private int inext, inextp;

        public Knuth(int seed)
        {
            int subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
            int mj = MSeed - subtraction, mk = 1;
            seedArray[55] = mj;
            for (int i = 1; i < 55; i++)
            {
                int ii = 21 * i % 55;
                seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBig;
                mj = seedArray[ii];
            }
            for (int k = 1; k < 5; k++)
                for (int i = 1; i < 56; i++)
                {
                    seedArray[i] -= seedArray[1 + (i + 30) % 55];
                    if (seedArray[i] < 0) seedArray[i] += MBig;
                }
            inext = 0;
            inextp = 21;
        }

        public int Next()
        {
            if (++inext >= 56) inext = 1;
            if (++inextp >= 56) inextp = 1;
            int value = seedArray[inext] - seedArray[inextp];
            if (value == MBig) value--;
            if (value < 0) value += MBig;
            seedArray[inext] = value;
            return value;
        }
    }

    // ── R250_521: no published vector, so a pin of this code ─────────────────────────────────

    [Fact]
    public void R250_521_IsPinnedToItsOwnOutput_AsARegressionCheckNotAReference()
    {
        // Printed by this code after the SIM-98 seeding fix. It proves nothing about correctness (the
        // uniformity checks below do that); it catches an accidental change, which would move every run
        // made with this generator (UN-033).
        var doubles = new R250_521(42);
        var integers = new R250_521(42);

        Enumerable.Range(0, 5).Select(_ => doubles.NextDouble()).Should().Equal(
            0.40915790549479425, 0.45860982104204595, 0.7943444678094238, 0.9295377577655017, 0.23099770117551088);
        Enumerable.Range(0, 5).Select(_ => integers.NextInteger()).Should().Equal(
            1757319823, 1969714183, 1264199863, 1844850622, 992127572);
    }

    // ── uniformity, all four generators ──────────────────────────────────────────────────────

    public static IEnumerable<object[]> Generators() => new[]
    {
        new object[] { "MersenneTwister" }, new object[] { "LinearCongruentialGenerator" },
        new object[] { "SubtractiveCongruentialGenerator" }, new object[] { "R250_521" },
    };

    private static IRandomSource Make(string name, int seed) => name switch
    {
        "MersenneTwister" => new MersenneTwister(seed),
        "LinearCongruentialGenerator" => new LinearCongruentialGenerator(seed),
        "SubtractiveCongruentialGenerator" => new SubtractiveCongruentialGenerator(seed),
        "R250_521" => new R250_521(seed),
        _ => throw new ArgumentException(name),
    };

    /// <summary>The 99.9 % quantiles of chi-square with 63 and 255 degrees of freedom.</summary>
    private const double Chi63 = 103.4, Chi255 = 330.5;

    [Theory]
    [MemberData(nameof(Generators))]
    public void TheFirstThousandDoubles_FallInTheUpperHalfAboutHalfTheTime(string name)
    {
        // 1,000 fair coin flips: 0.5 +- 0.05 is beyond 3 standard deviations
        foreach (int seed in new[] { 1, 42, 12345 })
        {
            IRandomSource source = Make(name, seed);
            double upper = Enumerable.Range(0, 1_000).Count(_ => source.NextDouble() >= 0.5) / 1_000d;
            upper.Should().BeInRange(0.45, 0.55, $"{name} seed {seed}");
        }
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void AMillionDoubles_FillSixtyFourEqualBinsEvenly(string name)
    {
        IRandomSource source = Make(name, 42);
        var bins = new long[64];
        const int n = 1_000_000;
        for (int i = 0; i < n; i++) bins[(int)(source.NextDouble() * 64)]++;

        double chi = bins.Sum(b => (b - n / 64d) * (b - n / 64d) / (n / 64d));

        chi.Should().BeLessThan(Chi63, $"{name}: bins {string.Join(", ", bins.Take(8))} ...");
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void AMillionSuccessivePairs_FillASixteenBySixteenGridEvenly(string name)
    {
        // pairs of consecutive draws: a generator whose next value leans on the last one fails this
        IRandomSource source = Make(name, 42);
        var cells = new long[256];
        const int n = 1_000_000;
        for (int i = 0; i < n; i++)
            cells[(int)(source.NextDouble() * 16) * 16 + (int)(source.NextDouble() * 16)]++;

        double chi = cells.Sum(c => (c - n / 256d) * (c - n / 256d) / (n / 256d));

        chi.Should().BeLessThan(Chi255, name);
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void EveryOneOfTheTopSixteenBitsOfADouble_IsSetAboutHalfTheTime(string name)
    {
        // a bit column stuck at 0 (an unseeded high bit) shows here even when the bins look tolerable
        IRandomSource source = Make(name, 42);
        var set = new int[16];
        const int n = 100_000;
        for (int i = 0; i < n; i++)
        {
            uint top = (uint)(source.NextDouble() * 65536d);
            for (int b = 0; b < 16; b++) if ((top & (1u << b)) != 0) set[b]++;
        }

        // 100,000 flips: 0.5 +- 0.006 is about 4 standard deviations
        set.Select(s => s / (double)n).Should().OnlyContain(share => share > 0.494 && share < 0.506,
            $"{name}: {string.Join(", ", set.Select(s => (s / (double)n).ToString("F3")))}");
    }
}
