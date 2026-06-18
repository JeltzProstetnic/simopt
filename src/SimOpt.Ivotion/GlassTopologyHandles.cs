using System.Collections.Generic;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Templates;

namespace SimOpt.Glass;

/// <summary>
/// Mutable handle onto a built glass production-line simulation. Holds direct
/// references to all relevant entities so KPI extraction, stepping, and
/// rendering (later) can operate without re-traversing the model.
/// </summary>
public sealed class GlassTopologyHandles
{
    public Model Model { get; }
    public GlassSolution Solution { get; }
    public SimpleSource Source { get; }
    public SimpleSink DoneSink { get; }
    public IReadOnlyList<SimpleServer> Mixers { get; }
    public IReadOnlyList<SimpleServer> ProcessingLines { get; }
    public SimpleServer Quality { get; }
    public SimpleServer Packing { get; }
    public IReadOnlyList<SimpleBuffer> Buffers { get; }

    public double ArrivalIntervalMinutes { get; }
    public double EffectiveMixTime { get; }
    public double EffectiveProcessTime { get; }
    public double EffectiveQualityTime { get; }
    public double EffectivePackingTime { get; }

    public GlassTopologyHandles(
        Model model,
        GlassSolution solution,
        SimpleSource source,
        SimpleSink doneSink,
        IReadOnlyList<SimpleServer> mixers,
        IReadOnlyList<SimpleServer> processingLines,
        SimpleServer quality,
        SimpleServer packing,
        IReadOnlyList<SimpleBuffer> buffers,
        double arrivalIntervalMinutes,
        double effectiveMixTime,
        double effectiveProcessTime,
        double effectiveQualityTime,
        double effectivePackingTime)
    {
        Model = model;
        Solution = solution;
        Source = source;
        DoneSink = doneSink;
        Mixers = mixers;
        ProcessingLines = processingLines;
        Quality = quality;
        Packing = packing;
        Buffers = buffers;
        ArrivalIntervalMinutes = arrivalIntervalMinutes;
        EffectiveMixTime = effectiveMixTime;
        EffectiveProcessTime = effectiveProcessTime;
        EffectiveQualityTime = effectiveQualityTime;
        EffectivePackingTime = effectivePackingTime;
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
