using System.Linq;
using FluentAssertions;
using SimOpt.Examples.CarWash;
using SimOpt.Simulation.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Simulation.Processes;

/// <summary>
/// SIM-126, slice 6: the process example in <c>examples/SimOpt.Examples.CarWash</c> (SimPy's classic car
/// wash, extended with impatient drivers, a soap tank and a supplier) runs, is reproducible, and gives
/// answers that move the right way when a bay is added.
/// </summary>
public class CarWashExampleTests
{
    private readonly ITestOutputHelper output;

    public CarWashExampleTests(ITestOutputHelper output) => this.output = output;

    private static ReplicationResult Day(int bays) => CarWash.Run(new CarWash.Settings { Bays = bays }, replications: 10, baseSeed: 2026);

    [Fact]
    public void TheSameSeeds_GiveTheSameDay()
    {
        ReplicationResult a = Day(2), b = Day(2);

        foreach (MetricSummary m in a.Metrics)
            b[m.StatisticId].ReplicateEstimates.Should().Equal(m.ReplicateEstimates, m.StatisticId);
    }

    [Fact]
    public void EveryFigureIsInRange_AndTheSupplierKeepsTheTankGoing()
    {
        ReplicationResult day = Day(2);
        foreach (MetricSummary m in day.Metrics)
            output.WriteLine($"{m.StatisticName}: {m.Mean:F3} ± {m.HalfWidth:F3}");

        day[CarWash.GaveUp].Mean.Should().BeInRange(0, 1);
        day[CarWash.WaitForBay].Mean.Should().BeGreaterThanOrEqualTo(0);
        day[CarWash.BayUtilization].Mean.Should().BeInRange(0, 1);
        day[CarWash.SoapEmpty].Mean.Should().BeInRange(0, 0.5, "the supplier refills the tank");
        day.Metrics.Should().OnlyContain(m => m.HasData);
    }

    [Fact]
    public void AThirdBay_LetsFewerDriversGiveUp_AndShortensTheWait()
    {
        ReplicationResult two = Day(2), three = Day(3);
        output.WriteLine($"gave up: {two[CarWash.GaveUp].Mean:P1} with 2 bays, {three[CarWash.GaveUp].Mean:P1} with 3");
        output.WriteLine($"wait: {two[CarWash.WaitForBay].Mean:F2} min with 2 bays, {three[CarWash.WaitForBay].Mean:F2} with 3");

        three[CarWash.GaveUp].Mean.Should().BeLessThan(two[CarWash.GaveUp].Mean);
        three[CarWash.WaitForBay].Mean.Should().BeLessThan(two[CarWash.WaitForBay].Mean);
    }
}
