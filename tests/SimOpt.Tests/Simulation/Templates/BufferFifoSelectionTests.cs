using System;
using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using SimOpt.Tests.Helpers;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// SIM-129: the FIFO rule found the next item by scanning every queued id through LINQ, so each
/// Get cost time in proportion to the queue and allocated an iterator. An overloaded configuration,
/// which an optimiser does explore, grows its queue for the whole run and became quadratic. The
/// rule itself is "the smallest integer id goes first" (ids count up with each Put unless the
/// caller gives them), and the tests pin that against out-of-order ids and removals by id or name.
/// </summary>
public class BufferFifoSelectionTests
{
    private static Buffer<string> CreateBuffer(Model model)
        => new Buffer<string>(model, QueueRule.FIFO, maxCapacity: int.MaxValue);

    private static List<string> Drain(Buffer<string> buffer)
    {
        var taken = new List<string>();
        while (buffer.Count > 0) taken.Add(buffer.Get());
        return taken;
    }

    [Fact]
    public void TheSmallestIdGoesFirst_EvenWhenIdsArePutOutOfOrder()
    {
        var buffer = CreateBuffer(new Model("m", 1, 0d));
        buffer.Put("five", 5, "s5");
        buffer.Put("two", 2, "s2");
        buffer.Put("nine", 9, "s9");
        buffer.Put("one", 1, "s1");

        buffer.Preview().Should().Be("one");
        Drain(buffer).Should().Equal("one", "two", "five", "nine");
    }

    [Fact]
    public void ItemsTakenByIdOrByName_AreSkipped()
    {
        var buffer = CreateBuffer(new Model("m", 1, 0d));
        for (int i = 0; i < 6; i++) buffer.Put("item" + i, i, "name" + i);

        buffer.Get(0).Should().Be("item0");
        buffer.Get("name2").Should().Be("item2");
        buffer.Get(1).Should().Be("item1");

        Drain(buffer).Should().Equal("item3", "item4", "item5");
    }

    [Fact]
    public void AfterAResetTheOrderStartsAgain()
    {
        var buffer = CreateBuffer(new Model("m", 1, 0d));
        buffer.Put("a");
        buffer.Put("b");
        buffer.Reset();
        buffer.Put("c");
        buffer.Put("d");

        Drain(buffer).Should().Equal("c", "d");
    }

    [Fact]
    public void ManyPutsUnderAnotherSelector_ThenFifoAgain_StillTakesTheSmallestId()
    {
        var buffer = CreateBuffer(new Model("m", 1, 0d));
        Func<string> fifo = buffer.ItemSelector;
        int last = -1;
        buffer.ItemSelector = () => buffer[last];   // newest first
        for (int i = 0; i < 1000; i++)
        {
            last = i;
            buffer.Put("x" + i, i, "x" + i);
            buffer.Get().Should().Be("x" + i);
        }
        buffer.Put("p", 1000, "p");
        buffer.Put("q", 1001, "q");
        buffer.ItemSelector = fifo;

        Drain(buffer).Should().Equal("p", "q");
    }

    [Fact]
    public void AFifoGet_AllocatesNothing_HoweverLongTheQueue()
    {
        var buffer = CreateBuffer(new Model("m", 1, 0d));
        for (int i = 0; i < 5000; i++) buffer.Put("w" + i, i, "w" + i);
        buffer.Get();

        // SIM-138: the fewest bytes over five windows of 500 Gets, after one warm-up window
        long bytes = Allocations.Least(() => { for (int i = 0; i < 500; i++) buffer.Get(); });

        bytes.Should().Be(0, "the FIFO rule must not scan the queue on every Get");
        buffer.Preview().Should().Be("w3001");
    }
}
