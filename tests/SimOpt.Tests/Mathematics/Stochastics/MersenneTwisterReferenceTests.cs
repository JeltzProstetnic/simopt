using System;
using System.Linq;
using FluentAssertions;
using SimOpt.Mathematics.Stochastics.RandomSources;
using Xunit;

namespace SimOpt.Tests.Mathematics.Stochastics;

/// <summary>
/// SIM-81 — <see cref="MersenneTwister"/> against the published MT19937 reference.
///
/// <para>
/// The class claimed to be the Matsumoto–Nishimura generator and was not. It omitted the tempering
/// transform entirely, and it filled its 624-word state from <c>System.Random.Next()</c> — which
/// returns values in <c>[0, int.MaxValue)</c>, so the high bit was never set — while starting its
/// index at 0 rather than at 624. The twist therefore did not run until the 625th draw, which means
/// <b>the first 624 outputs were literally System.Random's, not the Mersenne Twister's</b>, and none
/// of them had bit 31 set.
/// </para>
///
/// <para>
/// The fix is the reference initialisation plus the reference tempering, so the only test that
/// settles the matter is agreement with the published output vector. These values were produced by
/// an independent implementation of MT19937 written from the algorithm rather than from this code,
/// and the seed-5489 sequence is the one every MT19937 implementation is checked against.
/// </para>
///
/// <para>
/// <b>This change moves every random stream in the product.</b> That was the reason SIM-81 waited
/// for the SIM-64 analytic battery: the battery is what distinguishes "the numbers are different"
/// from "the numbers are worse", and its containment gate is set at 99.9% precisely so that
/// re-rolling every stream does not turn a correct engine red.
/// </para>
/// </summary>
public class MersenneTwisterReferenceTests
{
    [Fact]
    public void TheFirstOutputs_MatchThePublishedReferenceVector()
    {
        var rnd = new MersenneTwister(5489);

        uint[] actual = Enumerable.Range(0, 10).Select(_ => rnd.NextUInt()).ToArray();

        // The canonical MT19937 sequence for the reference default seed.
        actual.Should().Equal(
            3499211612u, 581869302u, 3890346734u, 3586334585u, 545404204u,
            4161255391u, 3922919429u, 949333985u, 2715962298u, 1323567403u);
    }

    [Theory]
    [InlineData(1, new uint[] { 1791095845u, 4282876139u, 3093770124u, 4005303368u, 491263u })]
    [InlineData(42, new uint[] { 1608637542u, 3421126067u, 4083286876u, 787846414u, 3143890026u })]
    public void OtherSeeds_AlsoMatchTheReference(int seed, uint[] expected)
    {
        // One seed could agree by coincidence of a shared bug; three cannot. Seed 1 in particular
        // exercises the initialisation recurrence from a state where most words start at zero.
        var rnd = new MersenneTwister(seed);

        Enumerable.Range(0, expected.Length).Select(_ => rnd.NextUInt())
            .Should().Equal(expected);
    }

    [Fact]
    public void TheTwistHappensAtTheRightDraw()
    {
        // Draws 624 and beyond come from the second twist of the state. The old implementation
        // started its index at 0 instead of 624, so its twist boundary sat one full block late and
        // every draw after it was displaced. Straddling the boundary is what catches an
        // off-by-one-block error that the first ten draws cannot see.
        var rnd = new MersenneTwister(5489);
        uint[] all = Enumerable.Range(0, 627).Select(_ => rnd.NextUInt()).ToArray();

        all.Skip(622).Take(5).Should().Equal(
            2227348307u, 4020325887u, 4178893912u, 610818241u, 2787397224u);
    }

    [Fact]
    public void TheHighBitIsSetInAboutHalfTheDraws_IncludingTheVeryFirstBlock()
    {
        // The precise signature of the old defect: bit 31 was set in exactly 0 of the first 624
        // draws, because System.Random.Next() cannot set it. The reference generator sets it 302
        // times in that block. This is the assertion that would have caught the defect the whole
        // time it shipped.
        var rnd = new MersenneTwister(5489);

        int withHighBit = Enumerable.Range(0, 624).Count(_ => (rnd.NextUInt() & 0x80000000u) != 0);

        withHighBit.Should().Be(302, "the reference generator's first block has exactly this many");
    }

    [Fact]
    public void TheFirstBlockIsNotSystemRandomsOutput()
    {
        // Stated as its own test because it is the claim in the class's name. A generator whose
        // first 624 outputs are another generator's is that other generator, whatever the file is
        // called — and 624 draws is more than most simulations of any size ever take from a single
        // stream.
        const int seed = 20_260_826;
        var rnd = new MersenneTwister(seed);
        var systemRandom = new Random(seed);

        int matches = Enumerable.Range(0, 624).Count(_ => rnd.NextUInt() == (uint)systemRandom.Next());

        matches.Should().Be(0);
    }

    [Fact]
    public void TheSameSeedStillReproducesBitwise()
    {
        // The point of the change is the quality of the stream, not its reproducibility, which was
        // never in doubt and must survive untouched (UN-009, UN-033).
        static uint[] Draw()
        {
            var rnd = new MersenneTwister(777);
            var values = new uint[2_000];
            for (int i = 0; i < values.Length; i++) values[i] = rnd.NextUInt();
            return values;
        }

        Draw().Should().Equal(Draw());
    }

    [Fact]
    public void TheDocumentedIntegerAndDoubleContractsStillHold()
    {
        // The mapping onto [0, int.MaxValue) and [0, 1) is SIM-56's work and is unchanged — but it
        // now runs on a source that genuinely sets all 32 bits, which is the regime it was written
        // for and never previously saw in the first block.
        var rnd = new MersenneTwister(5489);

        for (int i = 0; i < 100_000; i++)
        {
            int n = rnd.NextInteger();
            n.Should().BeInRange(0, int.MaxValue - 1);
        }

        var fresh = new MersenneTwister(5489);
        double[] doubles = Enumerable.Range(0, 100_000).Select(_ => fresh.NextDouble()).ToArray();

        doubles.Should().OnlyContain(d => d >= 0d && d < 1d);
        doubles.Average().Should().BeApproximately(0.5, 0.01);
    }
}
