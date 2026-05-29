using FluentAssertions;
using Moq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class CellInfoTests
{
    [Fact]
    public void SetCell_WithCellInfo_StoresMetadata()
    {
        var grid = new Grid2D(5, 5);
        var lava = CellInfo.HazardCell(HazardFamilies.Lava, CausalMechanisms.Thermal);

        grid.SetCell(new Coord2D(2, 2), lava);

        grid[2, 2].Should().Be(CellType.Hazard);
        var info = grid.GetCellInfo(new Coord2D(2, 2));
        info.HazardFamily.Should().Be("lava");
        info.CausalMechanism.Should().Be("thermal");
    }

    [Fact]
    public void GetCellInfo_WithoutSetCell_ReturnsCellTypeOnly()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 2] = CellType.Hazard;

        var info = grid.GetCellInfo(new Coord2D(2, 2));
        info.Type.Should().Be(CellType.Hazard);
        info.HazardFamily.Should().BeNull();
        info.CausalMechanism.Should().BeNull();
    }

    [Fact]
    public void DeathEvent_CarriesCellInfo()
    {
        var grid = new Grid2D(5, 5);
        grid.SetCell(new Coord2D(2, 1), CellInfo.HazardCell(HazardFamilies.Lava, CausalMechanisms.Thermal));

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = MockAgentHelper.Create2D("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 1));

        var result = sim.Tick();

        result.Deaths.Should().ContainSingle();
        var death = result.Deaths[0];
        death.Cause.Should().Be("thermal");
        death.CellInfo.Should().NotBeNull();
        death.CellInfo!.HazardFamily.Should().Be("lava");
    }

    [Fact]
    public void ResourceCollection_EmitsEvent()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 2] = CellType.Resource;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = MockAgentHelper.Create2D("r1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        var result = sim.Tick();

        result.Events.Should().Contain(e => e.EventType == AgentEventType.ResourceCollected);
    }

    [Fact]
    public void DeadAgent_DoesNotReceiveStepComplete()
    {
        var grid = new Grid2D(5, 5);
        grid.SetCell(new Coord2D(2, 2), CellInfo.HazardCell(HazardFamilies.Cliff, CausalMechanisms.Fall));

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());
        var agent = MockAgentHelper.Create2D("a1", Actions.Rect.Stay);
        sim.AddAgent(agent.Object, new Coord2D(2, 2));

        sim.Tick();

        agent.Object.IsAlive.Should().BeFalse();
        agent.Verify(a => a.OnStepComplete(It.IsAny<GridObservation<Coord2D>>(), -10.0), Times.Once);
    }

    [Fact]
    public void DifferentHazardTypes_DifferentCauses()
    {
        var grid = new Grid2D(5, 5);
        grid.SetCell(new Coord2D(1, 0), CellInfo.HazardCell(HazardFamilies.Lava, CausalMechanisms.Thermal));
        grid.SetCell(new Coord2D(3, 0), CellInfo.HazardCell(HazardFamilies.DeepWater, CausalMechanisms.Submersion));

        var info1 = grid.GetCellInfo(new Coord2D(1, 0));
        var info2 = grid.GetCellInfo(new Coord2D(3, 0));

        info1.CausalMechanism.Should().NotBe(info2.CausalMechanism);
        info1.HazardFamily.Should().NotBe(info2.HazardFamily);
    }

    [Fact]
    public void GetOrDefault_OutOfBounds_ReturnsCustomDefault()
    {
        var grid = new Grid2D(5, 5);
        grid.GetOrDefault(new Coord2D(10, 10), CellType.Resource).Should().Be(CellType.Resource);
    }
}
