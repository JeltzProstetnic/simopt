using System;

namespace SimOpt.Ivotion;

/// <summary>
/// Demo cost-model constants. US-typical dental production.
/// Displayed in the optimization UI for user override.
/// </summary>
public static class IvotionCostModel
{
    /// <summary>Amortized Roland LEF capital cost ($30k ÷ 5yr ÷ 2000hr).</summary>
    public const double RolandCapitalPerHour = 3.0;

    /// <summary>Fully-loaded US skilled production operator wage.</summary>
    public const double OperatorWagePerHour = 32.0;
}

/// <summary>
/// KPI bundle extracted from an Ivotion simulation run. All monetary values in USD. Time basis is
/// "per simulated hour".
/// </summary>
/// <remarks>
/// SIM-93: throughput and busy labour are measured by the run; total cost, paid labour and floor space
/// are computed from the configuration (headcount, printers, footprints) and do not depend on the run.
/// </remarks>
/// <param name="ThroughputPerHour">Pieces shipped per simulated hour after the warm-up, measured.</param>
/// <param name="TotalCostPerHour">Capital plus paid labour, from the configuration.</param>
/// <param name="LaborHoursPerSimHour">Paid labour hours per hour: the headcount, from the configuration.</param>
/// <param name="FloorSpaceM2">From the configuration's footprints.</param>
/// <param name="CostPerPiece">Total cost per hour over measured throughput.</param>
/// <param name="BusyLaborHoursPerSimHour">Hours operators actually worked per hour: each station's measured utilization times its operators.</param>
/// <param name="SimulatedMinutes">The minutes the run actually covered.</param>
/// <param name="Completed">False when the run stopped before the requested duration.</param>
public readonly record struct IvotionKpis(
    double ThroughputPerHour,
    double TotalCostPerHour,
    double LaborHoursPerSimHour,
    double FloorSpaceM2,
    double CostPerPiece,
    double BusyLaborHoursPerSimHour = double.NaN,
    double SimulatedMinutes = double.NaN,
    bool Completed = true)
{
    // Per-node footprints (metres²) — baseline approximate values matching the
    // IvotionPacking viz preset. Widths × heights from VizTopology.IvotionPacking().
    private const double SourceFootprint = 4.0 * 4.0;   // 16
    private const double SinkFootprint = 4.0 * 4.0;     // 16
    private const double BufferFootprint = 2.5 * 3.0;   // 7.5
    private const double ManualStationFootprint = 5.0 * 4.0; // 20
    private const double RolandFootprint = 6.0 * 5.0;   // 30
    private const int BufferCount = 5;                  // buf1..buf5
    private const int ManualStationCount = 4;           // inspect, pack, label, ssb

    /// <summary>
    /// Extracts the KPIs of a run of <paramref name="simDurationMinutes"/>. The operator wage can be
    /// overridden for UI-driven sensitivity analysis without touching the <see cref="IvotionCostModel"/>
    /// const; a warm-up leaves out what shipped before it (SIM-93).
    /// </summary>
    public static IvotionKpis Extract(
        IvotionTopologyHandles handles,
        double simDurationMinutes,
        double operatorWagePerHour = IvotionCostModel.OperatorWagePerHour,
        double warmupMinutes = 0d)
    {
        ArgumentNullException.ThrowIfNull(handles);
        if (simDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(simDurationMinutes),
                "Simulation duration must be positive.");
        if (operatorWagePerHour < 0)
            throw new ArgumentOutOfRangeException(nameof(operatorWagePerHour),
                "Operator wage must be non-negative.");

        var sol = handles.Solution;
        // SIM-93: the time the run actually covered, not the time requested
        double simulatedMinutes = handles.Model.CurrentTime - handles.StartTime;
        bool completed = simulatedMinutes >= simDurationMinutes;
        if (warmupMinutes < 0 || (warmupMinutes > 0 && warmupMinutes >= simulatedMinutes))
            throw new ArgumentOutOfRangeException(nameof(warmupMinutes),
                $"The warm-up must be at least 0 and shorter than the {simulatedMinutes} minutes simulated.");
        double measuredHours = Math.Max(0d, simulatedMinutes - warmupMinutes) / 60.0;

        int totalOperators = sol.OperatorsInspect + sol.OperatorsPack
                           + sol.OperatorsLabel + sol.OperatorsSsb;

        double capitalPerHour = IvotionCostModel.RolandCapitalPerHour * sol.RolandCount;
        double laborCostPerHour = operatorWagePerHour * totalOperators;
        double totalCostPerHour = capitalPerHour + laborCostPerHour;

        double piecesPerHour = measuredHours > 0
            ? handles.ShippedSince(handles.StartTime + warmupMinutes) * sol.RolandBatchSize / measuredHours
            : 0d;   // nothing simulated, nothing measured: cost per piece is unbounded

        int[] operators = { sol.OperatorsInspect, sol.OperatorsPack, sol.OperatorsLabel, sol.OperatorsSsb };
        double now = handles.Model.CurrentTime;
        double busyLabour = 0;
        for (int i = 0; i < handles.StationUtilization.Count; i++)
            if (now > handles.StartTime)
                busyLabour += handles.StationUtilization[i].TimeAverage(now) * operators[i];

        double floorSpace = SourceFootprint + SinkFootprint
                          + BufferCount * BufferFootprint
                          + ManualStationCount * ManualStationFootprint
                          + sol.RolandCount * RolandFootprint;

        double costPerPiece = piecesPerHour > 0
            ? totalCostPerHour / piecesPerHour
            : double.MaxValue;

        return new IvotionKpis(
            ThroughputPerHour: piecesPerHour,
            TotalCostPerHour: totalCostPerHour,
            LaborHoursPerSimHour: totalOperators,
            FloorSpaceM2: floorSpace,
            CostPerPiece: costPerPiece,
            BusyLaborHoursPerSimHour: busyLabour,
            SimulatedMinutes: simulatedMinutes,
            Completed: completed);
    }
}
