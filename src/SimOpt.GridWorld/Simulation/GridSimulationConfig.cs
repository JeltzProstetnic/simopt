namespace SimOpt.GridWorld.Simulation;

public class GridSimulationConfig
{
    public int ViewRadius { get; set; } = 2;
    public bool StopWhenAllDead { get; set; } = true;
    public double HazardReward { get; set; } = -10.0;
    public double ResourceReward { get; set; } = 1.0;
    public double StepReward { get; set; } = -0.01;
}
