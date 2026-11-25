using FluentAssertions;
using SimOpt.Optimization;
using Xunit;

namespace SimOpt.Tests.Optimization;

/// <summary>
/// SIM-59, SIM-68 D8: the generator a solution candidate carries for its moves. Its whole state is one
/// 64-bit number, so a copy of a candidate moves exactly as the candidate would.
/// </summary>
public class SplitMix64Tests
{
    [Fact]
    public void FromZero_GivesTheReferenceSequence()
    {
        // Vigna's reference splitmix64.c from state 0
        var g = new SplitMix64(0);

        g.Next().Should().Be(0xE220A8397B1DCDAFUL);
        g.Next().Should().Be(0x6E789E6AA1B965F4UL);
    }

    [Fact]
    public void ACopy_ContinuesTheSameSequence()
    {
        var g = new SplitMix64(42);
        g.Next();
        SplitMix64 copy = g;

        copy.Next().Should().Be(g.Next());
    }

    [Fact]
    public void Split_GivesANewSequence_AndMovesThisOneOn()
    {
        var g = new SplitMix64(42);
        SplitMix64 same = g;

        SplitMix64 child = g.Split();

        child.Next().Should().NotBe(g.Next());
        g.Should().NotBe(same);
    }

    [Fact]
    public void Uniform_IsInTheUnitInterval_AndBelow_IsInRange()
    {
        var g = new SplitMix64(9);
        for (int i = 0; i < 1000; i++)
        {
            g.Uniform().Should().BeInRange(0d, 1d - 1e-17);
            g.Below(3).Should().BeInRange(0, 2);
        }
    }
}
