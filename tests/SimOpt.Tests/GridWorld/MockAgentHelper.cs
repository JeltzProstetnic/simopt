using Moq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;

namespace SimOpt.Tests.GridWorld;

internal static class MockAgentHelper
{
    public static Mock<IGridAgent<Coord2D>> Create2D(string id, int action)
    {
        var mock = new Mock<IGridAgent<Coord2D>>();
        var pos = new Coord2D(0, 0);
        bool alive = true;

        mock.SetupGet(a => a.Id).Returns(id);
        mock.SetupGet(a => a.Position).Returns(() => pos);
        mock.SetupGet(a => a.IsAlive).Returns(() => alive);
        mock.Setup(a => a.SelectAction(It.IsAny<GridObservation<Coord2D>>())).Returns(action);
        mock.Setup(a => a.MoveTo(It.IsAny<Coord2D>()))
            .Callback<Coord2D>(p => pos = p);
        mock.Setup(a => a.Reset(It.IsAny<Coord2D>()))
            .Callback<Coord2D>(p => { pos = p; alive = true; });
        mock.Setup(a => a.OnDeath(It.IsAny<string>()))
            .Callback<string>(_ => alive = false);

        return mock;
    }
}
