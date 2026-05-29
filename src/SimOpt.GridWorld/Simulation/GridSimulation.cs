using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Simulation;

public class GridSimulation<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    private readonly List<IGridAgent<TCoord>> _agents = new();
    private readonly GridSimulationConfig _config;

    public Grid<TCoord> Grid { get; }
    public IReadOnlyList<IGridAgent<TCoord>> Agents => _agents;
    public int Step { get; private set; }

    public GridSimulation(Grid<TCoord> grid, GridSimulationConfig config)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void AddAgent(IGridAgent<TCoord> agent, TCoord position)
    {
        if (!Grid.InBounds(position))
            throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is out of bounds");
        agent.Reset(position);
        _agents.Add(agent);
    }

    public StepResult<TCoord> Tick()
    {
        Step++;
        var deaths = new List<AgentEvent<TCoord>>();
        var events = new List<AgentEvent<TCoord>>();

        var liveAgents = _agents.Where(a => a.IsAlive).ToList();

        var actions = new Dictionary<IGridAgent<TCoord>, int>();
        foreach (var agent in liveAgents)
        {
            var obs = BuildObservation(agent);
            actions[agent] = agent.SelectAction(obs);
        }

        foreach (var (agent, actionId) in actions)
        {
            var target = Grid.Topology.Step(agent.Position, actionId);

            if (!Grid.InBounds(target) || Grid[target] == CellType.Wall)
                continue;

            agent.MoveTo(target);
        }

        foreach (var agent in liveAgents)
        {
            if (!agent.IsAlive) continue;

            var cell = Grid[agent.Position];
            var cellInfo = Grid.GetCellInfo(agent.Position);
            double reward = _config.StepReward;

            if (cell == CellType.Hazard)
            {
                reward = _config.HazardReward;
                agent.OnStepComplete(BuildObservation(agent), reward);
                var cause = cellInfo.CausalMechanism ?? "hazard";
                agent.OnDeath(cause);
                var deathEvent = new AgentEvent<TCoord>(agent.Id, AgentEventType.Death,
                    agent.Position, cause, cellInfo);
                deaths.Add(deathEvent);
                events.Add(deathEvent);
            }
            else
            {
                if (cell == CellType.Resource)
                {
                    var collectEvent = new AgentEvent<TCoord>(agent.Id, AgentEventType.ResourceCollected,
                        agent.Position, CellInfo: cellInfo);
                    events.Add(collectEvent);
                    reward = _config.ResourceReward;
                }

                agent.OnStepComplete(BuildObservation(agent), reward);
            }
        }

        foreach (var evt in events)
        {
            foreach (var agent in _agents.Where(a => a.IsAlive && a.Id != evt.AgentId))
                agent.OnObserve(evt);
        }

        return new StepResult<TCoord> { StepNumber = Step, Deaths = deaths, Events = events };
    }

    public SimulationResult<TCoord> Run(int maxSteps)
    {
        var allDeaths = new List<AgentEvent<TCoord>>();

        for (int i = 0; i < maxSteps; i++)
        {
            if (_config.StopWhenAllDead && _agents.All(a => !a.IsAlive))
                break;

            var result = Tick();
            allDeaths.AddRange(result.Deaths);
        }

        return new SimulationResult<TCoord>
        {
            TotalSteps = Step,
            AllDeaths = allDeaths,
            AgentsAlive = _agents.Count(a => a.IsAlive),
            AgentsDead = _agents.Count(a => !a.IsAlive),
        };
    }

    private GridObservation<TCoord> BuildObservation(IGridAgent<TCoord> agent)
    {
        var others = _agents
            .Where(a => a.Id != agent.Id && a.IsAlive)
            .Select(a => (a.Id, a.Position, a.IsAlive));
        return GridObservation<TCoord>.FromGrid(Grid, agent.Position, _config.ViewRadius, others);
    }
}
