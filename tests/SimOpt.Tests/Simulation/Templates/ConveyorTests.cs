using FluentAssertions;
using SimOpt.Basics.Datastructures.Geometry;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// Tests for Conveyor&lt;T&gt; where T : class, IAttachable.
/// SimpleEntity (from SimOpt.Simulation.Entities) implements IAttachable and is used
/// as the item type throughout. Tests use a real Model instance (not mocked) because
/// the Conveyor constructor creates a MovableEntity internally that registers event
/// handlers on the model's scheduler — the Model concrete type satisfies IModel.
/// </summary>
public class ConveyorTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Model CreateModel() => new Model("ConveyorTestModel", 42, 0.0);

    private static Conveyor<SimpleEntity> CreateConveyor(
        Model model,
        double length = 100.0,
        int numberOfSections = 10,
        double firstSectionOffset = 0.0,
        double vMax = 1.0)
        => new Conveyor<SimpleEntity>(
            model,
            id: "conv1",
            name: "Conveyor1",
            orientation: new Vector(1, 0),
            length: length,
            numberOfSections: numberOfSections,
            firstSectionOffset: firstSectionOffset,
            vMax: vMax,
            acceleration: 1.0,
            deceleration: 1.0,
            initialPosition: new Point(0, 0));

    private static SimpleEntity CreateItem(Model model, string id = "item1")
        => new SimpleEntity(model, id, id);

    // ---------------------------------------------------------------------------
    // Construction & initial state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithModel_SetsLength()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, length: 50.0);

        conveyor.Length.Should().Be(50.0);
    }

    [Fact]
    public void Constructor_WithModel_SetsNumberOfSections()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 8);

        conveyor.NumberOfSections.Should().Be(8);
    }

    [Fact]
    public void Constructor_WithModel_SetsFirstSectionOffset()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, firstSectionOffset: 5.0);

        conveyor.FirstSectionOffset.Should().Be(5.0);
    }

    [Fact]
    public void Constructor_WithModel_OrientationIsNormalized()
    {
        var model = CreateModel();
        // Provide a non-unit vector; Conveyor normalizes it via Vector.AsNormalized.
        var conveyor = new Conveyor<SimpleEntity>(
            model,
            id: "convNorm",
            name: "ConveyorNorm",
            orientation: new Vector(3, 4)); // length = 5, normalized = (0.6, 0.8)

        // The normalized vector has length 1.
        double length = Math.Sqrt(conveyor.Orientation.X * conveyor.Orientation.X
                                + conveyor.Orientation.Y * conveyor.Orientation.Y);
        length.Should().BeApproximately(1.0, precision: 1e-9);
    }

    [Fact]
    public void Constructor_WithModel_InitialCountIsZero()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithModel_IsNotRunning()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithModel_IsNotFull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        conveyor.IsFull.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithModel_HasNoItemAtFirstPosition()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.HasItemAtFirstPosition.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithModel_HasNoItemAtLastPosition()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.HasItemAtLastPosition.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Parameterless_SetsDefaultOrientation()
    {
        // The parameterless constructor is valid and sets orientation to (1,0).
        var conveyor = new Conveyor<SimpleEntity>();

        conveyor.Orientation.X.Should().BeApproximately(1.0, precision: 1e-9);
        conveyor.Orientation.Y.Should().BeApproximately(0.0, precision: 1e-9);
    }

    // ---------------------------------------------------------------------------
    // SectionLength calculation
    // ---------------------------------------------------------------------------

    [Fact]
    public void SectionLength_IsLengthDividedByNumberOfSections()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, length: 100.0, numberOfSections: 10);

        conveyor.SectionLength.Should().BeApproximately(10.0, precision: 1e-9);
    }

    [Fact]
    public void SectionLength_NonEvenDivision_IsCorrect()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, length: 30.0, numberOfSections: 4);

        conveyor.SectionLength.Should().BeApproximately(7.5, precision: 1e-9);
    }

    // ---------------------------------------------------------------------------
    // Put — basic item placement
    // ---------------------------------------------------------------------------

    [Fact]
    public void Put_SingleItem_IncrementsCount()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        conveyor.Put(item);

        conveyor.Count.Should().Be(1);
    }

    [Fact]
    public void Put_SingleItem_ReturnsTrue()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        bool result = conveyor.Put(item);

        result.Should().BeTrue();
    }

    [Fact]
    public void Put_ItemAtIndexZero_ItemDetectedAtFirstPosition()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        // Put(T) delegates to Put(T, 0) which places item at index 0.
        conveyor.Put(item);

        conveyor.HasItemAtFirstPosition.Should().BeTrue();
    }

    [Fact]
    public void Put_ItemAtIndexZero_HasItemAt_ReturnsTrue()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        conveyor.Put(item);

        conveyor.HasItemAt(0).Should().BeTrue();
    }

    [Fact]
    public void Put_ItemAtIndexZero_GetItemAt_ReturnsSameItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        conveyor.Put(item);

        conveyor.GetItemAt(0).Should().BeSameAs(item);
    }

    [Fact]
    public void Put_SamePositionTwice_SecondCallReturnsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item1 = CreateItem(model, "item1");
        var item2 = CreateItem(model, "item2");

        conveyor.Put(item1, 0);
        bool second = conveyor.Put(item2, 0);

        second.Should().BeFalse();
    }

    [Fact]
    public void Put_SamePositionTwice_CountRemainsOne()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item1 = CreateItem(model, "item1");
        var item2 = CreateItem(model, "item2");

        conveyor.Put(item1, 0);
        conveyor.Put(item2, 0);

        conveyor.Count.Should().Be(1);
    }

    [Fact]
    public void Put_WithExplicitIndex_PlacesItemAtCorrectPosition()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);

        conveyor.Put(item, 3);

        conveyor.HasItemAt(3).Should().BeTrue();
        conveyor.GetItemAt(3).Should().BeSameAs(item);
    }

    [Fact]
    public void Put_WithExplicitIndex_OtherPositionsEmpty()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);

        conveyor.Put(item, 3);

        conveyor.HasItemAt(0).Should().BeFalse();
        conveyor.HasItemAt(1).Should().BeFalse();
        conveyor.HasItemAt(4).Should().BeFalse();
    }

    [Fact]
    public void Put_IndexExceedsNumberOfSections_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);

        // index > numberOfSections (6 > 5) should throw.
        Action act = () => conveyor.Put(item, 6);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // Get
    // ---------------------------------------------------------------------------

    [Fact]
    public void Get_FromPopulatedConveyor_ReturnsItem()
    {
        var model = CreateModel();
        // Default Get() fetches from index (NumberOfSections - 1).
        var conveyor = CreateConveyor(model, numberOfSections: 4);
        var item = CreateItem(model);
        conveyor.Put(item, 3); // index 3 == NumberOfSections - 1

        var result = conveyor.Get();

        result.Should().BeSameAs(item);
    }

    [Fact]
    public void Get_RemovesItemFromConveyor()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 4);
        var item = CreateItem(model);
        conveyor.Put(item, 3);

        conveyor.Get();

        conveyor.Count.Should().Be(0);
        conveyor.HasItemAt(3).Should().BeFalse();
    }

    [Fact]
    public void Get_ByIndex_ReturnsCorrectItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        conveyor.Put(item, 2);

        var result = conveyor.Get(2);

        result.Should().BeSameAs(item);
    }

    [Fact]
    public void Get_EmptyIndex_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        Action act = () => conveyor.Get(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // Preview
    // ---------------------------------------------------------------------------

    [Fact]
    public void Preview_ByIndex_ReturnsItemWithoutRemoving()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        conveyor.Put(item, 2);

        var result = conveyor.Preview(2);

        result.Should().BeSameAs(item);
        conveyor.Count.Should().Be(1); // item still on conveyor
    }

    [Fact]
    public void Preview_EmptyIndex_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        Action act = () => conveyor.Preview(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // GetItemAt / HasItemAt
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetItemAt_EmptyPosition_ReturnsNull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        var result = conveyor.GetItemAt(0);

        result.Should().BeNull();
    }

    [Fact]
    public void HasItemAt_EmptyConveyor_ReturnsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.HasItemAt(5).Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Capacity / IsFull
    // ---------------------------------------------------------------------------

    [Fact]
    public void IsFull_WhenAllSectionsOccupied_ReturnsTrue()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 3);
        for (int i = 0; i < 3; i++)
        {
            var item = CreateItem(model, $"item{i}");
            conveyor.Put(item, i);
        }

        conveyor.IsFull.Should().BeTrue();
    }

    [Fact]
    public void IsFull_WhenOneSectionFree_ReturnsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 3);
        conveyor.Put(CreateItem(model, "item0"), 0);
        conveyor.Put(CreateItem(model, "item1"), 1);
        // section 2 is free

        conveyor.IsFull.Should().BeFalse();
    }

    [Fact]
    public void Count_AfterMultiplePuts_IsCorrect()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        conveyor.Put(CreateItem(model, "a"), 0);
        conveyor.Put(CreateItem(model, "b"), 2);
        conveyor.Put(CreateItem(model, "c"), 4);

        conveyor.Count.Should().Be(3);
    }

    // ---------------------------------------------------------------------------
    // HasItemAtFirstPosition / HasItemAtLastPosition
    // ---------------------------------------------------------------------------

    [Fact]
    public void HasItemAtFirstPosition_AfterPutAtIndexZero_ReturnsTrue()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        conveyor.Put(CreateItem(model), 0);

        conveyor.HasItemAtFirstPosition.Should().BeTrue();
    }

    [Fact]
    public void HasItemAtFirstPosition_ItemNotAtZero_ReturnsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        conveyor.Put(CreateItem(model), 3);

        conveyor.HasItemAtFirstPosition.Should().BeFalse();
    }

    [Fact]
    public void HasItemAtLastPosition_AfterPutAtLastIndex_ReturnsTrue()
    {
        var model = CreateModel();
        // HasItemAtLastPosition checks index == NumberOfSections (not NumberOfSections-1).
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        // Put accepts index up to NumberOfSections (inclusive per guard: index > NumberOfSections throws).
        conveyor.Put(CreateItem(model), 5);

        conveyor.HasItemAtLastPosition.Should().BeTrue();
    }

    [Fact]
    public void HasItemAtLastPosition_EmptyConveyor_ReturnsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        conveyor.HasItemAtLastPosition.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // GetFirstItem / GetLastItem
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFirstItem_SingleItem_ReturnsThatItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        conveyor.Put(item, 3);

        conveyor.GetFirstItem().Should().BeSameAs(item);
    }

    [Fact]
    public void GetLastItem_SingleItem_ReturnsThatItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        conveyor.Put(item, 3);

        conveyor.GetLastItem().Should().BeSameAs(item);
    }

    [Fact]
    public void GetFirstItem_MultipleItems_ReturnsLowestIndex()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item0 = CreateItem(model, "item0");
        var item3 = CreateItem(model, "item3");
        conveyor.Put(item0, 0);
        conveyor.Put(item3, 3);

        conveyor.GetFirstItem().Should().BeSameAs(item0);
    }

    [Fact]
    public void GetLastItem_MultipleItems_ReturnsHighestIndex()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item0 = CreateItem(model, "item0");
        var item3 = CreateItem(model, "item3");
        conveyor.Put(item0, 0);
        conveyor.Put(item3, 3);

        conveyor.GetLastItem().Should().BeSameAs(item3);
    }

    [Fact]
    public void GetFirstItem_EmptyConveyor_ReturnsNull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        conveyor.GetFirstItem().Should().BeNull();
    }

    [Fact]
    public void GetLastItem_EmptyConveyor_ReturnsNull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);

        conveyor.GetLastItem().Should().BeNull();
    }

    // ---------------------------------------------------------------------------
    // GetItemAtFirstPosition / GetItemAtLastPosition
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetItemAtFirstPosition_ItemAtIndexZero_ReturnsItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        conveyor.Put(item, 0);

        conveyor.GetItemAtFirstPosition().Should().BeSameAs(item);
    }

    [Fact]
    public void GetItemAtFirstPosition_NoItemAtZero_ReturnsNull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        conveyor.Put(CreateItem(model), 2);

        conveyor.GetItemAtFirstPosition().Should().BeNull();
    }

    [Fact]
    public void GetItemAtLastPosition_ItemAtLastIndex_ReturnsItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        var item = CreateItem(model);
        // Last position checks index == NumberOfSections (= 5 here).
        conveyor.Put(item, 5);

        conveyor.GetItemAtLastPosition().Should().BeSameAs(item);
    }

    [Fact]
    public void GetItemAtLastPosition_NoItemAtLastIndex_ReturnsNull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5);
        conveyor.Put(CreateItem(model), 2);

        conveyor.GetItemAtLastPosition().Should().BeNull();
    }

    // ---------------------------------------------------------------------------
    // AutoDetach
    // ---------------------------------------------------------------------------

    [Fact]
    public void AutoDetach_DefaultValue_IsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.AutoDetach.Should().BeFalse();
    }

    [Fact]
    public void AutoDetach_WhenSetToTrue_ReflectsChange()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.AutoDetach = true;

        conveyor.AutoDetach.Should().BeTrue();
    }

    [Fact]
    public void AutoDetach_WhenSetToFalse_ReflectsChange()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        conveyor.AutoDetach = true;

        conveyor.AutoDetach = false;

        conveyor.AutoDetach.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // ContainedObjects
    // ---------------------------------------------------------------------------

    [Fact]
    public void ContainedObjects_EmptyConveyor_IsEmpty()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.ContainedObjects.Should().BeEmpty();
    }

    [Fact]
    public void ContainedObjects_AfterPut_ContainsItem()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        var item = CreateItem(model);

        conveyor.Put(item);

        conveyor.ContainedObjects.Should().Contain(item);
    }

    [Fact]
    public void ContainedObjects_AfterPutAndGet_IsEmpty()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 4);
        var item = CreateItem(model);
        conveyor.Put(item, 3);

        conveyor.Get(3);

        conveyor.ContainedObjects.Should().BeEmpty();
    }

    // ---------------------------------------------------------------------------
    // IsRunning — initial state
    // ---------------------------------------------------------------------------

    [Fact]
    public void IsRunning_AfterConstruction_IsFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.IsRunning.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // Advance(int) — index-based movement validation
    // ---------------------------------------------------------------------------

    [Fact]
    public void Advance_ZeroSections_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.Advance(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Advance_NegativeSections_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.Advance(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // Recede(int) — index-based movement validation
    // ---------------------------------------------------------------------------

    [Fact]
    public void Recede_ZeroSections_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.Recede(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Recede_NegativeSections_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.Recede(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // AdvanceRepeatedly / RecedeRepeatedly — argument validation
    // ---------------------------------------------------------------------------

    [Fact]
    public void AdvanceRepeatedly_ZeroRepetitions_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.AdvanceRepeatedly(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecedeRepeatedly_ZeroRepetitions_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 5, vMax: 100.0);

        Action act = () => conveyor.RecedeRepeatedly(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------------------
    // Reset
    // ---------------------------------------------------------------------------

    [Fact]
    public void Reset_AfterPuttingItems_ClearsCount()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        conveyor.Put(CreateItem(model, "a"), 0);
        conveyor.Put(CreateItem(model, "b"), 1);

        conveyor.Reset();

        conveyor.Count.Should().Be(0);
    }

    [Fact]
    public void Reset_AfterPuttingItems_ConveyorIsNotFull()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model, numberOfSections: 2);
        conveyor.Put(CreateItem(model, "a"), 0);
        conveyor.Put(CreateItem(model, "b"), 1);

        conveyor.Reset();

        conveyor.IsFull.Should().BeFalse();
    }

    [Fact]
    public void Reset_SetsIsRunningToFalse()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        conveyor.Reset();

        conveyor.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsHasItemAtFirstPosition()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);
        conveyor.Put(CreateItem(model), 0);

        conveyor.Reset();

        conveyor.HasItemAtFirstPosition.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // IsInitialized
    // ---------------------------------------------------------------------------

    [Fact]
    public void IsInitialized_AfterFullConstructor_IsTrue()
    {
        var model = CreateModel();
        var conveyor = CreateConveyor(model);

        // The full constructor sets up a MovableEntity (mobileElement != null),
        // satisfying the IsInitialized override in Conveyor<T>.
        conveyor.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void IsInitialized_AfterParameterlessConstructor_IsFalse()
    {
        // Parameterless ctor leaves mobileElement null, so IsInitialized is false.
        var conveyor = new Conveyor<SimpleEntity>();

        conveyor.IsInitialized.Should().BeFalse();
    }
}
