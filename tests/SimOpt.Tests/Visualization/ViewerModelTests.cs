using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Visualization.Models;
using Xunit;
using Xunit.Abstractions;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-110 step 4 (Q-VIEW-PATH): every model the viewer shows is built by the schema builder
/// (<see cref="ModelRegistry.TryBuild"/>, through <see cref="VizSchema"/> for a <see cref="VizTopology"/>), so the
/// viewer runs the model the MCP head runs (UN-004, UN-025) and a reject follows the model's seed (UN-009).
/// SIM-92 (G1), kept from the old <c>SimulationModel</c>: the viewer displays what the engine's collectors
/// measured and computes no statistic itself.
/// </summary>
public class ViewerModelTests
{
    private readonly ITestOutputHelper output;

    public ViewerModelTests(ITestOutputHelper output) { this.output = output; }

    private static ViewerModel Run(VizTopology topology, double step, double until)
    {
        var sim = ViewerModel.Of(topology, endTime: until);
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

    // ── SIM-92: the engine measures, the viewer displays ─────────────────────────────

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
        var sim = ViewerModel.Of(Clockwork(), endTime: 50);

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

    // ── one model path ───────────────────────────────────────────────────────────────

    public static IEnumerable<object[]> Presets() => new[]
    {
        new object[] { "SQSS" }, new object[] { "Parallel Servers" }, new object[] { "Production Line" },
        new object[] { "Electronics Assembly Factory" }, new object[] { "Ivoclar Ivotion Packing Line" },
    };

    private static VizTopology Preset(string name) => name switch
    {
        "SQSS" => VizTopology.Sqss(),
        "Parallel Servers" => VizTopology.ParallelServers(),
        "Production Line" => VizTopology.ProductionLine(),
        "Electronics Assembly Factory" => VizTopology.FactoryFloor(),
        _ => VizTopology.IvotionPacking(),
    };

    [Theory]
    [MemberData(nameof(Presets))]
    public void EachPreset_ShowsTheModelTheSchemaBuilderBuilds(string preset)
    {
        const double until = 2_000;
        VizTopology viz = Preset(preset);
        var registry = new ModelRegistry();
        ActiveModel direct = registry.Get(registry.Create(VizSchema.ToSchema(viz).Topology));
        direct.Model.Run(until);

        ViewerModel shown = Run(viz, step: 1.0, until: until);

        Dictionary<string, int> expected = direct.Sinks.ToDictionary(s => s.Key, s => s.Value.Count);
        Dictionary<string, int> drawn = shown.GetNodeStates().Where(s => s.Type == "sink").ToDictionary(s => s.Id, s => s.Count);
        output.WriteLine($"{preset}, sinks at t = {until}: " + string.Join(", ", drawn.Select(kv => $"{kv.Key} {kv.Value}")));
        drawn.Should().Equal(expected, "the viewer steps the schema builder's model and its collectors change nothing");
    }

    [Fact]
    public void A_reject_follows_the_models_seed()
    {
        int Waste(int seed) => Run(VizTopology.FactoryFloor(seed), step: 1.0, until: 500).GetNodeStates().Single(s => s.Id == "waste").Count;

        Waste(42).Should().Be(Waste(42), "the same model and seed give the same run (UN-009)");
        Enumerable.Range(1, 5).Select(Waste).Distinct().Count().Should().BeGreaterThan(1, "the reject decisions are drawn from the seed");
    }

    [Fact]
    public void The_drawn_model_has_every_node_the_builder_made_and_keeps_the_layout_of_the_given_ones()
    {
        VizTopology viz = VizTopology.FactoryFloor();
        ViewerModel sim = ViewerModel.Of(viz);

        sim.Drawn.Nodes.Select(n => n.Id).Should().BeEquivalentTo(sim.Active.Topology.Nodes.Select(n => n.Id));
        foreach (VizNode given in viz.Nodes)
        {
            VizNode drawn = sim.Drawn.Nodes.Single(n => n.Id == given.Id);
            (drawn.X, drawn.Y, drawn.Width, drawn.Height, drawn.Label, drawn.Color)
                .Should().Be((given.X, given.Y, given.Width, given.Height, given.Label, given.Color));
        }
        sim.Drawn.Nodes.Single(n => n.Id == "qc_to_packing").Type.Should().Be("buffer", "a push into a server gets a queue between them");
        sim.Drawn.Nodes.Single(n => n.Id == "inspect_reject").Type.Should().Be("router");
        sim.Drawn.Nodes.Single(n => n.Id == "inspect").Type.Should().Be("server");
        sim.Drawn.Connections.Select(c => (c.From, c.To)).Should()
           .BeEquivalentTo(sim.Active.Topology.Connections.Select(c => (c.From, c.To)));
    }

    [Fact]
    public void An_inserted_queue_is_drawn_with_its_contents_and_measured()
    {
        ViewerModel sim = Run(VizTopology.FactoryFloor(), step: 1.0, until: 300);

        sim.BufferIds.Should().Contain("qc_to_packing");
        NodeState inserted = sim.GetNodeStates().Single(s => s.Id == "qc_to_packing");
        inserted.Capacity.Should().Be(int.MaxValue);
        sim.Measure("qc_to_packing").MeanWait.Should().NotBeNull();
    }

    [Fact]
    public void A_station_of_two_servers_is_one_node_busy_when_either_is()
    {
        var doc = new VizDocument(new TopologyDefinition
        {
            Name = "pool",
            Nodes =
            {
                new NodeDefinition { Id = "in", Type = "source", Params = { ["arrival"] = new DistributionSpec { Type = "constant", Value = 1.0 } } },
                new NodeDefinition { Id = "q", Type = "queue" },
                new NodeDefinition { Id = "desk", Type = "station", Params = { ["servers"] = 2, ["service"] = new DistributionSpec { Type = "constant", Value = 1.5 } } },
                new NodeDefinition { Id = "out", Type = "sink" },
            },
            Connections = { new() { From = "in", To = "q" }, new() { From = "q", To = "desk" }, new() { From = "desk", To = "out" } },
        }, new Dictionary<string, NodeLayout>());
        var sim = new ViewerModel(doc, endTime: 100);
        while (sim.Step(0.5)) { }

        sim.ServerIds.Should().Equal("desk");
        sim.GetNodeStates().Single(s => s.Id == "desk").Working.Should().BeTrue();
        sim.Measure("desk").Utilization!.Value.Should().BeApproximately(0.75, 0.02, "two servers each busy 1.5 of every 2.0");
    }

    [Fact]
    public void A_document_with_errors_is_refused_with_every_problem()
    {
        var doc = new VizDocument(new TopologyDefinition
        {
            Name = "broken",
            Nodes = { new NodeDefinition { Id = "in", Type = "source" }, new NodeDefinition { Id = "x", Type = "teleporter" } },
            Connections = { new() { From = "in", To = "x" } },
        }, new Dictionary<string, NodeLayout>());

        Action build = () => new ViewerModel(doc);

        build.Should().Throw<ModelValidationException>()
             .Which.Report.Errors.Should().Contain(e => e.Node == "x" && e.Code == "UNKNOWN_NODE_TYPE");
    }

    // ── warm-up ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Before_the_warm_up_ends_nothing_is_measured()
    {
        var sim = new ViewerModel(VizSchema.ToSchema(Clockwork()), endTime: 100, warmup: 40);
        while (sim.CurrentTime < 30) sim.Step(0.5);

        sim.WarmingUp.Should().BeTrue();
        sim.Measure("press").Utilization.Should().BeNull();
        sim.Measure("queue").MeanWait.Should().BeNull();
        sim.Measure("out").Count.Should().Be(0, "no item is counted before the warm-up ends");
        sim.Throughput.Should().BeNull();
        sim.GetNodeStates().Single(s => s.Id == "out").Count.Should().BeGreaterThan(10, "the items themselves still move");
    }

    [Fact]
    public void After_the_warm_up_the_measures_cover_only_the_time_after_it()
    {
        var sim = new ViewerModel(VizSchema.ToSchema(Clockwork()), endTime: 100, warmup: 40);
        while (sim.Step(0.5)) { }

        sim.WarmingUp.Should().BeFalse();
        sim.Warmup.Should().Be(40);
        sim.Measure("press").Utilization!.Value.Should().BeApproximately(0.5, 0.03);
        int counted = sim.Measure("out").Count!.Value;
        counted.Should().BeInRange(29, 31, "one item every 2.0 over the 60 after the warm-up");
        sim.Throughput.Should().Be(counted / 60d);
    }

    [Fact]
    public void A_warm_up_as_long_as_the_run_is_refused()
    {
        Action build = () => new ViewerModel(VizSchema.ToSchema(Clockwork()), endTime: 100, warmup: 100);

        build.Should().Throw<ArgumentOutOfRangeException>();
    }
}
