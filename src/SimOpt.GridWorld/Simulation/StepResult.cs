using System;
using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class StepResult<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    public int StepNumber { get; init; }
    public IReadOnlyList<AgentEvent<TCoord>> Deaths { get; init; } = [];
    public IReadOnlyList<AgentEvent<TCoord>> Events { get; init; } = [];
}
