using FluentAssertions;
using SimOpt.Ivotion;
using Xunit;

namespace SimOpt.Tests.Ivotion;

/// <summary>
/// Tests for IvotionKpis — cost / throughput / labor / space extraction from
/// a completed simulation run.
/// </summary>
public class IvotionKpisTests
{
    private static IvotionTopologyHandles BuildAndRun(
        int[] parameters,
        double simMinutes = 120.0,
        double? arrivalIntervalMinutes = null)
    {
        var handles = IvotionTopologyBuilder.Build(
            new IvotionSolution(parameters),
            arrivalIntervalMinutes: arrivalIntervalMinutes);
        handles.RunFor(simMinutes);
        return handles;
    }

    [Fact]
    public void Extract_LaborCost_IsThirtyTwoDollarsPerOperator()
    {
        var h = BuildAndRun(new[] { 1, 2, 3, 1, 2, 15 });

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        // 2 + 3 + 1 + 2 = 8 operators × $32 = $256/hr labor.
        // Plus capital 1 × $3 = $3/hr. Total $259/hr.
        kpis.TotalCostPerHour.Should().BeApproximately(259.0, 1e-9);
    }

    [Fact]
    public void Extract_CapitalCost_IsThreeDollarsPerRoland()
    {
        var one = BuildAndRun(new[] { 1, 1, 1, 1, 1, 15 });
        var two = BuildAndRun(new[] { 2, 1, 1, 1, 1, 15 });

        var kOne = IvotionKpis.Extract(one, 120.0);
        var kTwo = IvotionKpis.Extract(two, 120.0);

        (kTwo.TotalCostPerHour - kOne.TotalCostPerHour).Should().BeApproximately(3.0, 1e-9);
    }

    [Fact]
    public void Extract_LaborHoursPerSimHour_EqualsTotalOperatorCount()
    {
        var h = BuildAndRun(new[] { 1, 3, 3, 2, 2, 15 });

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        // 3 + 3 + 2 + 2 = 10 operator-hours per simulated hour.
        kpis.LaborHoursPerSimHour.Should().Be(10);
    }

    [Fact]
    public void Extract_FloorSpace_IncreasesWithRolandCount()
    {
        var one = BuildAndRun(new[] { 1, 1, 1, 1, 1, 15 });
        var two = BuildAndRun(new[] { 2, 1, 1, 1, 1, 15 });

        var kOne = IvotionKpis.Extract(one, 120.0);
        var kTwo = IvotionKpis.Extract(two, 120.0);

        kTwo.FloorSpaceM2.Should().BeGreaterThan(kOne.FloorSpaceM2);
    }

    [Fact]
    public void Extract_Throughput_IsShippedPiecesPerHour()
    {
        var h = BuildAndRun(new[] { 1, 2, 2, 1, 1, 15 }, simMinutes: 120.0);
        int shippedBatches = h.ShippedSink.Count;

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        // Roland emits 1 entity per completed batch (SIM-44). To report
        // physical pieces/hour the extractor must multiply by batch size and
        // divide by the simulated hours.
        double expectedPiecesPerHour = shippedBatches * 15 / 2.0;
        kpis.ThroughputPerHour.Should().BeApproximately(expectedPiecesPerHour, 1e-9);
    }

    [Fact]
    public void Extract_CostPerPiece_IsCostPerHourDividedByThroughput()
    {
        var h = BuildAndRun(new[] { 1, 2, 2, 1, 1, 15 }, simMinutes: 120.0);

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        if (kpis.ThroughputPerHour > 0)
            kpis.CostPerPiece.Should().BeApproximately(
                kpis.TotalCostPerHour / kpis.ThroughputPerHour, 1e-9);
        else
            kpis.CostPerPiece.Should().Be(double.MaxValue);
    }

    [Fact]
    public void Extract_CostPerPiece_ReturnsMaxValueWhenNoThroughput()
    {
        // Zero run time → zero shipped → throughput = 0 → cost per piece unbounded.
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(new[] { 1, 1, 1, 1, 1, 15 }));
        // do NOT run

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        kpis.ThroughputPerHour.Should().Be(0);
        kpis.CostPerPiece.Should().Be(double.MaxValue);
    }
}
