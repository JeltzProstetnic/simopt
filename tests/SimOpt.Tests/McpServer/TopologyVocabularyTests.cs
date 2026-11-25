using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using SimOpt.McpServer.Models;
using SimOpt.Engine.Api.Models;
using SimOpt.McpServer.Simulation;
using SimOpt.Engine.Api.Simulation;
using SimOpt.McpServer.Tools;
using SimOpt.Simulation.Entities;
using Xunit;

namespace SimOpt.Tests.McpServer;

/// <summary>
/// SIM-65 step 2, first part: the v1 names <c>queue</c> and <c>station</c>, with <c>buffer</c> and
/// <c>server</c> kept as aliases (D1); arrival and service times as distributions
/// (<c>arrival</c>, <c>service</c>), with the old <c>mean_interval</c> and <c>service_time</c> still
/// read as exponential means; a queue's <c>discipline</c>. What the builder cannot honour yet is
/// refused rather than silently built differently. (Stations with more than one server: step 3,
/// <see cref="TopologyMultiServerTests"/>.)
/// </summary>
public class TopologyVocabularyTests
{
    private static TopologyDefinition Line(string queueType = "buffer", string stationType = "server",
                                           Action<Dictionary<string, ParamValue>>? source = null,
                                           Action<Dictionary<string, ParamValue>>? queue = null,
                                           Action<Dictionary<string, ParamValue>>? station = null)
    {
        var src = new NodeDefinition { Id = "arrivals", Type = "source" };
        var q = new NodeDefinition { Id = "q", Type = queueType };
        var s = new NodeDefinition { Id = "s", Type = stationType };
        (source ?? (p => p["mean_interval"] = 2.0))(src.Params);
        queue?.Invoke(q.Params);
        (station ?? (p => p["service_time"] = 1.0))(s.Params);
        return new TopologyDefinition
        {
            Name = "line",
            Seed = 7,
            Nodes = { src, q, s, new NodeDefinition { Id = "done", Type = "sink" } },
            Connections =
            {
                new ConnectionDefinition { From = "arrivals", To = "q" },
                new ConnectionDefinition { From = "q", To = "s" },
                new ConnectionDefinition { From = "s", To = "done" },
            },
        };
    }

    private static int Delivered(TopologyDefinition topology, double duration = 1000)
    {
        var registry = new ModelRegistry();
        string id = registry.Create(topology);
        using JsonDocument doc = JsonDocument.Parse(new SimulationTools(registry).RunSimulation(id, duration));
        return doc.RootElement.GetProperty("stats").GetProperty("sinks").GetProperty("done").GetInt32();
    }

    private static DistributionSpec Constant(double v) => new DistributionSpec { Type = "constant", Value = v };

    [Fact]
    public void QueueAndStation_BuildTheSameModelAsBufferAndServer()
    {
        Delivered(Line("queue", "station")).Should().Be(Delivered(Line("buffer", "server")));
        Delivered(Line("Queue", "STATION")).Should().Be(Delivered(Line()));
    }

    [Fact]
    public void ArrivalAndServiceDistributions_DriveTheModel()
    {
        // D/D/1: an arrival every 2, a service of 1, so 1000 time units deliver 500 items, give or take the edge
        int delivered = Delivered(Line(stationType: "station",
            source: p => p["arrival"] = Constant(2),
            station: p => p["service"] = Constant(1)));

        delivered.Should().BeInRange(499, 501);
    }

    [Fact]
    public void ATriangularService_RunsTheClinicKindOfStation()
    {
        int delivered = Delivered(Line(stationType: "station",
            source: p => p["arrival"] = new DistributionSpec { Type = "exponential", Mean = 2 },
            station: p => p["service"] = new DistributionSpec { Type = "triangular", Min = 0.5, Mode = 1, Max = 1.5 }),
            duration: 20_000);

        delivered.Should().BeInRange(9_500, 10_500, "rho = 0.5, so the station keeps up with about 10,000 arrivals");
    }

    [Theory]
    [InlineData("source", "arrival", "mean_interval")]
    [InlineData("station", "service", "service_time")]
    public void GivingADistributionAndTheOldNumberTogether_IsRefused(string type, string dist, string number)
    {
        TopologyDefinition t = Line(stationType: "station");
        NodeDefinition node = t.Nodes.Find(n => n.Type == type)!;
        node.Params[dist] = Constant(1);
        node.Params[number] = 1.0;

        Action build = () => new ModelRegistry().Create(t);

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'" + node.Id + "'") && e.Message.Contains(dist) && e.Message.Contains(number));
    }

    [Fact]
    public void AMalformedServiceDistribution_IsRefused_NamingNodeAndParameter()
    {
        TopologyDefinition t = Line(stationType: "station",
            station: p => p["service"] = new DistributionSpec { Type = "triangular", Min = 1 });

        Action build = () => new ModelRegistry().Create(t);

        build.Should().Throw<InvalidOperationException>().Where(e => e.Message.Contains("'s'") && e.Message.Contains("service"));
    }

    [Theory]
    [InlineData("fifo", "E2")]
    [InlineData("lifo", "E3")]
    [InlineData("FIFO", "E2")]
    public void TheQueueDiscipline_DecidesWhoIsNext(string discipline, string next)
    {
        var registry = new ModelRegistry();
        ActiveModel active = registry.Get(registry.Create(Line("queue", "station", queue: p => p["discipline"] = discipline)));
        for (int i = 1; i <= 3; i++) active.Buffers["q"].Put(new SimpleEntity(active.Model, "E" + i, "E" + i));

        // the idle station took E1 at once; of E2 and E3 the discipline picks
        active.Buffers["q"].Preview().Identifier.Should().Be(next);
    }

    [Fact]
    public void AnUnknownDiscipline_IsRefused_NamingTheChoices()
    {
        Action build = () => new ModelRegistry().Create(Line("queue", queue: p => p["discipline"] = "random"));

        build.Should().Throw<InvalidOperationException>()
             .Where(e => e.Message.Contains("'q'") && e.Message.Contains("fifo") && e.Message.Contains("lifo"));
    }
}
