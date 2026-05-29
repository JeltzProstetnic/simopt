using System;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public class RandomAgent<TCoord> : IGridAgent<TCoord>
    where TCoord : struct, IEquatable<TCoord>
{
    private readonly Random _rng;
    private readonly int _actionCount;

    public string Id { get; }
    public TCoord Position { get; private set; }
    public bool IsAlive { get; private set; }

    public RandomAgent(string id, ITopology<TCoord> topology, int seed)
    {
        Id = id;
        _actionCount = topology.ActionCount;
        _rng = new Random(seed);
    }

    public int SelectAction(GridObservation<TCoord> observation) =>
        _rng.Next(_actionCount);

    public void MoveTo(TCoord position) => Position = position;
    public void OnDeath(string cause) => IsAlive = false;
    public void OnObserve(AgentEvent<TCoord> agentEvent) { }
    public void OnStepComplete(GridObservation<TCoord> newObservation, double reward) { }

    public void Reset(TCoord startPosition)
    {
        Position = startPosition;
        IsAlive = true;
    }
}
