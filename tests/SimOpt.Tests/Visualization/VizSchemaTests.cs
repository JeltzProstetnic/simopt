using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;
using SimOpt.Visualization.Models;
using Xunit;

namespace SimOpt.Tests.Visualization;

/// <summary>
/// SIM-110 step 3 (Q-VIEW-PATH): a <see cref="VizTopology"/> (the presets, furkansim's glass line)
/// converts to a schema v1 document plus a layout overlay, so the viewer can build every model with the
/// schema builder. <c>buffer</c> becomes a queue, <c>server</c> a station with a constant service, a
/// source's Gaussian arrivals the <c>normal</c> family, <c>reject_rate</c> a station followed by a
/// probabilistic router, <c>roland</c> a batch station, and a direct push into a server a queue between
/// them, as the schema requires. Step 3 ran each preset under both paths before step 4 deleted the old
/// builder: SQSS, Parallel Servers and Production Line agreed within 2 %; the factory's waste moved
/// (276 -> 241 at t = 2000) because the reject now follows the seed; the Ivotion line shipped about 15 times
/// as many (24 -> 354) because the old Roland printer passed one item of every batch of 15 on.
/// </summary>
public class VizSchemaTests
{
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
}
