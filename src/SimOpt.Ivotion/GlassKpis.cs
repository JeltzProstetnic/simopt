using System;

namespace SimOpt.Glass;

/// <summary>
/// Demo cost-model constants for the generic glass production line.
/// All monetary values in generic currency units per hour.
/// Displayed in the optimization UI for user override.
/// </summary>
public static class GlassCostModel
{
    /// <summary>Amortized capital cost per mixer per hour.</summary>
    public const double MixerCapitalPerHour = 4.0;

    /// <summary>Amortized capital cost per processing machine per hour.</summary>
    public const double ProcessingCapitalPerHour = 5.0;

    /// <summary>Fully-loaded production operator wage per hour.</summary>
    public const double OperatorWagePerHour = 32.0;
}

/// <summary>
/// KPI bundle extracted from a completed glass production-line simulation run.
/// All monetary values in generic currency units. Time basis is "per simulated hour".
/// </summary>
public readonly record struct GlassKpis(
    double ThroughputPerHour,
    double TotalCostPerHour,
    double LaborHoursPerSimHour,
    double FloorSpaceM2,
    double CostPerPiece)
{
    // Per-node footprints (metres²) — generic baseline values.
    private const double SourceFootprint = 4.0 * 4.0;       // 16
    private const double SinkFootprint = 4.0 * 4.0;         // 16
    private const double BufferFootprint = 2.5 * 3.0;       // 7.5
    private const double MixerFootprint = 5.0 * 4.0;        // 20
    private const double ProcessingFootprint = 5.0 * 4.0;   // 20
    private const double ManualStationFootprint = 5.0 * 4.0; // 20 (quality, packing)
    private const int BufferCount = 4;                      // bufMix, bufWip, bufQuality, bufPacking
    private const int ManualStationCount = 2;               // quality, packing

    public static GlassKpis Extract(GlassTopologyHandles handles, double simDurationMinutes)
        => Extract(handles, simDurationMinutes, GlassCostModel.OperatorWagePerHour);

    /// <summary>
    /// Extract with an explicit operator wage override. Enables UI-driven
    /// sensitivity analysis without touching the <see cref="GlassCostModel"/> const.
    /// </summary>
    public static GlassKpis Extract(
        GlassTopologyHandles handles,
        double simDurationMinutes,
        double operatorWagePerHour)
    {
        ArgumentNullException.ThrowIfNull(handles);
        if (simDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(simDurationMinutes),
                "Simulation duration must be positive.");
        if (operatorWagePerHour < 0)
            throw new ArgumentOutOfRangeException(nameof(operatorWagePerHour),
                "Operator wage must be non-negative.");

        var sol = handles.Solution;
        double simHours = simDurationMinutes / 60.0;

        int totalOperators = sol.OperatorsQuality + sol.OperatorsPacking;

        double capitalPerHour = GlassCostModel.MixerCapitalPerHour * sol.NumberOfMixers
                              + GlassCostModel.ProcessingCapitalPerHour * sol.NumberOfProcessingLines;
        double laborCostPerHour = operatorWagePerHour * totalOperators;
        double totalCostPerHour = capitalPerHour + laborCostPerHour;

        double piecesPerHour = handles.DoneSink.Count / simHours;

        double floorSpace = SourceFootprint + SinkFootprint
                          + BufferCount * BufferFootprint
                          + sol.NumberOfMixers * MixerFootprint
                          + sol.NumberOfProcessingLines * ProcessingFootprint
                          + ManualStationCount * ManualStationFootprint;

        double costPerPiece = piecesPerHour > 0
            ? totalCostPerHour / piecesPerHour
            : double.MaxValue;

        return new GlassKpis(
            ThroughputPerHour: piecesPerHour,
            TotalCostPerHour: totalCostPerHour,
            LaborHoursPerSimHour: totalOperators,
            FloorSpaceM2: floorSpace,
            CostPerPiece: costPerPiece);
    }
}
