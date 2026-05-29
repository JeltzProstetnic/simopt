using System;

namespace SimOpt.GridWorld.Agents;

public class RandomAgent : IGridAgent
{
    private static readonly GridAction[] AllActions =
        (GridAction[])Enum.GetValues(typeof(GridAction));

    private readonly Random _rng;

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsAlive { get; private set; }

    public RandomAgent(string id, int seed)
    {
        Id = id;
        _rng = new Random(seed);
    }

    public GridAction SelectAction(GridObservation observation) =>
        AllActions[_rng.Next(AllActions.Length)];

    public void OnDeath(string cause) => IsAlive = false;
    public void OnObserve(AgentEvent agentEvent) { }
    public void OnStepComplete(GridObservation newObservation, double reward) { }

    public void Reset(int startX, int startY)
    {
        X = startX;
        Y = startY;
        IsAlive = true;
    }
}
