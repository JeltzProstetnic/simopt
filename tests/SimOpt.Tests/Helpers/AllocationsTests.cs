using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace SimOpt.Tests.Helpers;

/// <summary>
/// SIM-138: the zero-allocation tests measured one window after one warm-up call and saw stray
/// bytes now and then (3,680 and 7,432), about one full suite run in three once the suite grew.
/// The runtime does one-off work on the calling thread, such as promoting a method to its optimised
/// tier, at a moment no test controls. <see cref="Allocations.Least"/> measures several windows and
/// reports the smallest: code that allocates does so in every window, so a real allocation still
/// shows, while one-off runtime work lands in one window only.
/// </summary>
public class AllocationsTests
{
    [Fact]
    public void AnActionThatAllocatesEveryTime_IsSeenInEveryWindow()
    {
        var kept = new List<object>();

        long bytes = Allocations.Least(() => kept.Add(new byte[64]));

        bytes.Should().BeGreaterThan(64);
    }

    [Fact]
    public void AnActionThatAllocatesNothing_Measures0()
    {
        int sum = 0;

        long bytes = Allocations.Least(() => { for (int i = 0; i < 1000; i++) sum += i; });

        bytes.Should().Be(0);
        sum.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnAllocationInOneWindowOnly_IsNotReported()
    {
        int calls = 0;
        var kept = new List<object>();

        long bytes = Allocations.Least(() => { if (++calls == 3) kept.Add(new byte[4096]); });

        bytes.Should().Be(0, "the third call is in the second of five windows, and the others allocate nothing");
    }
}
