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
/// SIM-110 step 3 (Q-VIEW-PATH): a <see cref="VizTopology"/> (the presets, furkansim's glass line)
/// converts to a schema v1 document plus a layout overlay, so the viewer can build every model with the
/// schema builder. <c>buffer</c> becomes a queue, <c>server</c> a station with a constant service, a
/// source's Gaussian arrivals the <c>normal</c> family, <c>reject_rate</c> a station followed by a
/// probabilistic router, <c>roland</c> a batch station, and a direct push into a server a queue between
/// them, as the schema requires. Each preset runs under both paths here, before the old one is deleted,
/// and the test reports where the numbers differ and why.
/// </summary>
public class VizSchemaTests
{
    private readonly ITestOutputHelper output;

    public VizSchemaTests(ITestOutputHelper output) { this.output = output; }

    private static NodeDefinition Node(VizDocument doc, string id) => doc.Topology.Nodes.Single(n => n.Id == id);

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
    public void EveryPreset_ConvertsToADocumentThatValidates(string preset)
    {
        VizDocument doc = VizSchema.ToSchema(Preset(preset));
        ValidationReport report = ModelValidator.Validate(doc.Topology);

        report.Errors.Should().BeEmpty(string.Join("; ", report.Errors.Select(e => e.Line)));
        doc.Topology.SchemaVersion.Should().Be("1");
    }

    [Fact]
    public void ASourceServerAndBuffer_MapToNormalArrivalsAConstantStationAndAQueue()
    {
        VizDocument doc = VizSchema.ToSchema(VizTopology.Sqss(seed: 9));

        doc.Topology.Seed.Should().Be(9);
        DistributionSpec arrival = Node(doc, "source").Params["arrival"].Distribution!;
        (arrival.Type, arrival.Mean, arrival.Stddev).Should().Be(("normal", 2.0, 0.3));
        Node(doc, "queue").Type.Should().Be("queue");
        Node(doc, "queue").Params["capacity"].Number.Should().Be(15);
        Node(doc, "server").Type.Should().Be("station");
        Node(doc, "server").Params["service"].Distribution!.Should().Match<DistributionSpec>(d => d.Type == "constant" && d.Value == 1.8);
    }

    [Fact]
    public void NoSpread_IsAConstantInterval()
    {
        var viz = VizTopology.Sqss();
        viz.Nodes[0].Params["stddev"] = 0;

        VizDocument doc = VizSchema.ToSchema(viz);

        Node(doc, "source").Params["arrival"].Distribution!.Should().Match<DistributionSpec>(d => d.Type == "constant" && d.Value == 2.0);
    }

    [Fact]
    public void ARejectRate_IsAStationThenARouter_AndAPushIntoAServer_GetsAQueue()
    {
        VizDocument doc = VizSchema.ToSchema(VizTopology.FactoryFloor());
        TopologyDefinition t = doc.Topology;

        Node(doc, "inspect").Type.Should().Be("station");
        string router = t.Connections.Single(c => c.From == "inspect").To;
        Node(doc, router).Params["policy"].Text.Should().Be("probabilistic");
        t.Connections.Where(c => c.From == router).Select(c => (c.To, c.Weight))
         .Should().BeEquivalentTo(new (string, double?)[] { ("staging", 0.95), ("waste", 0.05) });
        string between = t.Connections.Single(c => c.From == "qc").To;
        Node(doc, between).Type.Should().Be("queue");
        t.Connections.Should().Contain(c => c.From == between && c.To == "packing");
    }

    [Fact]
    public void TheRolandPrinter_IsABatchStation()
    {
        VizDocument doc = VizSchema.ToSchema(VizTopology.IvotionPacking());

        NodeDefinition printer = Node(doc, "roland");
        printer.Type.Should().Be("station");
        printer.Params["batch"].Number.Should().Be(15);
        printer.Params["service"].Distribution!.Value.Should().BeApproximately(15 * 0.4, 1e-12);
    }

    [Fact]
    public void TheLayout_KeepsEveryDrawnNode_ByItsId()
    {
        VizTopology viz = VizTopology.FactoryFloor();
        VizDocument doc = VizSchema.ToSchema(viz);

        foreach (VizNode node in viz.Nodes)
        {
            NodeLayout layout = doc.Layout[node.Id];
            (layout.X, layout.Y, layout.Width, layout.Height, layout.Label, layout.Color)
                .Should().Be((node.X, node.Y, node.Width, node.Height, node.Label, node.Color));
        }
        doc.Layout.Keys.Should().BeEquivalentTo(viz.Nodes.Select(n => n.Id), "inserted queues and routers are laid out automatically");
    }

    // ── both paths, side by side ──────────────────────────────────────────────

    private const double Until = 2_000;

    private static Dictionary<string, int> OldPath(VizTopology viz)
    {
        var sim = new SimulationModel { EndTime = Until };
        sim.Build(viz);
        sim.Start();
        while (sim.Step(1.0)) { }
        return sim.GetNodeStates().Where(s => s.Type == "sink").ToDictionary(s => s.Id, s => s.Count);
    }

    private static Dictionary<string, int> NewPath(VizTopology viz)
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(VizSchema.ToSchema(viz).Topology));
        active.Model.Run(Until);
        return active.Sinks.ToDictionary(s => s.Key, s => s.Value.Count);
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void EachPreset_UnderBothPaths(string preset)
    {
        VizTopology viz = Preset(preset);
        Dictionary<string, int> before = OldPath(viz), after = NewPath(viz);
        output.WriteLine($"{preset}, sinks at t = {Until}: " +
                         string.Join(", ", before.Keys.Select(k => $"{k} {before[k]} -> {after[k]}")));

        before.Keys.Should().BeEquivalentTo(after.Keys);
        switch (preset)
        {
            case "Ivoclar Ivotion Packing Line":
                // the Roland printer passed one item per batch of 15 on and lost the rest; the batch station
                // passes all 15, so the line ships about 15 times as many
                ((double)after["shipped"] / before["shipped"]).Should().BeInRange(12, 16);
                break;
            case "Electronics Assembly Factory":
                // the reject now follows the model's seed (a router) instead of System.Random, and qc no
                // longer pushes straight into packing: the shipped count stays within 3 %, the waste share
                // near the 5 % reject rate on both paths
                after["shipping"].Should().BeCloseTo(before["shipping"], (uint)(0.03 * before["shipping"]));
                ((double)after["waste"] / (after["waste"] + after["shipping"])).Should().BeApproximately(0.05, 0.015);
                ((double)before["waste"] / (before["waste"] + before["shipping"])).Should().BeApproximately(0.05, 0.015);
                break;
            default:
                // same structure, new streams: the counts agree within 2 %
                foreach (string sink in before.Keys)
                    after[sink].Should().BeCloseTo(before[sink], (uint)Math.Max(2, 0.02 * before[sink]));
                break;
        }
    }
}
