using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public class QLearningAgent<TCoord> : IGridAgent<TCoord>
    where TCoord : struct, IEquatable<TCoord>
{
    private readonly Random _rng;
    private readonly int _actionCount;
    private readonly double _epsilon;
    private readonly double _learningRate;
    private readonly double _discount;
    private readonly Dictionary<(int State, int Action), double> _qTable = new();

    private int _lastState;
    private int _lastAction;

    public string Id { get; }
    public TCoord Position { get; private set; }
    public bool IsAlive { get; private set; }

    public QLearningAgent(string id, ITopology<TCoord> topology, int seed,
        double epsilon = 0.1, double learningRate = 0.1, double discount = 0.95)
    {
        Id = id;
        _actionCount = topology.ActionCount;
        _rng = new Random(seed);
        _epsilon = epsilon;
        _learningRate = learningRate;
        _discount = discount;
    }

    public int SelectAction(GridObservation<TCoord> observation)
    {
        _lastState = ObservationToState(observation);

        if (_rng.NextDouble() < _epsilon)
        {
            _lastAction = _rng.Next(_actionCount);
        }
        else
        {
            _lastAction = Enumerable.Range(0, _actionCount)
                .OrderByDescending(a => GetQValue(_lastState, a))
                .First();
        }

        return _lastAction;
    }

    public void OnStepComplete(GridObservation<TCoord> newObservation, double reward)
    {
        int newState = ObservationToState(newObservation);
        double maxNextQ = Enumerable.Range(0, _actionCount).Max(a => GetQValue(newState, a));
        double currentQ = GetQValue(_lastState, _lastAction);
        double newQ = currentQ + _learningRate * (reward + _discount * maxNextQ - currentQ);
        _qTable[(_lastState, _lastAction)] = newQ;
    }

    public void OnDeath(string cause) => IsAlive = false;
    public void OnObserve(AgentEvent<TCoord> agentEvent) { }

    public void Reset(TCoord startPosition)
    {
        Position = startPosition;
        IsAlive = true;
    }

    public double GetQValue(int state, int action) =>
        _qTable.TryGetValue((state, action), out var q) ? q : 0.0;

    public int ObservationToState(GridObservation<TCoord> obs)
    {
        int hash = 17;
        foreach (var kvp in obs.Cells.OrderBy(k => k.Key.GetHashCode()))
            hash = hash * 31 + (int)kvp.Value;
        return hash;
    }
}
