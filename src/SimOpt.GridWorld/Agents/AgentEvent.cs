namespace SimOpt.GridWorld.Agents;

public enum AgentEventType
{
    Death,
    ResourceCollected,
    Moved
}

public record AgentEvent(
    string AgentId,
    AgentEventType EventType,
    int X,
    int Y,
    string? Cause = null);
