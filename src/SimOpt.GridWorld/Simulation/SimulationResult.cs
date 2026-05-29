using System;
using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class SimulationResult<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    public int TotalSteps { get; init; }
    public IReadOnlyList<AgentEvent<TCoord>> AllDeaths { get; init; } = [];
    public int AgentsAlive { get; init; }
    public int AgentsDead { get; init; }
}
