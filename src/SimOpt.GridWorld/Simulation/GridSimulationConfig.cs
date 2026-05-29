namespace SimOpt.GridWorld.Simulation;

public class GridSimulationConfig
{
    public int ViewRadius { get; init; } = 2;
    public bool StopWhenAllDead { get; init; } = true;
    public double HazardReward { get; init; } = -10.0;
    public double ResourceReward { get; init; } = 1.0;
    public double StepReward { get; init; } = -0.01;
}
