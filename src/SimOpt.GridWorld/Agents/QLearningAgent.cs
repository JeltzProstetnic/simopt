using System;
using System.Collections.Generic;
using System.Linq;

namespace SimOpt.GridWorld.Agents;

public class QLearningAgent : IGridAgent
{
    private static readonly GridAction[] AllActions =
        (GridAction[])Enum.GetValues(typeof(GridAction));

    private readonly Random _rng;
    private readonly double _epsilon;
    private readonly double _learningRate;
    private readonly double _discount;
    private readonly Dictionary<(int State, GridAction Action), double> _qTable = new();

    private int _lastState;
    private GridAction _lastAction;

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsAlive { get; private set; }

    public QLearningAgent(string id, int seed, double epsilon = 0.1,
        double learningRate = 0.1, double discount = 0.95)
    {
        Id = id;
        _rng = new Random(seed);
        _epsilon = epsilon;
        _learningRate = learningRate;
        _discount = discount;
    }

    public GridAction SelectAction(GridObservation observation)
    {
        _lastState = ObservationToState(observation);

        if (_rng.NextDouble() < _epsilon)
        {
            _lastAction = AllActions[_rng.Next(AllActions.Length)];
        }
        else
        {
            _lastAction = AllActions
                .OrderByDescending(a => GetQValue(_lastState, a))
                .First();
        }

        return _lastAction;
    }

    public void OnStepComplete(GridObservation newObservation, double reward)
    {
        int newState = ObservationToState(newObservation);
        double maxNextQ = AllActions.Max(a => GetQValue(newState, a));
        double currentQ = GetQValue(_lastState, _lastAction);
        double newQ = currentQ + _learningRate * (reward + _discount * maxNextQ - currentQ);
        _qTable[(_lastState, _lastAction)] = newQ;
    }

    public void OnDeath(string cause) => IsAlive = false;

    public void OnObserve(AgentEvent agentEvent) { }

    public void Reset(int startX, int startY)
    {
        X = startX;
        Y = startY;
        IsAlive = true;
    }

    public double GetQValue(int state, GridAction action) =>
        _qTable.TryGetValue((state, action), out var q) ? q : 0.0;

    public int ObservationToState(GridObservation obs)
    {
        int hash = 17;
        int w = obs.LocalView.GetLength(0);
        int h = obs.LocalView.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                hash = hash * 31 + (int)obs.LocalView[x, y];
        return hash;
    }
}
