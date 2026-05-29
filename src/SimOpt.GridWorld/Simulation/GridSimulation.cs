using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Agents;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Simulation;

public class GridSimulation
{
    private readonly List<IGridAgent> _agents = new();
    private readonly GridSimulationConfig _config;

    public Grid Grid { get; }
    public IReadOnlyList<IGridAgent> Agents => _agents;
    public int Step { get; private set; }

    public GridSimulation(Grid grid, GridSimulationConfig config)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void AddAgent(IGridAgent agent, int x, int y)
    {
        if (!Grid.InBounds(x, y))
            throw new ArgumentOutOfRangeException($"Position ({x},{y}) is out of bounds");
        agent.Reset(x, y);
        _agents.Add(agent);
    }

    public StepResult Tick()
    {
        Step++;
        var deaths = new List<AgentEvent>();
        var events = new List<AgentEvent>();

        var liveAgents = _agents.Where(a => a.IsAlive).ToList();

        var actions = new Dictionary<IGridAgent, GridAction>();
        foreach (var agent in liveAgents)
        {
            var obs = BuildObservation(agent);
            actions[agent] = agent.SelectAction(obs);
        }

        foreach (var (agent, action) in actions)
        {
            var (dx, dy) = ActionToDelta(action);
            int nx = agent.X + dx;
            int ny = agent.Y + dy;

            if (!Grid.InBounds(nx, ny) || Grid[nx, ny] == CellType.Wall)
                continue;

            agent.Reset(nx, ny);
        }

        foreach (var agent in liveAgents)
        {
            if (!agent.IsAlive) continue;

            var cell = Grid[agent.X, agent.Y];
            double reward = _config.StepReward;

            if (cell == CellType.Hazard)
            {
                agent.OnDeath("hazard");
                var deathEvent = new AgentEvent(agent.Id, AgentEventType.Death, agent.X, agent.Y, "hazard");
                deaths.Add(deathEvent);
                events.Add(deathEvent);
                reward = _config.HazardReward;
            }
            else if (cell == CellType.Resource)
            {
                var collectEvent = new AgentEvent(agent.Id, AgentEventType.ResourceCollected, agent.X, agent.Y);
                events.Add(collectEvent);
                reward = _config.ResourceReward;
            }

            agent.OnStepComplete(BuildObservation(agent), reward);
        }

        foreach (var evt in events)
        {
            foreach (var agent in _agents.Where(a => a.IsAlive && a.Id != evt.AgentId))
                agent.OnObserve(evt);
        }

        return new StepResult { StepNumber = Step, Deaths = deaths, Events = events };
    }

    public SimulationResult Run(int maxSteps)
    {
        var allDeaths = new List<AgentEvent>();

        for (int i = 0; i < maxSteps; i++)
        {
            if (_config.StopWhenAllDead && _agents.All(a => !a.IsAlive))
                break;

            var result = Tick();
            allDeaths.AddRange(result.Deaths);
        }

        return new SimulationResult
        {
            TotalSteps = Step,
            AllDeaths = allDeaths,
            AgentsAlive = _agents.Count(a => a.IsAlive),
            AgentsDead = _agents.Count(a => !a.IsAlive),
        };
    }

    private GridObservation BuildObservation(IGridAgent agent)
    {
        var others = _agents
            .Where(a => a.Id != agent.Id && a.IsAlive)
            .Select(a => (a.Id, a.X, a.Y, a.IsAlive));
        return GridObservation.FromGrid(Grid, agent.X, agent.Y, _config.ViewRadius, others);
    }

    private static (int dx, int dy) ActionToDelta(GridAction action) => action switch
    {
        GridAction.North => (0, -1),
        GridAction.South => (0, 1),
        GridAction.East => (1, 0),
        GridAction.West => (-1, 0),
        GridAction.Stay => (0, 0),
        _ => (0, 0),
    };
}
