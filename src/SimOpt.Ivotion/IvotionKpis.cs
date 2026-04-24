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
/// KPI bundle extracted from a completed Ivotion simulation run.
/// All monetary values in USD. Time basis is "per simulated hour".
/// </summary>
public readonly record struct IvotionKpis(
    double ThroughputPerHour,
    double TotalCostPerHour,
    double LaborHoursPerSimHour,
    double FloorSpaceM2,
    double CostPerPiece)
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

    public static IvotionKpis Extract(IvotionTopologyHandles handles, double simDurationMinutes)
    {
        ArgumentNullException.ThrowIfNull(handles);
        if (simDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(simDurationMinutes),
                "Simulation duration must be positive.");

        var sol = handles.Solution;
        double simHours = simDurationMinutes / 60.0;

        int totalOperators = sol.OperatorsInspect + sol.OperatorsPack
                           + sol.OperatorsLabel + sol.OperatorsSsb;

        double capitalPerHour = IvotionCostModel.RolandCapitalPerHour * sol.RolandCount;
        double laborCostPerHour = IvotionCostModel.OperatorWagePerHour * totalOperators;
        double totalCostPerHour = capitalPerHour + laborCostPerHour;

        double piecesPerHour = handles.ShippedSink.Count * sol.RolandBatchSize / simHours;

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
            CostPerPiece: costPerPiece);
    }
}
