using System;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Enum;
using SimOpt.Simulation.Templates;
using SimOpt.Mathematics.Stochastics.Distributions;

namespace SimOpt.Visualization.Models;

/// <summary>
/// Single Queue Single Server demo model for visualization.
/// Source → Buffer (FIFO, cap 15) → Server → Sink
/// </summary>
public class SqssDemo
{
    private int _productCounter;
    private double _stepSize = 0.5;

    public Model Model { get; }
    public SimpleSource Source { get; private set; } = null!;
    public SimpleBuffer Queue { get; private set; } = null!;
    public SimpleServer Server { get; private set; } = null!;
    public SimpleSink Sink { get; private set; } = null!;
    public double EndTime { get; set; } = 200.0;

    public bool IsFinished => Model.CurrentTime >= EndTime;

    public SqssDemo(int seed = 42)
    {
        Model = new Model("SQSS", seed, DateTime.MinValue);
    }

    public void Build()
    {
        _productCounter = 0;

        // Source: Gaussian inter-arrival (mean=2.0, stddev=0.3)
        var interArrival = new GaussianDistribution(2.0, 0.3);
        Source = new SimpleSource(Model, interArrival, ProductGenerator, name: "Source");

        // Queue: FIFO buffer, capacity 15
        Queue = new SimpleBuffer(Model, QueueRule.FIFO, name: "Queue", maxCapacity: 15);

        // Server: constant processing time = 1.8
        var machiningTime = new ConstantDoubleDistribution(1.8, false);
        Server = new SimpleServer(Model, machiningTime, name: "Server");
        Server.AutoContinue = true;
        Server.ConnectTo(Queue);

        // Sink: counts finished items
        Sink = new SimpleSink(Model, name: "Sink");
        Sink.ConnectTo(Server);
    }

    /// <summary>
    /// Initialize and start the source. Call before stepping.
    /// </summary>
    public void StartSource()
    {
        Source.Start();
    }

    /// <summary>
    /// Advance simulation by one step for animated visualization.
    /// Returns false when simulation has reached EndTime.
    /// </summary>
    public bool Step()
    {
        if (IsFinished) return false;
        Model.Step(_stepSize);
        return !IsFinished;
    }

    private SimpleEntity ProductGenerator()
    {
        _productCounter++;
        return new SimpleEntity(Model, $"P{_productCounter}", $"Product {_productCounter}");
    }
}
