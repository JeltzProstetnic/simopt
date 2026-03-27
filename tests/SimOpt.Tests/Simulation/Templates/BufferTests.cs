using FluentAssertions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// Tests for Buffer&lt;T&gt; covering ordering rules, capacity, state
/// properties, Preview semantics, Count tracking, and Reset.
///
/// All tests use a real Model instance because Buffer&lt;T&gt;.Initialize()
/// accesses Model.Antithetic and Model.NonStochasticMode during construction.
/// </summary>
public class BufferTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Create a minimal Model suitable for buffer construction.
    /// Seed 42 is arbitrary; start time 0 is standard.
    /// </summary>
    private static Model CreateModel() => new Model("BufferTest", 42, 0.0);

    /// <summary>
    /// Build a typed Buffer&lt;string&gt; with the given queue rule and optional
    /// capacity limit.
    /// </summary>
    private static Buffer<string> CreateBuffer(
        Model model,
        QueueRule rule = QueueRule.FIFO,
        int maxCapacity = int.MaxValue)
        => new Buffer<string>(model, rule, maxCapacity: maxCapacity);

    // ---------------------------------------------------------------------------
    // FIFO ordering
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_FifoRule_GetReturnsFirstInsertedItem()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("alpha");
        buffer.Put("beta");
        buffer.Put("gamma");

        buffer.Get().Should().Be("alpha");
    }

    [Fact]
    public void Put_FifoRule_GetReturnsItemsInInsertionOrder()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("first");
        buffer.Put("second");
        buffer.Put("third");

        var results = new[] { buffer.Get(), buffer.Get(), buffer.Get() };

        results.Should().Equal("first", "second", "third");
    }

    // ---------------------------------------------------------------------------
    // LIFO ordering
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_LifoRule_GetReturnsLastInsertedItem()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.LIFO);

        buffer.Put("alpha");
        buffer.Put("beta");
        buffer.Put("gamma");

        buffer.Get().Should().Be("gamma");
    }

    [Fact]
    public void Put_LifoRule_GetReturnsItemsInReverseInsertionOrder()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.LIFO);

        buffer.Put("first");
        buffer.Put("second");
        buffer.Put("third");

        var results = new[] { buffer.Get(), buffer.Get(), buffer.Get() };

        results.Should().Equal("third", "second", "first");
    }

    // ---------------------------------------------------------------------------
    // Priority ordering
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_PriorityRule_GetReturnsLowestPriorityNumberFirst()
    {
        // The SortedDictionary sorts ascending by Priority.CompareTo, and
        // PrioritySelector returns .First().Value — so the item with the
        // lowest PriorityNumber is retrieved first (highest urgency).
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.Priority);

        buffer.Put("low-urgency",  new Priority(priority: 10.0));
        buffer.Put("high-urgency", new Priority(priority: 1.0));
        buffer.Put("mid-urgency",  new Priority(priority: 5.0));

        buffer.Get().Should().Be("high-urgency");
    }

    [Fact]
    public void Put_PriorityRule_GetDrainsInPriorityOrder()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.Priority);

        buffer.Put("p3", new Priority(priority: 3.0));
        buffer.Put("p1", new Priority(priority: 1.0));
        buffer.Put("p2", new Priority(priority: 2.0));

        var results = new[] { buffer.Get(), buffer.Get(), buffer.Get() };

        results.Should().Equal("p1", "p2", "p3");
    }

    // ---------------------------------------------------------------------------
    // Capacity enforcement
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_WhenNotFull_ReturnsTrue()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 3);

        var accepted = buffer.Put("item");

        accepted.Should().BeTrue();
    }

    [Fact]
    public void Put_WhenAtMaxCapacity_ReturnsFalse()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 2);

        buffer.Put("item-1");
        buffer.Put("item-2");

        var rejected = buffer.Put("item-3");

        rejected.Should().BeFalse();
    }

    [Fact]
    public void Put_WhenAtMaxCapacity_DoesNotIncreaseCount()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 2);

        buffer.Put("item-1");
        buffer.Put("item-2");
        buffer.Put("item-3"); // should be rejected

        buffer.Count.Should().Be(2);
    }

    [Fact]
    public void IsFull_WhenAtCapacity_ReturnsTrue()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 2);

        buffer.Put("a");
        buffer.Put("b");

        buffer.IsFull.Should().BeTrue();
    }

    [Fact]
    public void IsFull_WhenBelowCapacity_ReturnsFalse()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 5);

        buffer.Put("a");

        buffer.IsFull.Should().BeFalse();
    }

    [Fact]
    public void MaxCapacity_ReflectsConstructorValue()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 7);

        buffer.MaxCapacity.Should().Be(7);
    }

    [Fact]
    public void Put_AfterGetFreesSlot_AcceptsNewItem()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 1);

        buffer.Put("first");
        buffer.Get();                      // free the slot
        var accepted = buffer.Put("second");

        accepted.Should().BeTrue();
        buffer.Count.Should().Be(1);
    }

    // ---------------------------------------------------------------------------
    // Empty buffer behaviour
    // ---------------------------------------------------------------------------

    [Fact]
    public void IsEmpty_OnNewBuffer_ReturnsTrue()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_AfterPut_ReturnsFalse()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("item");

        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_AfterPutThenGet_ReturnsTrue()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("item");
        buffer.Get();

        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Get_OnEmptyBuffer_ReturnsDefault()
    {
        // Buffer<string>.Get on empty returns default(string) == null,
        // and logs a warning — no exception expected.
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        var result = buffer.Get();

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------------------
    // Preview — non-destructive peek
    // ---------------------------------------------------------------------------

    [Fact]
    public void Preview_ReturnsNextItem_WithoutRemoving()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("only-item");

        var peeked = buffer.Preview();

        peeked.Should().Be("only-item");
        buffer.Count.Should().Be(1);
    }

    [Fact]
    public void Preview_CalledRepeatedly_ReturnsSameItem()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("item-A");
        buffer.Put("item-B");

        var first  = buffer.Preview();
        var second = buffer.Preview();

        first.Should().Be(second);
    }

    [Fact]
    public void Preview_MatchesNextGet()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("x");
        buffer.Put("y");

        var peeked   = buffer.Preview();
        var retrieved = buffer.Get();

        peeked.Should().Be(retrieved);
    }

    [Fact]
    public void Preview_OnEmptyBuffer_ReturnsDefault()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        var result = buffer.Preview();

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------------------
    // Count tracking
    // ---------------------------------------------------------------------------

    [Fact]
    public void Count_StartsAtZero()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void Count_IncreasesOnEachPut()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("a");
        buffer.Count.Should().Be(1);

        buffer.Put("b");
        buffer.Count.Should().Be(2);

        buffer.Put("c");
        buffer.Count.Should().Be(3);
    }

    [Fact]
    public void Count_DecreasesOnGet()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("a");
        buffer.Put("b");
        buffer.Put("c");

        buffer.Get();
        buffer.Count.Should().Be(2);

        buffer.Get();
        buffer.Count.Should().Be(1);
    }

    [Fact]
    public void Count_UnchangedByPreview()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("item");
        _ = buffer.Preview();

        buffer.Count.Should().Be(1);
    }

    // ---------------------------------------------------------------------------
    // Reset
    // ---------------------------------------------------------------------------

    [Fact]
    public void Reset_ClearsAllItems()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("a");
        buffer.Put("b");
        buffer.Put("c");

        buffer.Reset();

        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void Reset_IsEmptyAfterReset()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("item");

        buffer.Reset();

        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Reset_IsNotFullAfterReset()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 1);

        buffer.Put("item");

        buffer.Reset();

        buffer.IsFull.Should().BeFalse();
    }

    [Fact]
    public void Reset_AllowsPutAfterReset()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 2);

        buffer.Put("a");
        buffer.Put("b");
        buffer.Reset();

        var accepted = buffer.Put("new-item");

        accepted.Should().BeTrue();
        buffer.Count.Should().Be(1);
    }

    [Fact]
    public void Reset_RestoresMaxCapacity()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 5);

        // Shrink runtime capacity then reset — should restore to 5.
        buffer.Put("x");
        buffer.MaxCapacity = 3;
        buffer.Reset();

        buffer.MaxCapacity.Should().Be(5);
    }

    // ---------------------------------------------------------------------------
    // Event callbacks — ItemReceivedEvent, BufferFullEvent, BufferEmptyEvent
    // ---------------------------------------------------------------------------

    [Fact]
    public void ItemReceivedEvent_FiresOnSuccessfulPut()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);
        string? receivedItem = null;

        buffer.ItemReceivedEvent.AddHandler((sender, item) => receivedItem = item);
        buffer.Put("trigger");

        receivedItem.Should().Be("trigger");
    }

    [Fact]
    public void ItemReceivedEvent_DoesNotFireOnRejectedPut()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 1);
        int callCount = 0;

        buffer.ItemReceivedEvent.AddHandler((_, _) => callCount++);
        buffer.Put("accepted");
        buffer.Put("rejected"); // buffer is full

        callCount.Should().Be(1);
    }

    [Fact]
    public void BufferFullEvent_FiresWhenCapacityReached()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO, maxCapacity: 2);
        bool fullFired = false;

        buffer.BufferFullEvent.AddHandler(_ => fullFired = true);
        buffer.Put("a");
        buffer.Put("b"); // fills the buffer

        fullFired.Should().BeTrue();
    }

    [Fact]
    public void BufferEmptyEvent_FiresWhenLastItemIsRemoved()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);
        bool emptyFired = false;

        buffer.BufferEmptyEvent.AddHandler(_ => emptyFired = true);
        buffer.Put("sole-item");
        buffer.Get();

        emptyFired.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // Indexed Put overloads — Put(item, int id) and Put(item, string id)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_WithIntId_ItemRetrievableByIntId()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("tagged", 99);

        buffer.Preview(99).Should().Be("tagged");
    }

    [Fact]
    public void Put_WithStringId_ItemRetrievableByStringId()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("labelled", "my-key");

        buffer.Preview("my-key").Should().Be("labelled");
    }

    [Fact]
    public void Get_WithStringId_RemovesAndReturnsCorrectItem()
    {
        var model = CreateModel();
        var buffer = CreateBuffer(model, QueueRule.FIFO);

        buffer.Put("one", "id-1");
        buffer.Put("two", "id-2");

        var result = buffer.Get("id-1");

        result.Should().Be("one");
        buffer.Count.Should().Be(1);
    }
}
