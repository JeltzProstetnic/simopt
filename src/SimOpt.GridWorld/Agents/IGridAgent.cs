using System;

namespace SimOpt.GridWorld.Agents;

public interface IGridAgent<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    string Id { get; }
    TCoord Position { get; }
    bool IsAlive { get; }

    int SelectAction(GridObservation<TCoord> observation);
    void MoveTo(TCoord position);
    void OnDeath(string cause);
    void OnObserve(AgentEvent<TCoord> agentEvent);
    void OnStepComplete(GridObservation<TCoord> newObservation, double reward);
    void Reset(TCoord startPosition);
}
