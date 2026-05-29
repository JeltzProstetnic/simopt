using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class SimulationResult
{
    public int TotalSteps { get; init; }
    public IReadOnlyList<AgentEvent> AllDeaths { get; init; } = [];
    public int AgentsAlive { get; init; }
    public int AgentsDead { get; init; }
}
