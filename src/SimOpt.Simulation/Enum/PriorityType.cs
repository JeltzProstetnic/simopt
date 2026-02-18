namespace SimOpt.Simulation.Enum
{
    /// <summary>
    /// general types of priority
    /// order will always be the following:
    /// [LowLevelBeforeOthers][SimWorldBeforeOthers][User][SimWorldAfterOthers][LowLevelAfterOthers]
    /// </summary>
    public enum PriorityType
    {
        LowLevelBeforeOthers,
        SimWorldBeforeOthers,
        User,
        SimWorldAfterOthers,
        LowLevelAfterOthers
    }
}