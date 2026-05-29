using FluentAssertions;
using Moq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;
using SimOpt.GridWorld.Simulation;
using Xunit;

namespace SimOpt.Tests.GridWorld;

public class DeathObservationTests
{
    [Fact]
    public void WhenAgentDies_OtherAgentsReceiveDeathEvent()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());

        var agentA = CreateMockAgent("a", Actions.Rect.North);
        sim.AddAgent(agentA.Object, new Coord2D(2, 2));

        var agentB = CreateMockAgent("b", Actions.Rect.Stay);
        sim.AddAgent(agentB.Object, new Coord2D(0, 0));

        sim.Tick();

        agentB.Verify(b => b.OnObserve(
            It.Is<AgentEvent<Coord2D>>(e =>
                e.AgentId == "a" &&
                e.EventType == AgentEventType.Death &&
                e.Cause == "hazard")),
            Times.Once);
    }

    [Fact]
    public void DeadAgent_DoesNotReceiveEvents()
    {
        var grid = new Grid2D(5, 5);
        grid[2, 1] = CellType.Hazard;
        grid[0, 1] = CellType.Hazard;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());

        var agentA = CreateMockAgent("a", Actions.Rect.North);
        sim.AddAgent(agentA.Object, new Coord2D(2, 2));
        var agentB = CreateMockAgent("b", Actions.Rect.North);
        sim.AddAgent(agentB.Object, new Coord2D(0, 2));

        sim.Tick();

        agentA.Verify(a => a.OnObserve(It.IsAny<AgentEvent<Coord2D>>()), Times.Never);
        agentB.Verify(b => b.OnObserve(It.IsAny<AgentEvent<Coord2D>>()), Times.Never);
    }

    [Fact]
    public void MultipleDeaths_SingleTick_AllBroadcast()
    {
        var grid = new Grid2D(10, 10);
        grid[2, 1] = CellType.Hazard;
        grid[4, 1] = CellType.Hazard;

        var sim = new GridSimulation<Coord2D>(grid, new GridSimulationConfig());

        var agentA = CreateMockAgent("a", Actions.Rect.North);
        sim.AddAgent(agentA.Object, new Coord2D(2, 2));
        var agentB = CreateMockAgent("b", Actions.Rect.North);
        sim.AddAgent(agentB.Object, new Coord2D(4, 2));
        var observer = CreateMockAgent("observer", Actions.Rect.Stay);
        sim.AddAgent(observer.Object, new Coord2D(6, 6));

        sim.Tick();

        observer.Verify(o => o.OnObserve(
            It.Is<AgentEvent<Coord2D>>(e => e.EventType == AgentEventType.Death)),
            Times.Exactly(2));
    }

    private static Mock<IGridAgent<Coord2D>> CreateMockAgent(string id, int action) =>
        MockAgentHelper.Create2D(id, action);
}
