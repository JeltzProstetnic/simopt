using System;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public enum AgentEventType
{
    Death,
    ResourceCollected,
    Moved
}

public record AgentEvent<TCoord>(
    string AgentId,
    AgentEventType EventType,
    TCoord Position,
    string? Cause = null,
    CellInfo? CellInfo = null) where TCoord : struct, IEquatable<TCoord>;
