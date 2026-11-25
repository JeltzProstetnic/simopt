using System.Collections.Generic;
using System.Linq;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Statistics;
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

    /// <summary>The model time the handles were made at; KPIs are taken from here on.</summary>
    public double StartTime { get; }

    /// <summary>
    /// SIM-93: the measured busy share of each manual station, in the order of
    /// <see cref="ManualStations"/>. A station is one server whose speed scales with its operators, so
    /// all of its operators are busy exactly when it is.
    /// </summary>
    public IReadOnlyList<TimeWeightedCollector> StationUtilization { get; }

    private readonly List<double> shipTimes = new();

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
        StartTime = model.CurrentTime;
        StationUtilization = manualStations.Select(s => Instrumentation.ObserveUtilization(s)).ToArray();
        shippedSink.ItemReceived.AddHandler((_, _) => shipTimes.Add(model.CurrentTime));
    }

    /// <summary>SIM-93: the batches that reached the sink at or after <paramref name="time"/>.</summary>
    public int ShippedSince(double time) => shipTimes.Count(t => t >= time);

    /// <summary>
    /// Run the model for the given number of simulated minutes, ending exactly at the requested time.
    /// Starts the source if it hasn't already been started.
    /// </summary>
    /// <returns>
    /// SIM-93: false when the step cap stopped the run before the requested time; the KPIs of such a
    /// run say so (<see cref="IvotionKpis.Completed"/>).
    /// </returns>
    public bool RunFor(double minutes, double step = 0.1, int? maxSteps = null)
    {
        Source.Start();
        double endTime = Model.CurrentTime + minutes;
        int cap = maxSteps ?? (int)(minutes / step) * 100 + 10_000; // sanity cap
        int steps = 0;
        while (Model.CurrentTime < endTime && steps < cap)
        {
            Model.Run(System.Math.Min(Model.CurrentTime + step, endTime));   // the last step lands on the end
            steps++;
        }
        return Model.CurrentTime >= endTime;
    }
}
