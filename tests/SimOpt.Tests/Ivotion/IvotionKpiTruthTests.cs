using System.Linq;
using FluentAssertions;
using SimOpt.Ivotion;
using SimOpt.Simulation.Statistics;
using SimOpt.Visualization.Views;
using Xunit;

namespace SimOpt.Tests.Ivotion;

/// <summary>
/// SIM-93: the Ivotion KPIs report what the run measured. Before, a run stopped by the step cap was
/// reported as complete, throughput divided by the requested duration, and "labour hours per hour"
/// was the headcount, which equals the hours worked only if every operator is busy all the time.
/// </summary>
public class IvotionKpiTruthTests
{
    private static readonly int[] Baseline = { 1, 2, 2, 1, 1, 15 };

    [Fact]
    public void A_run_ends_exactly_at_the_requested_time()
    {
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(Baseline));

        bool completed = h.RunFor(120.0);

        completed.Should().BeTrue();
        h.Model.CurrentTime.Should().Be(120.0);
    }

    [Fact]
    public void A_run_cut_short_by_the_step_cap_says_so()
    {
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(Baseline));

        bool completed = h.RunFor(120.0, maxSteps: 600);   // 600 steps of 0.1: 60 of the 120 minutes

        completed.Should().BeFalse();
        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);
        kpis.Completed.Should().BeFalse();
        kpis.SimulatedMinutes.Should().Be(h.Model.CurrentTime);
        kpis.ThroughputPerHour.Should().Be(h.ShippedSink.Count * 15 / (h.Model.CurrentTime / 60.0),
            "throughput is over the time simulated, not the time requested");
    }

    [Fact]
    public void A_completed_run_reports_its_length()
    {
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(Baseline));
        h.RunFor(120.0);

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        kpis.Completed.Should().BeTrue();
        kpis.SimulatedMinutes.Should().Be(120.0);
    }

    [Fact]
    public void Busy_labour_is_each_stations_measured_utilization_times_its_operators()
    {
        var solution = new IvotionSolution(new[] { 1, 3, 3, 2, 2, 15 });
        var h = IvotionTopologyBuilder.Build(solution);
        // busy time integrated here from each station's working transitions, apart from the KPI code
        var busy = new double[4];
        var since = new double[4];
        for (int k = 0; k < 4; k++)
        {
            int i = k;
            h.ManualStations[i].WorkingChangedEvent.AddHandler((_, working) =>
            {
                if (working) since[i] = h.Model.CurrentTime;
                else busy[i] += h.Model.CurrentTime - since[i];
            });
        }
        h.RunFor(120.0);
        for (int i = 0; i < 4; i++)
            if (h.ManualStations[i].Working) busy[i] += 120.0 - since[i];

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        int[] operators = { solution.OperatorsInspect, solution.OperatorsPack, solution.OperatorsLabel, solution.OperatorsSsb };
        double expected = busy.Select((b, i) => b / 120.0 * operators[i]).Sum();
        kpis.BusyLaborHoursPerSimHour.Should().BeApproximately(expected, 1e-12);
        kpis.LaborHoursPerSimHour.Should().Be(10, "paid labour is the headcount: operators are paid whether busy or not");
        kpis.BusyLaborHoursPerSimHour.Should().BeLessThan(kpis.LaborHoursPerSimHour);
        kpis.BusyLaborHoursPerSimHour.Should().BeGreaterThan(0);
    }

    [Fact]
    public void A_warm_up_leaves_out_what_shipped_before_it()
    {
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(Baseline));
        var afterWarmup = Instrumentation.ObserveCount(h.ShippedSink, warmup: 240.0);
        h.RunFor(480.0);

        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 480.0, warmupMinutes: 240.0);

        kpis.ThroughputPerHour.Should().Be(afterWarmup.Count * 15 / (240.0 / 60.0));
        afterWarmup.Count.Should().BeLessThan(h.ShippedSink.Count, "the line ships during the warm-up too");
    }

    [Fact]
    public void The_summary_names_paid_and_busy_labour_and_what_is_computed_from_the_configuration()
    {
        var h = IvotionTopologyBuilder.Build(new IvotionSolution(Baseline));
        h.RunFor(120.0);
        var kpis = IvotionKpis.Extract(h, simDurationMinutes: 120.0);

        string text = (string)IvotionKpisToSummaryConverter.Instance.Convert(kpis, typeof(string), null,
            System.Globalization.CultureInfo.InvariantCulture)!;

        text.Should().Contain("Paid labour").And.Contain("Busy labour").And.Contain("configuration");
        text.Should().NotContain("Labor hrs/hr");
    }
}
