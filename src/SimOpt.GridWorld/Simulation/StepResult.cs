using System.Collections.Generic;
using SimOpt.GridWorld.Agents;

namespace SimOpt.GridWorld.Simulation;

public class StepResult
{
    public int StepNumber { get; init; }
    public IReadOnlyList<AgentEvent> Deaths { get; init; } = [];
    public IReadOnlyList<AgentEvent> Events { get; init; } = [];
}
