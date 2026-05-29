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
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);

        var agentB = CreateMovingAgent("b", GridAction.Stay);
        sim.AddAgent(agentB.Object, 0, 0);

        sim.Tick();

        agentB.Verify(b => b.OnObserve(
            It.Is<AgentEvent>(e =>
                e.AgentId == "a" &&
                e.EventType == AgentEventType.Death &&
                e.Cause == "hazard")),
            Times.Once);
    }

    [Fact]
    public void DeadAgent_DoesNotReceiveEvents()
    {
        var grid = new Grid(5, 5);
        grid[2, 1] = CellType.Hazard;
        grid[0, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);
        var agentB = CreateMovingAgent("b", GridAction.North);
        sim.AddAgent(agentB.Object, 0, 2);

        sim.Tick();

        agentA.Verify(a => a.OnObserve(It.IsAny<AgentEvent>()), Times.Never);
        agentB.Verify(b => b.OnObserve(It.IsAny<AgentEvent>()), Times.Never);
    }

    [Fact]
    public void MultipleDeaths_SingleTick_AllBroadcast()
    {
        var grid = new Grid(10, 10);
        grid[2, 1] = CellType.Hazard;
        grid[4, 1] = CellType.Hazard;

        var sim = new GridSimulation(grid, new GridSimulationConfig());

        var agentA = CreateMovingAgent("a", GridAction.North);
        sim.AddAgent(agentA.Object, 2, 2);
        var agentB = CreateMovingAgent("b", GridAction.North);
        sim.AddAgent(agentB.Object, 4, 2);
        var observer = CreateMovingAgent("observer", GridAction.Stay);
        sim.AddAgent(observer.Object, 6, 6);

        sim.Tick();

        observer.Verify(o => o.OnObserve(
            It.Is<AgentEvent>(e => e.EventType == AgentEventType.Death)),
            Times.Exactly(2));
    }

    private static Mock<IGridAgent> CreateMovingAgent(string id, GridAction action)
    {
        var mock = new Mock<IGridAgent>();
        int x = 0, y = 0;
        bool alive = true;

        mock.SetupGet(a => a.Id).Returns(id);
        mock.SetupGet(a => a.X).Returns(() => x);
        mock.SetupGet(a => a.Y).Returns(() => y);
        mock.SetupGet(a => a.IsAlive).Returns(() => alive);
        mock.Setup(a => a.SelectAction(It.IsAny<GridObservation>())).Returns(action);
        mock.Setup(a => a.Reset(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((sx, sy) => { x = sx; y = sy; alive = true; });
        mock.Setup(a => a.OnDeath(It.IsAny<string>()))
            .Callback<string>(_ => alive = false);

        return mock;
    }
}
