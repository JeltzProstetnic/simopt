using System.Collections.Generic;
using FluentAssertions;
using SimOpt.Visualization.Models;
using Xunit;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-92 (SIM-110 slice V1, G1): the viewer displays what the engine's collectors measured and
/// computes no statistic itself. Before, it polled the busy flag once per tick (a value that moved
/// with the step size), differenced sink counts over a window, ranked bottlenecks twice (once by a
/// truncated display name) and invented a capacity of 100, or 20 racks, for an unbounded queue.
/// </summary>
public class SimulationModelMeasureTests
{
    private static SimulationModel Run(VizTopology topology, double step, double until)
    {
        var sim = new SimulationModel { EndTime = until };
        sim.Build(topology);
        sim.Start();
        while (sim.Step(step)) { }
        return sim;
    }

    // one source every 2.0 time units (no spread), one server taking 1.0: busy half the time
    private static VizTopology Clockwork(int? capacity = null) => new()
    {
        Name = "clockwork",
        Nodes = new List<VizNode>
        {
            new() { Id = "in", Type = "source", Params = new() { ["mean_interval"] = 2.0, ["stddev"] = 0.0 } },
            new() { Id = "queue", Type = "buffer", Params = capacity is int c ? new() { ["capacity"] = c } : new() },
            new() { Id = "press", Type = "server", Params = new() { ["service_time"] = 1.0 } },
            new() { Id = "out", Type = "sink" },
        },
        Connections = new List<VizConnection>
        {
            new() { From = "in", To = "queue" }, new() { From = "queue", To = "press" }, new() { From = "press", To = "out" },
        },
    };

    [Fact]
    public void Utilization_is_the_engines_and_does_not_move_with_the_step_size()
    {
        var coarse = Run(Clockwork(), step: 0.5, until: 50);
        var fine = Run(Clockwork(), step: 0.125, until: 50);

        double? u = coarse.Measure("press").Utilization;
        u.Should().NotBeNull();
        u!.Value.Should().BeInRange(0.46, 0.52, "the press works 1.0 in every 2.0");
        fine.Measure("press").Utilization.Should().Be(u, "a measure must not depend on how often the screen looks");
    }

    [Fact]
    public void Nothing_is_measured_before_time_passes()
    {
        var sim = new SimulationModel { EndTime = 50 };
        sim.Build(Clockwork());
        sim.Start();

        sim.Measure("press").Utilization.Should().BeNull();
        sim.Measure("queue").MeanQueueLength.Should().BeNull();
        sim.Throughput.Should().BeNull();
        sim.Bottleneck.Should().BeNull();
    }

    [Fact]
    public void Throughput_is_the_items_finished_per_unit_of_time()
    {
        var sim = Run(Clockwork(), step: 0.5, until: 50);

        sim.Throughput.Should().Be(sim.Measure("out").Count / 50d);
        sim.Measure("out").Count.Should().BeGreaterThan(20);
    }

    [Fact]
    public void The_bottleneck_is_the_busiest_server_named_by_its_id()
    {
        var sim = Run(VizTopology.ProductionLine(), step: 0.5, until: 100);

        double? u1 = sim.Measure("stage1").Utilization, u2 = sim.Measure("stage2").Utilization;
        sim.Bottleneck.Should().Be(u2 > u1 ? "stage2" : "stage1");
    }

    [Fact]
    public void An_unbounded_queue_has_no_capacity_to_fill()
    {
        Run(Clockwork(), step: 0.5, until: 10).Measure("queue").Capacity.Should().BeNull();
        Run(Clockwork(capacity: 7), step: 0.5, until: 10).Measure("queue").Capacity.Should().Be(7);
    }

    [Fact]
    public void A_queue_reports_its_time_average_length_and_mean_wait()
    {
        var sim = Run(VizTopology.ProductionLine(), step: 0.5, until: 100);

        sim.Measure("buf1").MeanQueueLength.Should().BeGreaterThan(0);
        sim.Measure("buf1").MeanWait.Should().BeGreaterThan(0);
        sim.Measure("stage1").MeanWait.Should().BeNull("a server has no queue of its own");
    }
}
