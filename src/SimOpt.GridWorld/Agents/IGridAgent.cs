namespace SimOpt.GridWorld.Agents;

public interface IGridAgent
{
    string Id { get; }
    int X { get; }
    int Y { get; }
    bool IsAlive { get; }

    GridAction SelectAction(GridObservation observation);
    void OnDeath(string cause);
    void OnObserve(AgentEvent agentEvent);
    void OnStepComplete(GridObservation newObservation, double reward);
    void Reset(int startX, int startY);
}
