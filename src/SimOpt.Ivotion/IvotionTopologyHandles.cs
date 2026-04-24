using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Templates;

namespace SimOpt.Ivotion;

/// <summary>
/// Mutable handle onto a built Ivotion simulation. Holds direct references to
/// all relevant entities so KPI extraction, stepping, and rendering (later)
/// can operate without re-traversing the model.
/// </summary>
public sealed class IvotionTopologyHandles
{
    public Model Model { get; }
    public IvotionSolution Solution { get; }
    public SimpleSource Source { get; }
    public SimpleSink ShippedSink { get; }
    public IReadOnlyList<RolandPrinter> Rolands { get; }
    public IReadOnlyList<SimpleServer> ManualStations { get; }
    public IReadOnlyList<SimpleBuffer> Buffers { get; }

    public double ArrivalIntervalMinutes { get; }
    public double EffectiveInspectTime { get; }
    public double EffectivePackTime { get; }
    public double EffectiveLabelTime { get; }
    public double EffectiveSsbTime { get; }

    public IvotionTopologyHandles(
        Model model,
        IvotionSolution solution,
        SimpleSource source,
        SimpleSink shippedSink,
        IReadOnlyList<RolandPrinter> rolands,
        IReadOnlyList<SimpleServer> manualStations,
        IReadOnlyList<SimpleBuffer> buffers,
        double arrivalIntervalMinutes,
        double effectiveInspectTime,
        double effectivePackTime,
        double effectiveLabelTime,
        double effectiveSsbTime)
    {
        Model = model;
        Solution = solution;
        Source = source;
        ShippedSink = shippedSink;
        Rolands = rolands;
        ManualStations = manualStations;
        Buffers = buffers;
        ArrivalIntervalMinutes = arrivalIntervalMinutes;
        EffectiveInspectTime = effectiveInspectTime;
        EffectivePackTime = effectivePackTime;
        EffectiveLabelTime = effectiveLabelTime;
        EffectiveSsbTime = effectiveSsbTime;
    }

    /// <summary>
    /// Run the model for the given number of simulated minutes.
    /// Starts the source if it hasn't already been started.
    /// </summary>
    public void RunFor(double minutes, double step = 0.1)
    {
        Source.Start();
        double endTime = Model.CurrentTime + minutes;
        int maxSteps = (int)(minutes / step) * 100 + 10_000; // sanity cap
        int steps = 0;
        while (Model.CurrentTime < endTime && steps < maxSteps)
        {
            Model.Step(step);
            steps++;
        }
    }
}
