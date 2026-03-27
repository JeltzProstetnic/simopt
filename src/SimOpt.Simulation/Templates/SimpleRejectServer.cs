using System;
using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Mathematics.Stochastics.Interfaces;

namespace SimOpt.Simulation.Templates;

/// <summary>
/// Server that probabilistically rejects items to a waste sink.
/// Handles all output routing internally — do NOT connect downstream via standard Connect() patterns.
/// Instead, use <see cref="SetRejectTarget"/> and <see cref="AddPassTarget(SimpleBuffer)"/>.
/// </summary>
[Serializable]
public class SimpleRejectServer : SimpleServer
{
    private readonly Random _rng;
    private readonly double _rejectRate;
    [NonSerialized] private readonly List<Action<SimpleEntity>> _passTargets = new();
    [NonSerialized] private Action<SimpleEntity>? _rejectAction;

    public double RejectRate => _rejectRate;

    public SimpleRejectServer(IModel model, IDistribution<double> machiningTime,
        double rejectRate, string name = "", int seed = 42)
        : base(model, machiningTime, name: name, createProduct: m => m[0])
    {
        _rejectRate = rejectRate;
        _rng = new Random(seed);
        AutoContinue = true;

        EntityCreatedEvent.AddHandler((sender, entity) =>
        {
            if (_rejectAction != null && _rng.NextDouble() < _rejectRate)
                _rejectAction(entity);
            else
                foreach (var target in _passTargets)
                    target(entity);
        }, new Priority(type: PriorityType.SimWorldBeforeOthers));
    }

    public void SetRejectTarget(SimpleSink sink) => _rejectAction = e => sink.Put(e);
    public void AddPassTarget(SimpleBuffer buffer) => _passTargets.Add(e => buffer.Put(e));
    public void AddPassTarget(SimpleSink sink) => _passTargets.Add(e => sink.Put(e));
}
